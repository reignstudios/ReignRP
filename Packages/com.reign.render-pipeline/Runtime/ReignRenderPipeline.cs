using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using Unity.Collections;
using static UnityEngine.GraphicsBuffer;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reign.SRP
{
    public sealed partial class ReignRenderPipeline : RenderPipeline
    {
		#if UNITY_EDITOR
		private Material errorMaterial;
		private static string[] errorIDs = new string[]
		{
			"Always",
			"ForwardBase",// TODO: this is used for mesh previews
			"ForwardAdd",
			"Deferred",
			"MotionVectors",
			"PrepassBase",
			"PrepassFinal",
			"Vertex",
			"VertexLMRGBM",
			"VertexLM"
		};
		#endif

		public const string lightModeID_Opaque = "Reign_Opaque";
        public const string lightModeID_Transparent = "Reign_Transparent";

        public static ReignRenderPipeline singleton { get; private set; }
		private ReignRenderPipelineAsset asset;

		#if !UNITY_EDITOR
		private int fullscreenSwapchainResolutionDivision;
		#endif

		private CommandBuffer cmd;
        private ShaderVars shaderVars;
        private CameraDataComparer cameraDataComparer = new CameraDataComparer();
        private List<CameraResource> cameraResources = new List<CameraResource>();

		private Vector4 directionalLight_Direction, directionalLight_Color;

		private const int pointLight_MaxConst = 4;
		private int pointLight_Max;
		private Vector4[] pointLight_Positions, pointLight_Colors;
		private Vector4[] pointLight_Positions_Const, pointLight_Colors_Const;
		private float[] pointLight_Distances;

        private bool motionBlurEnabled;

		private Material blitMaterial;

		public static int customGameWidth = -1, customGameHeight = -1;
        public static int gameWidth { get; private set; }
        public static int gameHeight { get; private set; }

		public static int cpuThreadCount { get; private set; }
		public static bool texturesSupported_32Bit { get; private set; }
        public static GraphicsDeviceType graphicsDeviceType { get; private set; }
		public static int graphicsShaderLevel { get; private set; }
		public static bool isOpenGL { get; private set; }

		private static List<XRDisplaySubsystem> xrSubsystemList;
		private static XRRenderPassInfo xrRenderPassInfo = new XRRenderPassInfo();

		public delegate void CustomDraw(Camera camera, in ScriptableRenderContext context, in CullingResults cullResults);
		public static event CustomDraw DrawCustom_PreOpaque, DrawCustom_PostOpaque, DrawCustom_PreTransparent, DrawCustom_PostTransparent;

		public ReignRenderPipeline(ReignRenderPipelineAsset asset)
		{
			singleton = this;
			this.asset = asset;

			// set graphic defaults
			GraphicsSettings.useScriptableRenderPipelineBatching = false;
			GraphicsSettings.lightsUseLinearIntensity = true;
			XRSettings.eyeTextureResolutionScale = 1;
			XRSettings.gameViewRenderMode = asset.xrPreviewMode;

			// create command buffer
			cmd = new CommandBuffer();

			// load error material
			#if UNITY_EDITOR
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
			#endif

			// configure shader vars
			shaderVars.time = Shader.PropertyToID("_Time");
			shaderVars.sinTime = Shader.PropertyToID("_SinTime");
            shaderVars.cosTime = Shader.PropertyToID("_CosTime");
            shaderVars.deltaTime = Shader.PropertyToID("unity_DeltaTime");
            shaderVars.timeParams = Shader.PropertyToID("_TimeParameters");

			//Lightmapping.SetDelegate(lightsDelegate);

			// disable vulkan pre-rotation setting
			#if UNITY_EDITOR
			PlayerSettings.vulkanEnablePreTransform = false;
			#endif

			// grab hardware info
			cpuThreadCount = Environment.ProcessorCount;
			graphicsDeviceType = SystemInfo.graphicsDeviceType;
            graphicsShaderLevel = SystemInfo.graphicsShaderLevel;
			isOpenGL = graphicsDeviceType == GraphicsDeviceType.OpenGLCore || graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
			texturesSupported_32Bit = SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormatUsage.Sample) && SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormatUsage.SetPixels);
		}

		private void CheckResourceInit()
		{
			// blit resources
			BlitMesh.InitCheck();
			//if (!skyboxMesh) skyboxMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
			if (!blitMaterial) blitMaterial = new Material(asset.resources.shaders.blitShader);

			// allocate render-path specific light buffers
			if (asset.maxLights < 0) pointLight_Max = 1024;
			else pointLight_Max = Math.Max(0, asset.maxLights);
			if (pointLight_Positions == null || pointLight_Positions.Length != pointLight_Max)
			{
                pointLight_Positions = new Vector4[pointLight_Max];
				pointLight_Colors = new Vector4[pointLight_Max];

				pointLight_Distances = new float[pointLight_Max];
			}

			if (pointLight_Positions_Const == null)
			{
				pointLight_Positions_Const = new Vector4[pointLight_MaxConst];
				pointLight_Colors_Const = new Vector4[pointLight_MaxConst];
			}
        }
		
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
			// ensure asset settings are valid
			asset.ValidateSettings();

			// validate swap-chain resolution
			#if !UNITY_EDITOR
			if (Screen.fullScreen && fullscreenSwapchainResolutionDivision != asset.fullscreenSwapchainResolutionDivision)
			{
				int swapchainResolutionDivisionLast = fullscreenSwapchainResolutionDivision;
				fullscreenSwapchainResolutionDivision = asset.fullscreenSwapchainResolutionDivision;
				if (swapchainResolutionDivisionLast != 0 || fullscreenSwapchainResolutionDivision != 1)
				{
					var resolution = Screen.currentResolution;
					resolution.width /= fullscreenSwapchainResolutionDivision;
					resolution.height /= fullscreenSwapchainResolutionDivision;
					ChangeSwapChainResolution(resolution, Screen.fullScreenMode);
				}
			}
			#endif

			// check if common resources init
			CheckResourceInit();

			// check if camera resources need to be released
			for (int i = cameraResources.Count - 1; i != -1; --i)
			{
				var resource = cameraResources[i];
				++resource.frame;
				#if UNITY_EDITOR
				if (resource.frame >= 100 && resource.camera && !resource.camera.enabled)
				#else
				if (resource.frame >= 100)
				#endif
				{
					resource.ReleaseBuffers(true);
					cameraResources.RemoveAt(i);
				}
			}

            // set shader time vars
            SetShaderTimeValues(cmd, Time.time, Time.deltaTime, Time.smoothDeltaTime);
            context.ExecuteCommandBuffer(cmd);

			// start rendering cameras
            SortCameras(cameras);
            BeginContextRendering(context, cameras);
            foreach (var camera in cameras)
            {
				motionBlurEnabled = MotionBlurEnabled(camera);
                BeginCameraRendering(context, camera);
                if (IsXREnabled(camera))
                {
                    // validate XR single-pass support
                    //bool singlepassSupported = false;
					if (xrSubsystemList == null) xrSubsystemList = new List<XRDisplaySubsystem>();
                    if (xrSubsystemList.Count == 0)
					{
						SubsystemManager.GetSubsystems(xrSubsystemList);
					}
                    else
                    {
                        var subsystem = xrSubsystemList[0];
						int renderPassCount = subsystem.GetRenderPassCount();
                        if (renderPassCount > 0)
                        {
							xrRenderPassInfo.isXRActive = true;
							if (renderPassCount == 2)
							{
								for (int i = 0; i != renderPassCount; ++i)
								{
									xrRenderPassInfo.eyePass = i;
									subsystem.GetRenderPass(i, out xrRenderPassInfo.pass);
									xrRenderPassInfo.pass.GetRenderParameter(camera, 0, out xrRenderPassInfo.parameter);
									RenderPass(ref context, camera);// manually set eye for multi-pass
								}
							}
							else
							{
								xrRenderPassInfo.eyePass = -1;
								subsystem.GetRenderPass(0, out xrRenderPassInfo.pass);
								xrRenderPassInfo.pass.GetRenderParameter(camera, 0, out xrRenderPassInfo.parameter);
								RenderPass(ref context, camera);// let Unity deside what the eye is for single-pass
							}
                        }
                    }
                }
                else
                {
					xrRenderPassInfo.isXRActive = false;
					xrRenderPassInfo.eyePass = -1;
                    RenderPass(ref context, camera);// non-XR single eye pass
                }
                EndCameraRendering(context, camera);
            }
			
            // render scene
            EndContextRendering(context, cameras);
        }

		private void SetShaderTimeValues(CommandBuffer cmd, float time, float deltaTime, float smoothDeltaTime)
		{
			// We make these parameters to mirror those described in `https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
			float timeEights = time / 8f;
			float timeFourth = time / 4f;
			float timeHalf = time / 2f;

			// Time values
			Vector4 timeVector = time * new Vector4(1f / 20f, 1f, 2f, 3f);
			Vector4 sinTimeVector = new Vector4(Mathf.Sin(timeEights), Mathf.Sin(timeFourth), Mathf.Sin(timeHalf), Mathf.Sin(time));
			Vector4 cosTimeVector = new Vector4(Mathf.Cos(timeEights), Mathf.Cos(timeFourth), Mathf.Cos(timeHalf), Mathf.Cos(time));
			Vector4 deltaTimeVector = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
			Vector4 timeParametersVector = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);

			// set shader values
			cmd.SetGlobalVector(shaderVars.time, timeVector);
			cmd.SetGlobalVector(shaderVars.sinTime, sinTimeVector);
			cmd.SetGlobalVector(shaderVars.cosTime, cosTimeVector);
			cmd.SetGlobalVector(shaderVars.deltaTime, deltaTimeVector);
			cmd.SetGlobalVector(shaderVars.timeParams, timeParametersVector);
		}

		private void SortCameras(List<Camera> cameras)
		{
			cameras.Sort(cameraDataComparer);
		}

		private void SortPointLights(Camera camera, int pointLight_Count)
        {
            // get light distances from camera
            var camPos = camera.transform.position;
            for (int i = 0; i != pointLight_Count; ++i)
            {
                Vector3 lightPos = pointLight_Positions[i];
                var vec = lightPos - camPos;
                pointLight_Distances[i] = Vector3.Dot(vec, vec);
            }

			// sort lights by distance to camera
            for (int i = 0; i != pointLight_Count; ++i)
            {
                float dis = pointLight_Distances[i];
                for (int i2 = i + 1; i2 < pointLight_Count; ++i2)
                {
                    if (pointLight_Distances[i2] < dis)
                    {
                        pointLight_Distances[i] = pointLight_Distances[i2];
                        pointLight_Distances[i2] = dis;
                        dis = pointLight_Distances[i];

                        var current = pointLight_Positions[i];
                        pointLight_Positions[i] = pointLight_Positions[i2];
                        pointLight_Positions[i2] = current;

                        current = pointLight_Colors[i];
                        pointLight_Colors[i] = pointLight_Colors[i2];
                        pointLight_Colors[i2] = current;
                    }
                }
            }

            // set closest lights to full forward
            CopyPointLightsToConsts(pointLight_Count);
        }

		private void CopyPointLightsToConsts(int pointLight_Count)
		{
			for (int i = 0; i != pointLight_MaxConst; ++i)
            {
                if (i < pointLight_Count)
                {
                    pointLight_Positions_Const[i] = pointLight_Positions[i];
                    pointLight_Colors_Const[i] = pointLight_Colors[i];
                }
                else
                {
                    pointLight_Positions_Const[i] = Vector4.zero;
                    pointLight_Colors_Const[i] = Vector4.zero;
                }
            }
		}


		private void RenderPass(ref ScriptableRenderContext context, Camera camera)
		{
			if (asset.compositionDivision < 1) asset.compositionDivision = 1;

			// find or allocate camera resources
			CameraResource cameraResource = null;
			if (!CameraResourceExists(camera, out cameraResource))
			{
				cameraResource = new CameraResource(camera, this);
				cameraResources.Add(cameraResource);
			}
			cameraResource.UpdateStart();

			// get max shadow plane
			float maxShadowPlane = 0;
			if (asset.shadowType != ShadowType.Off)
			{
				switch (asset.shadowCascades)
				{
					case ShadowCascades.x1: maxShadowPlane = asset.shadowCascadePlanes.x; break;
					case ShadowCascades.x2: maxShadowPlane = asset.shadowCascadePlanes.y; break;
					case ShadowCascades.x3: maxShadowPlane = asset.shadowCascadePlanes.z; break;
					case ShadowCascades.x4: maxShadowPlane = asset.shadowCascadePlanes.w; break;
				}
			}

			// standard camera prep
			CameraPrep(ref context, camera, out var cullResults, out var cullingParameters, maxShadowPlane);

			// setup camera special data mode
            var depthTextureMode = DepthTextureMode.None;
            var specialRenderParams = PerObjectData.None;
			if (asset.enableReflectionProbes) specialRenderParams |= PerObjectData.ReflectionProbes;
			if (asset.enableLightmaps) specialRenderParams |= PerObjectData.Lightmaps;
            if (motionBlurEnabled)
            {
                depthTextureMode = DepthTextureMode.MotionVectors;
                specialRenderParams = PerObjectData.MotionVectors;
            }
            if ((camera.depthTextureMode & depthTextureMode) == 0) camera.depthTextureMode = depthTextureMode;

			// clear camera pre
			ClearCameraPre(ref context, camera, cameraResource, depthTextureMode);

			// setup lighting
			var lights = cullResults.visibleLights;
			int directionalLight_Count = 0;
			int pointLight_Count = 0;
			foreach (var light in lights)
			{
				switch (light.lightType)
				{
					case LightType.Directional:
						if (directionalLight_Count < 1)
						{
							directionalLight_Direction = light.light.transform.forward;
							directionalLight_Color = light.finalColor;
							directionalLight_Color.w = light.light.intensity;
							directionalLight_Count++;
						}
						break;

					case LightType.Point:
						if (pointLight_Count < pointLight_Max)
						{
							pointLight_Positions[pointLight_Count] = light.light.transform.position;
							pointLight_Positions[pointLight_Count].w = light.range;
							pointLight_Colors[pointLight_Count] = light.finalColor;
							pointLight_Colors[pointLight_Count].w = light.light.intensity;
							pointLight_Count++;
						}
						break;
				}
			}
			
			cmd.Clear();
			cmd.SetGlobalVector("directionalLight_Direction", directionalLight_Direction);
			cmd.SetGlobalVector("directionalLight_Color", directionalLight_Color);
			cmd.SetGlobalFloat("directionalLight_Count", directionalLight_Count);
			if (pointLight_Count > 0)
			{
				if (asset.sortPointLights) SortPointLights(camera, pointLight_Count);
				else CopyPointLightsToConsts(pointLight_Count);
				cmd.SetGlobalVectorArray("pointLight_Positions", pointLight_Positions_Const);
				cmd.SetGlobalVectorArray("pointLight_Colors", pointLight_Colors_Const);
				cmd.SetGlobalFloat("pointLight_Count", Math.Min(pointLight_Count, pointLight_MaxConst));
				cmd.DisableShaderKeyword("REIGN_POINT_LIGHTS_DISABLE");
			}
			else
			{
				cmd.EnableShaderKeyword("REIGN_POINT_LIGHTS_DISABLE");
			}
			context.ExecuteCommandBuffer(cmd);

			// set special data
			cmd.Clear();
            cmd.SetGlobalVector("compositingSize", new Vector4(cameraResource.widthComposited, cameraResource.heightComposited, 1.0f / cameraResource.widthComposited, 1.0f / cameraResource.heightComposited));
            cmd.SetGlobalMatrix("clipToWorld", cameraResource.clipToWorld);
			context.ExecuteCommandBuffer(cmd);
			context.Submit();

			// start opaque render pass
			StartRenderPass(context, cameraResource.forwardRenderPass_Opaque, camera);
			
			// draw custom pre-opaque objects
			DrawCustom_PreOpaque?.Invoke(camera, context, cullResults);

			// draw custom opaque objects
			DrawCustomUnlitObjects(ref context, ref cullResults, QueueRange.Opaque, camera);

			// draw opaque objects
			DrawObjects(ref context, ref cullResults, lightModeID_Opaque, QueueRange.Opaque, camera, null, specialRenderParams);

			// draw custom post-opaque objects
			DrawCustom_PostOpaque?.Invoke(camera, context, cullResults);

			// finish opaque render pass
			EndRenderPass(context);

			// enable depth-texture to be sampled
			if (asset.enableComposition && asset.compositionDepthClone)
			{
				cmd.Clear();
				cameraResource.ResolveCompositedDepthTexture(cmd);
				context.ExecuteCommandBuffer(cmd);
				context.Submit();
			}

			// start lighting render pass
			StartRenderPass(context, cameraResource.forwardRenderPass_Transparent, camera);

			// clear camera post (after opaque)
            ClearCameraPost(ref context, camera);

            // draw custom pre-transparent objects
            DrawCustom_PreTransparent?.Invoke(camera, context, cullResults);

			// draw custom transparent objects
			DrawCustomUnlitObjects(ref context, ref cullResults, QueueRange.Transparent, camera);

			// draw transparent objects
			DrawObjects(ref context, ref cullResults, lightModeID_Transparent, QueueRange.Transparent, camera, null, specialRenderParams);

			// draw custom post-transparent objects
			DrawCustom_PostTransparent?.Invoke(camera, context, cullResults);

			// draw unuspported objects & editor gizmos
			DrawErrorObjectsAndPreGizmos(ref context, ref cullResults, camera);

			// finish lighting render pass
			EndRenderPass(context);

			// compositing
			if (asset.enableComposition)
			{
				#if UNITY_EDITOR
				Mesh blitMesh;
				if (isOpenGL)
				{
					blitMesh = BlitMesh.mesh;
				}
				else
				{
					blitMesh = (camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview) ? BlitMesh.meshFlipped : BlitMesh.mesh;
				}
				#else
				var blitMesh = BlitMesh.mesh;
				#endif
				
				cmd.Clear();
				cmd.SetRenderTarget(cameraResource.cameraTargetTextureID);
				cmd.SetGlobalTexture("_BlitTex", cameraResource.colorTextureID);
				cmd.SetViewport(camera.pixelRect);
				cmd.DrawMesh(blitMesh, Matrix4x4.identity, blitMaterial);
				context.ExecuteCommandBuffer(cmd);
			}
			else if (xrRenderPassInfo.isXRActive)
			{
				bool draw = false;
				var eye = XRSettings.gameViewRenderMode;
				var r = camera.pixelRect;
				if (xrRenderPassInfo.eyePass < 0)
				{
					r = camera.pixelRect;
					draw = true;
				}
				else
				{
					r = camera.pixelRect;
					if (xrRenderPassInfo.eyePass == 0)
					{
						if (eye == GameViewRenderMode.BothEyes)
						{
							r.x = 0;
							r.width /= 2;
							draw = true;
						}
						else if (eye == GameViewRenderMode.LeftEye)
						{
							draw = true;
						}
					}
					else
					{
						if (eye == GameViewRenderMode.BothEyes)
						{
							r.x = r.width / 2;
							r.width /= 2;
							draw = true;
						}
						else if (eye == GameViewRenderMode.RightEye)
						{
							draw = true;
						}
					}
					cmd.SetViewport(r);
				}

				if (draw)
				{
					cmd.Clear();
					cmd.SetRenderTarget(BuiltinRenderTextureType.None);
					cmd.SetGlobalTexture("_BlitTex", cameraResource.cameraTargetTextureID);
					cmd.SetViewport(r);
					cmd.DrawMesh(BlitMesh.meshFlipped, Matrix4x4.identity, blitMaterial);
					context.ExecuteCommandBuffer(cmd);
				}
			}

			// standard camera finish
			CameraFinish(ref context, camera, ref cullResults);
			
			// release temp resources
			cameraResource.UpdateEnd();
            context.Submit();
        }

		private void StartRenderPass(in ScriptableRenderContext context, in RenderPassDesc renderPassDesc, Camera camera)
		{
            if (asset.useRenderPasses)
            {
                context.BeginRenderPass(renderPassDesc.width, renderPassDesc.height, 1, renderPassDesc.attachments, renderPassDesc.depthIndex);
                context.BeginSubPass(renderPassDesc.attachmentIndices);

				/*if (!asset.enableComposition)
				{
					cmd.Clear();
					cmd.SetViewport(camera.pixelRect);
					context.ExecuteCommandBuffer(cmd);
				}*/

				if (xrRenderPassInfo.isXRActive)
				{
					cmd.Clear();
					var r = camera.pixelRect;
					r.x *= xrRenderPassInfo.parameter.viewport.x;
					r.y *= xrRenderPassInfo.parameter.viewport.y;
					r.width *= xrRenderPassInfo.parameter.viewport.width;
					r.height *= xrRenderPassInfo.parameter.viewport.height;
					cmd.SetViewport(r);
					context.ExecuteCommandBuffer(cmd);
				}
            }
			else
			{
				cmd.Clear();

				// enable targets
				if (renderPassDesc.renderTargets.Length >= 2)
				{
					cmd.SetRenderTarget(renderPassDesc.renderTargets, renderPassDesc.renderTarget_Depth);
				}
				else
				{
					cmd.SetRenderTarget(renderPassDesc.renderTarget_First, renderPassDesc.renderTarget_Depth);
				}

                //if (!asset.enableComposition) cmd.SetViewport(camera.pixelRect);
				if (xrRenderPassInfo.isXRActive)
				{
					//float offset = 0, mul = .5f;
					//if (xrRenderPassInfo.eyePass == 1) offset = .5f;
					//Debug.Log(xrRenderPassInfo.parameter.viewport);
					var s = new Vector2(xrRenderPassInfo.pass.renderTargetDesc.width, xrRenderPassInfo.pass.renderTargetDesc.height);
					var viewR = xrRenderPassInfo.parameter.viewport;
					var r = new Rect
					(
						viewR.x * s.x,
						viewR.y * s.y,
						viewR.width * s.x,
						viewR.height * s.y
					);
					cmd.SetViewport(r);
				}

                // clear color and depth (NOTE: only clear with first color-targets clear color for performance)
				bool clearColor = false, clearDepth = false;
				var mainClearColor = Color.clear;
				for (int i = 0; i != renderPassDesc.attachments.Length; ++i)
				{
					ref var target = ref renderPassDesc.targets[i];
					if (target.clear)
					{
						if (target.renderTargetFormat == RenderTextureFormat.Depth)
						{
							clearDepth = true;
						}
						else if (!clearColor)
						{
							clearColor = true;
							mainClearColor = target.clearColor;
						}
					}
				}

				if (clearColor || clearDepth)
				{
					cmd.ClearRenderTarget(clearDepth, clearColor, mainClearColor);
				}

				context.ExecuteCommandBuffer(cmd);
			}
        }

		private void EndRenderPass(in ScriptableRenderContext context)
		{
			if (asset.useRenderPasses)
			{
				context.EndSubPass();
				context.EndRenderPass();
			}
			else
			{
				cmd.Clear();
				cmd.SetRenderTarget(BuiltinRenderTextureType.None);// disable all render-targets so they can be used in other operations
				context.ExecuteCommandBuffer(cmd);
                context.Submit();
            }
        }

		private void CameraPrep(ref ScriptableRenderContext context, Camera camera, out CullingResults cullResults, out ScriptableCullingParameters cullingParameters, float shadowDistance)
		{
			// setup camera
			if (xrRenderPassInfo.isXRActive)
			{
				if (xrRenderPassInfo.eyePass >= 0)
				{
					context.SetupCameraProperties(camera, true, xrRenderPassInfo.eyePass);
					context.StartMultiEye(camera, xrRenderPassInfo.eyePass);
				}
				else
				{
					context.SetupCameraProperties(camera, true);
					context.StartMultiEye(camera);
				}
			}
			else
			{
				context.SetupCameraProperties(camera);
			}

			// allow UI scene objects to be culled
			#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView) ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			#endif
			
			// get camera culled objects
			if (!camera.TryGetCullingParameters(xrRenderPassInfo.isXRActive, out cullingParameters)) Debug.LogError("Failed: TryGetCullingParameters");
			cullingParameters.shadowDistance = shadowDistance;
			cullingParameters.maximumVisibleLights = 1 + pointLight_Max;// directional + point
			cullResults = context.Cull(ref cullingParameters);
		}

		private void CameraFinish(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullResults)
		{
			// make sure editor camera target textures are enabled
			#if UNITY_EDITOR
			if (camera.cameraType != CameraType.SceneView)
			{
				cmd.Clear();
				if (camera.targetTexture) cmd.SetRenderTarget(camera.targetTexture);
				else cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				context.ExecuteCommandBuffer(cmd);

				// draw post gizmos
				if (camera.cameraType == CameraType.SceneView && Handles.ShouldRenderGizmos())
				{
					context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
				}
			}
			#endif

			// finish XR eye rendering
			if (IsXREnabled(camera))
			{
				context.StopMultiEye(camera);
				if (xrRenderPassInfo.eyePass >= 0) context.StereoEndRender(camera, xrRenderPassInfo.eyePass, xrRenderPassInfo.eyePass != 0);
				else context.StereoEndRender(camera);
			}
		}

		private void ClearCameraPre(ref ScriptableRenderContext context, Camera camera, CameraResource cameraResource, DepthTextureMode depthTextureMode)
		{
			// pre-clear if needed
			/*cmd.Clear();
			if (!IsXREnabled(camera) || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
			{
				// set target to clear
				if (asset.enableEffectCompositing)
				{
					if (depthTextureMode == DepthTextureMode.MotionVectors)
					{
						cmd.SetRenderTarget(cameraResource.velocityTexture);// clear velocity texture
						cmd.ClearRenderTarget(false, true, Color.clear);
					}

					cmd.SetRenderTarget(cameraResource.colorTextureID, cameraResource.depthTextureID);
				}
				else
				{
					cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				}

				// set viewport
				cmd.SetViewport(camera.pixelRect);

				// clear target
				if (camera.cameraType == CameraType.Preview)
				{
					cmd.ClearRenderTarget(true, true, Color.black);
				}
				else if (camera.clearFlags == CameraClearFlags.SolidColor)
				{
					cmd.ClearRenderTarget(true, true, camera.backgroundColor.linear);
				}
				else if (camera.clearFlags == CameraClearFlags.Depth || camera.clearFlags == CameraClearFlags.Skybox)
				{
					cmd.ClearRenderTarget(true, false, Color.black);
				}
			}
			else
			{
				if (camera.clearFlags == CameraClearFlags.SolidColor)
				{
					if (asset.enableEffectCompositing)
					{
						CoreUtils.SetRenderTarget(cmd, cameraResource.colorTextureID, cameraResource.depthTextureID, ClearFlag.All, camera.backgroundColor.linear);
					}
					else
					{
						CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, ClearFlag.All, camera.backgroundColor.linear);
					}
				}
				else if (camera.clearFlags == CameraClearFlags.Depth || camera.clearFlags == CameraClearFlags.Skybox)
				{
					if (asset.enableEffectCompositing)
					{
						CoreUtils.SetRenderTarget(cmd, cameraResource.colorTextureID, cameraResource.depthTextureID, ClearFlag.Depth, camera.backgroundColor.linear);
					}
					else
					{
						CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, ClearFlag.Depth, camera.backgroundColor.linear);
					}
				}
				else
				{
					if (asset.enableEffectCompositing)
					{
						CoreUtils.SetRenderTarget(cmd, cameraResource.colorTextureID, cameraResource.depthTextureID, ClearFlag.None, Color.black);
					}
					else
					{
						CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, ClearFlag.None, Color.black);
					}
				}
			}
			context.ExecuteCommandBuffer(cmd);*/

			/*// clip invisible pixels
            #if UNITY_EDITOR
            if (IsXREnabled(camera) && XRSettings.gameViewRenderMode == GameViewRenderMode.OcclusionMesh)
            {
                cmd.Clear();
                //XRUtils.DrawOcclusionMesh(cmd, camera);
				var r = camera.pixelRect;
				cmd.DrawOcclusionMesh(new RectInt((int)r.x, (int)r.y, (int)r.width, (int)r.height));
                context.ExecuteCommandBuffer(cmd);
            }
            #else
            if (IsXREnabled(camera))
            {
                cmd.Clear();
                //XRUtils.DrawOcclusionMesh(cmd, camera);
				var r = camera.pixelRect;
				cmd.DrawOcclusionMesh(new RectInt((int)r.x, (int)r.y, (int)r.width, (int)r.height));
                context.ExecuteCommandBuffer(cmd);
            }
            #endif*/
		}

		private void ClearCameraPost(ref ScriptableRenderContext context, Camera camera)
		{
			// post-clear if needed
			if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox)
			{
				cmd.Clear();
				var cam = camera.transform;
				cmd.DrawMesh(asset.resources.meshes.skyboxMesh, Matrix4x4.TRS(cam.position, Quaternion.identity, Vector3.one * camera.farClipPlane * .95f), RenderSettings.skybox, 0, 0);
				context.ExecuteCommandBuffer(cmd);
				//context.DrawSkybox(camera);// this has issues on GLES3 platforms. Use method above
			}
		}

		private void DrawErrorObjectsAndPreGizmos(ref ScriptableRenderContext context, ref CullingResults cullResults, Camera camera)
		{
			// draw editor objects
			#if UNITY_EDITOR
			// draw material error objects
			foreach (string errorID in errorIDs) DrawObjects(ref context, ref cullResults, errorID, QueueRange.Any, camera, errorMaterial, PerObjectData.None);

			// draw pre gizmos
			if (camera.cameraType == CameraType.SceneView && Handles.ShouldRenderGizmos())
			{
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
			#endif
		}

		private void DrawCustomUnlitObjects(ref ScriptableRenderContext context, ref CullingResults cullResults, QueueRange range, Camera camera)
		{
			DrawObjects(ref context, ref cullResults, "SRPDefaultUnlit", range, camera, null, PerObjectData.None);
		}

		private void DrawCustomUnlitObjects(ref ScriptableRenderContext context, ref CullingResults cullResults, Camera camera)
		{
			DrawObjects(ref context, ref cullResults, "SRPDefaultUnlit", QueueRange.Opaque, camera, null, PerObjectData.None);
			DrawObjects(ref context, ref cullResults, "SRPDefaultUnlit", QueueRange.Transparent, camera, null, PerObjectData.None);
		}

		public static void DrawObjectsCustom(ScriptableRenderContext context, CullingResults cullResults, string lightModeID, QueueRange range, Camera camera)
		{
			singleton.DrawObjects(ref context, ref cullResults, lightModeID, range, camera, null, PerObjectData.None);
		}

		public static void DrawObjectsCustom(ScriptableRenderContext context, CullingResults cullResults, string lightModeID, QueueRange range, Camera camera, Material overrideMaterial, PerObjectData objectData)
		{
			singleton.DrawObjects(ref context, ref cullResults, lightModeID, range, camera, overrideMaterial, objectData);
		}

		private void DrawObjects(ref ScriptableRenderContext context, ref CullingResults cullResults, string lightModeID, QueueRange range, Camera camera, Material overrideMaterial, PerObjectData objectData)
		{
			// filter settings
			var filterSettings = FilteringSettings.defaultValue;
			filterSettings.renderQueueRange = QueueRangeToRenderQueueRange(range, out var sortingCriteria);

			// draw settings
			var sortSettings = new SortingSettings(camera);
			sortSettings.criteria = sortingCriteria;
			var drawSettings = new DrawingSettings(new ShaderTagId(lightModeID), sortSettings);
			drawSettings.overrideMaterial = overrideMaterial;
			drawSettings.perObjectData = objectData;
			drawSettings.enableDynamicBatching = true;
			drawSettings.enableInstancing = true;

			// draw objects
			#if UNITY_2021
			context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
			#else
			var renderListParams = new RendererListParams(cullResults, drawSettings, filterSettings);
			var renderContext = context.CreateRendererList(ref renderListParams);
			cmd.Clear();
			cmd.DrawRendererList(renderContext);
			context.ExecuteCommandBuffer(cmd);
			#endif
		}

		private Color GetSceneAmbientColor()
		{
			SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
			return new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.ambientIntensity * 2.0f;
		}

		private bool IsXREnabled(Camera camera)
		{
			return XRSettings.enabled && camera.stereoTargetEye == StereoTargetEyeMask.Both && camera.cameraType == CameraType.Game;
		}

		public bool MotionBlurEnabled(Camera camera)
		{
			#if UNITY_EDITOR
			bool allowMotionBlur = EditorApplication.isPlaying;
			#else
			const bool allowMotionBlur = true;
			#endif

			return allowMotionBlur && asset.enableMotionVectors && SystemInfo.supportsMotionVectors &&
				(camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView);
		}

		private bool CameraResourceExists(Camera camera, out CameraResource cameraResource)
		{
			foreach (var resource in cameraResources)
			{
				if (resource.camera == camera)
				{
					cameraResource = resource;
					return true;
				}
			}
			cameraResource = null;
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			//Lightmapping.ResetDelegate();

			// shadows
			/*if (shadowTextures != null)
			{
				foreach (var shadowTexture in shadowTextures)
				{
					try
					{
						shadowTexture.Release();
					}
					catch { }

					try
					{
						GameObject.DestroyImmediate(shadowTexture);
					}
					catch { }
				}
				shadowTextures = null;
			}*/

			// camera resources
			foreach (var cameraResource in cameraResources)
			{
				cameraResource.ReleaseBuffers(true);
			}
			cameraResources.Clear();

			// command buffer
			if (disposing)
			{
				if (cmd != null)
				{
					cmd.Release();
					cmd = null;
				}
			}

			// base
			base.Dispose(disposing);
		}

		/*private void PostProcess(ref ScriptableRenderContext context, Reign_PostProcessResources resources, Reign_PostProcess[] postProcesses, RenderTexture renderTextureSrcStart, RenderTexture renderTextureDstStart, out RenderTexture finalRenderTexture)
		{
			// Editor needs to refresh each frame in case changes are made
			#if UNITY_EDITOR
			postProcesses = null;
			var camera = resources.camera;
			if (camera.cameraType == CameraType.SceneView)
			{
				var sceneCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
				if (sceneCameraObject != null)
				{
					var sceneCamera = sceneCameraObject.GetComponent<Camera>();
					if (sceneCamera != null) postProcesses = sceneCameraObject.GetComponents<Reign_PostProcess>();
				}
			}
			else if (camera.cameraType == CameraType.Game)
			{
				postProcesses = camera.GetComponents<Reign_PostProcess>();
			}

			if (postProcesses == null)
			{
				finalRenderTexture = renderTextureSrcStart;
				return;
			}
			#endif

			foreach (var postProcess in postProcesses)
			{
				if (!postProcess.enabled || !postProcess.IsSupported(resources)) continue;
				#if UNITY_EDITOR
				if (!postProcess.previewInSceneView && resources.camera.cameraType == CameraType.SceneView) continue;
				#endif
				postProcess.OnPostProcess(resources, cmd, ref context, renderTextureSrcStart, renderTextureDstStart);
				var src = renderTextureSrcStart;
				renderTextureSrcStart = renderTextureDstStart;
				renderTextureDstStart = src;
			}
			finalRenderTexture = renderTextureSrcStart;
		}

		private void CopyTexture(Texture srcTexture, RenderTextureSubElement srcElement, int srcMipLvl, RenderTexture dstTexture, int dstMipLvl, Material copyMaterial, int copyMaterialPass)
		{
			var blitMesh = Reign_PostProcess.GetBlitMesh();
			cmd.SetGlobalVector("srcRect", new Vector4(0, 0, 1, 1));
			cmd.SetGlobalVector("dstRect", new Vector4(0, 0, 1, 1));
			cmd.SetGlobalFloat("srcMipLvl", srcMipLvl);
			cmd.SetRenderTarget(dstTexture, dstMipLvl);
			cmd.SetGlobalTexture("_SrcTex", srcTexture, srcElement);
			cmd.DrawMesh(blitMesh, Matrix4x4.identity, copyMaterial, 0, copyMaterialPass);
		}

		private void BlurRoughnessTexture(Texture srcTexture, RenderTextureSubElement srcElement, RenderTexture screenSpaceTexture, RenderTexture screenSpaceTextureTEMP, Material roughnessBlurMaterial)
		{
			// blur mip levels
			var blitMesh = Reign_PostProcess.GetBlitMesh();
			cmd.SetGlobalFloat("srcMipLvl", 0);
			cmd.SetRenderTarget(screenSpaceTexture, 0);
			cmd.SetGlobalTexture("_SrcTex", srcTexture, srcElement);
			cmd.DrawMesh(blitMesh, Matrix4x4.identity, roughnessBlurMaterial, 0, 0);
			int mipWidth = screenSpaceTexture.width / 2;// start at half size
			int mipHeight = screenSpaceTexture.height / 2;
			for (int i = 1; i < screenSpaceTexture.mipmapCount; ++i)
			{
				// size-down
				cmd.SetGlobalFloat("srcMipLvl", i - 1);
				cmd.SetRenderTarget(screenSpaceTextureTEMP, i);
				cmd.SetGlobalTexture("_SrcTex", screenSpaceTexture);
				cmd.DrawMesh(blitMesh, Matrix4x4.identity, roughnessBlurMaterial, 0, 0);

				// blur X
				cmd.SetGlobalFloat("srcMipLvl", i);
				cmd.SetRenderTarget(screenSpaceTexture, i);
				cmd.SetGlobalTexture("_SrcTex", screenSpaceTextureTEMP);
				cmd.SetGlobalVector("_SrcTex_ST", new Vector4(1f / mipWidth, 1f / mipHeight, mipWidth, mipHeight));
				cmd.DrawMesh(blitMesh, Matrix4x4.identity, roughnessBlurMaterial, 0, 1);

				mipWidth /= 2;
				mipHeight /= 2;
			}
		}*/
    }
}