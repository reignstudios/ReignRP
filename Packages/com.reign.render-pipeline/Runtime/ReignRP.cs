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
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reign.SRP
{
    public sealed partial class ReignRP : RenderPipeline
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
		public const string lightModeID_Refractive = "Reign_RefractiveSS";
        public const string lightModeID_Transparent = "Reign_Transparent";

        public static ReignRP singleton { get; private set; }
		private ReignRP_Asset asset;

		#if !UNITY_EDITOR
		private int fullscreenSwapchainResolutionDivision;
		#endif

		private CommandBuffer cmd;
        private ShaderVars shaderVars;
        private CameraDataComparer cameraDataComparer = new CameraDataComparer();
        private List<CameraResource> cameraResources = new List<CameraResource>();

		private Vector3 directionalLight_Position;
		private Quaternion directionalLight_Rotation;
		private Vector4 directionalLight_Direction, directionalLight_Color;
		private float directionalLight_Bias;

		private const int pointLight_MaxConst = 4;
		private int pointLight_Max;
		private Vector4[] pointLight_Positions, pointLight_Colors, pointLight_Flags;
		private Vector4[] pointLight_Positions_Const, pointLight_Colors_Const, pointLight_Flags_Const;
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
		public static bool msaaTextureLoadSupported { get; private set; }
		public static bool msaaSwapChainSupported { get; private set; }

		public static bool refreshPostProcessState = true;

		private XRDisplaySubsystem xrSubsystem;
		private List<XRDisplaySubsystem> xrSubsystemList;
		private XRRenderPassInfo xrRenderPassInfo = new XRRenderPassInfo();
		public static bool xrActive => singleton != null ? singleton.xrRenderPassInfo.isXRActive : false;
		private Matrix4x4[] stereoMatrices = new Matrix4x4[2];
		private Vector4[] stereoVectors = new Vector4[2];

		public delegate void CustomDraw(Camera camera, CommandBuffer cmd, in ScriptableRenderContext context, in CullingResults cullResults);
		public static event CustomDraw DrawCustom_PreOpaque, DrawCustom_PostOpaque;
		public static event CustomDraw DrawCustom_PreRefractive, DrawCustom_PostRefractive;
		public static event CustomDraw DrawCustom_PreTransparent, DrawCustom_PostTransparent;

		public ReignRP(ReignRP_Asset asset)
		{
			singleton = this;
			this.asset = asset;

			// set graphic defaults
			GraphicsSettings.useScriptableRenderPipelineBatching = false;
			GraphicsSettings.lightsUseLinearIntensity = true;
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
			msaaTextureLoadSupported = SystemInfo.supportsMultisampledTextures > 0 && !asset.compositionMSAA_ForceHardwareResolve;
			msaaSwapChainSupported = SystemInfo.supportsMultisampledBackBuffer;
		}

		private bool CheckResourceInit()
		{
			try
			{
				// blit resources
				BlitMesh.InitCheck();

				//if (!skyboxMesh) skyboxMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

				if (!asset.resources.shaders.blitShader) throw new Exception("Missing Blit Shader");
				if (!blitMaterial) blitMaterial = new Material(asset.resources.shaders.blitShader);

				// allocate render-path specific light buffers
				if (asset.maxLights < 0) pointLight_Max = 1024;
				else pointLight_Max = Math.Max(0, asset.maxLights);
				if (pointLight_Positions == null || pointLight_Positions.Length != pointLight_Max)
				{
					pointLight_Positions = new Vector4[pointLight_Max];
					pointLight_Colors = new Vector4[pointLight_Max];
					pointLight_Flags = new Vector4[pointLight_Max];
					pointLight_Distances = new float[pointLight_Max];
				}

				if (pointLight_Positions_Const == null)
				{
					pointLight_Positions_Const = new Vector4[pointLight_MaxConst];
					pointLight_Colors_Const = new Vector4[pointLight_MaxConst];
					pointLight_Flags_Const = new Vector4[pointLight_MaxConst];
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return false;
			}

			return true;
        }
		
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
			#if UNITY_EDITOR
			refreshPostProcessState = true;// force refresh in editor each frame
			#endif

			// ensure asset settings are valid
			asset.ValidateSettings();

			// MSAA
			if (asset.enableComposition)// always disable swap-buffer MSAA in compositing
			{
				if (QualitySettings.antiAliasing != 1)
				{
					QualitySettings.antiAliasing = 1;
					Screen.SetMSAASamples(1);
				}

				if (XRSettings.enabled)
				{
					var xrTargetDesc = XRSettings.eyeTextureDesc;
					if (XRSystem.GetDisplayMSAASamples() != MSAASamples.None || xrTargetDesc.msaaSamples != 1)
					{
						xrTargetDesc.msaaSamples = 1;
						XRSystem.SetDisplayMSAASamples(MSAASamples.None);
					}
				}
			}
			else// handle target situations
			{
				if (XRSettings.enabled)
				{
					// always disable preview MSAA in compositing
					if (QualitySettings.antiAliasing != 1)
					{
						QualitySettings.antiAliasing = 1;
						Screen.SetMSAASamples(1);
					}

					var xrTargetDesc = XRSettings.eyeTextureDesc;
					if (XRSystem.GetDisplayMSAASamples() != (MSAASamples)asset.msaa || xrTargetDesc.msaaSamples != (int)asset.msaa)
					{
						XRSystem.SetDisplayMSAASamples(MSAASamples.None);
						xrTargetDesc.msaaSamples = 1;
					}
				}
				else if (msaaSwapChainSupported)
				{
					if (QualitySettings.antiAliasing != (int)asset.msaa)
					{
						QualitySettings.antiAliasing = (int)asset.msaa;
						Screen.SetMSAASamples((int)asset.msaa);
					}
				}
			}

			// scale
			if (XRSettings.enabled)
			{
				if (XRSettings.eyeTextureResolutionScale != asset.xrTargetScale)
				{
					XRSettings.eyeTextureResolutionScale = asset.xrTargetScale;
					XRSystem.SetRenderScale(asset.xrTargetScale);
				}
			}

			// validate swap-chain resolution TODO
			/*#if !UNITY_EDITOR
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
			#endif*/

			// check if common resources init
			if (!CheckResourceInit()) return;

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

            // set shader vars
			cmd.Clear();
            SetShaderTimeValues(cmd, Time.time, Time.deltaTime, Time.smoothDeltaTime);
			cmd.SetGlobalVector("randoValues", new Vector4(UnityEngine.Random.value * 12.9898f, UnityEngine.Random.value * 78.233f, UnityEngine.Random.value * 43.849f, UnityEngine.Random.value * 43758.5453123f));
			cmd.SetGlobalTexture("_DitherTex", asset.resources.textures.ditherTexture);
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
					if (xrSubsystemList == null) xrSubsystemList = new List<XRDisplaySubsystem>();
                    if (xrSubsystemList.Count == 0)
					{
						SubsystemManager.GetSubsystems(xrSubsystemList);
					}
                    else
                    {
                        xrSubsystem = xrSubsystemList[0];
						if (xrSubsystem.foveatedRenderingLevel != asset.xrFoveatedRenderingLevel) xrSubsystem.foveatedRenderingLevel = asset.xrFoveatedRenderingLevel;
						int renderPassCount = xrSubsystem.GetRenderPassCount();
                        if (renderPassCount > 0)
                        {
							xrRenderPassInfo.isXRActive = true;
							if (renderPassCount == 2)
							{
								cmd.Clear();
								cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
								cmd.DisableShaderKeyword("STEREO_MULTIVIEW_ON");
								context.ExecuteCommandBuffer(cmd);

								xrRenderPassInfo.renderPassCount = renderPassCount;
								for (int i = 0; i != renderPassCount; ++i)
								{
									xrRenderPassInfo.eyePass = i;
									xrRenderPassInfo.passIndex = i;

									// get multi-pass info
									xrSubsystem.GetRenderPass(i, out xrRenderPassInfo.pass[i]);
									xrRenderPassInfo.pass[i].GetRenderParameter(camera, 0, out xrRenderPassInfo.parameter[i]);

									// configure camera matrix
									ref var parameter = ref xrRenderPassInfo.parameter[xrRenderPassInfo.passIndex];
									camera.worldToCameraMatrix = parameter.view;
									camera.projectionMatrix = parameter.projection;

									// setup multi-pass camera
									context.StartMultiEye(camera, i);

									// render scene
									RenderPass(ref context, camera, i == 0 && asset.shadowType != ShadowType.Off);

									// stop multi-pass camera
									context.StopMultiEye(camera);
									context.StereoEndRender(camera, i, i == renderPassCount - 1);
								}
							}
							else
							{
								xrRenderPassInfo.renderPassCount = 1;
								xrRenderPassInfo.eyePass = -1;
								xrRenderPassInfo.passIndex = 0;

								// get single-pass info
								xrSubsystem.GetRenderPass(0, out xrRenderPassInfo.pass[0]);// force both passes as the same (as there is only 1)
								xrRenderPassInfo.pass[1] = xrRenderPassInfo.pass[0];
								for (int i = 0; i != 2; ++i) xrRenderPassInfo.pass[i].GetRenderParameter(camera, i, out xrRenderPassInfo.parameter[i]);// get both eye parameters

								// configure camera matrix
								ref var parameterLeft = ref xrRenderPassInfo.parameter[0];
								ref var parameterRight = ref xrRenderPassInfo.parameter[1];
								var pLeft = GL.GetGPUProjectionMatrix(parameterLeft.projection, false);
								var pRight = GL.GetGPUProjectionMatrix(parameterRight.projection, false);
								camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, parameterLeft.view);
								camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, parameterRight.view);

								camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, pLeft);
								camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, pRight);

								cmd.Clear();
								if (SystemInfo.supportsMultiview)
								{
									cmd.EnableShaderKeyword("STEREO_MULTIVIEW_ON");
									cmd.SetInstanceMultiplier(1);
								}
								else
								{
									cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
									cmd.SetInstanceMultiplier(2);
								}

								stereoMatrices[0] = parameterLeft.view;
								stereoMatrices[1] = parameterRight.view;
								cmd.SetGlobalMatrixArray("unity_StereoMatrixV", stereoMatrices);

								stereoMatrices[0] = pLeft;
								stereoMatrices[1] = pRight;
								cmd.SetGlobalMatrixArray("unity_StereoMatrixP", stereoMatrices);

								stereoMatrices[0] = pLeft * parameterLeft.view;
								stereoMatrices[1] = pRight * parameterRight.view;
								cmd.SetGlobalMatrixArray("unity_StereoMatrixVP", stereoMatrices);

								stereoVectors[0] = parameterLeft.view.inverse.GetColumn(3);
								stereoVectors[1] = parameterRight.view.inverse.GetColumn(3);
								cmd.SetGlobalVectorArray("unity_StereoWorldSpaceCameraPos", stereoVectors);
								context.ExecuteCommandBuffer(cmd);

								// setup single-pass camera
								context.StartMultiEye(camera);

								// render scene
								RenderPass(ref context, camera, asset.shadowType != ShadowType.Off);

								// stop single-pass camera
								context.StopMultiEye(camera);
								context.StereoEndRender(camera);
							}
                        }
                    }
                }
                else
                {
					cmd.Clear();
					cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
					cmd.DisableShaderKeyword("STEREO_MULTIVIEW_ON");
					cmd.SetInstanceMultiplier(1);
					context.ExecuteCommandBuffer(cmd);

					xrRenderPassInfo.isXRActive = false;
					xrRenderPassInfo.renderPassCount = 0;
					xrRenderPassInfo.eyePass = -1;
					xrRenderPassInfo.passIndex = 0;
                    RenderPass(ref context, camera, asset.shadowType != ShadowType.Off);// non-XR single eye pass
                }
                EndCameraRendering(context, camera);
            }
			
            // render scene
            EndContextRendering(context, cameras);
			refreshPostProcessState = false;// stop refresh
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

						current = pointLight_Flags[i];
                        pointLight_Flags[i] = pointLight_Flags[i2];
                        pointLight_Flags[i2] = current;
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
					pointLight_Flags_Const[i] = pointLight_Flags[i];
                }
                else
                {
                    pointLight_Positions_Const[i] = Vector4.zero;
                    pointLight_Colors_Const[i] = Vector4.zero;
					pointLight_Flags_Const[i] = Vector4.zero;
                }
            }
		}

		private void RenderPass(ref ScriptableRenderContext context, Camera camera, bool renderShadows)
		{
			// find or allocate camera resources
			CameraResource cameraResource = null;
			if (!CameraResourceExists(camera, out cameraResource))
			{
				cameraResource = new CameraResource(camera, this);
				cameraResources.Add(cameraResource);
			}
			cameraResource.UpdateStart();

			// get max shadow plane
			/*float maxShadowPlane = 0;
			if (asset.shadowType != ShadowType.Off)
			{
				switch (asset.shadowCascades)
				{
					case ShadowCascades.x1: maxShadowPlane = asset.shadowCascadePlanes.x; break;
					case ShadowCascades.x2: maxShadowPlane = asset.shadowCascadePlanes.y; break;
					case ShadowCascades.x3: maxShadowPlane = asset.shadowCascadePlanes.z; break;
					case ShadowCascades.x4: maxShadowPlane = asset.shadowCascadePlanes.w; break;
				}
			}*/

			// standard camera prep
			CameraPrep(ref context, camera, out var cullResults, out var cullingParameters);//, maxShadowPlane);

			// setup camera special data mode
            var depthTextureMode = DepthTextureMode.None;
            var specialRenderParams = PerObjectData.None;
			if (asset.enableReflectionProbes) specialRenderParams |= PerObjectData.ReflectionProbes;
			if (asset.enableLightmaps) specialRenderParams |= PerObjectData.Lightmaps;
            if (motionBlurEnabled)
            {
                depthTextureMode |= DepthTextureMode.MotionVectors;
                specialRenderParams |= PerObjectData.MotionVectors;
            }
            if ((camera.depthTextureMode & depthTextureMode) == 0) camera.depthTextureMode = depthTextureMode;

			// process culled lights
			var lights = cullResults.visibleLights;
			int directionalLight_Count = 0;
			int pointLight_Count = 0;
			foreach (var light in lights)
			{
				var l = light.light;
				var bakeType = l.bakingOutput.lightmapBakeType;
				if (bakeType == LightmapBakeType.Baked) continue;// skip non-realtime

				float bakeFlag = bakeType == LightmapBakeType.Mixed ? 1 : 0;
				switch (light.lightType)
				{
					case LightType.Directional:
						if (directionalLight_Count < 1)
						{
							var t = l.transform;

							directionalLight_Position = t.position;
							directionalLight_Rotation = t.rotation;

							directionalLight_Direction = t.forward;
							directionalLight_Direction.w = bakeFlag;// lightmap diffuse mode
							directionalLight_Color = light.finalColor;
							directionalLight_Color.w = l.intensity;
							directionalLight_Bias = l.shadowBias;
							directionalLight_Count++;
						}
						break;

					case LightType.Point:
						if (pointLight_Count < pointLight_Max)
						{
							var t = l.transform;

							pointLight_Positions[pointLight_Count] = t.position;
							pointLight_Positions[pointLight_Count].w = light.range;
							pointLight_Colors[pointLight_Count] = light.finalColor;
							pointLight_Colors[pointLight_Count].w = l.intensity;
							pointLight_Flags[pointLight_Count].w = bakeFlag;// lightmap diffuse mode
							pointLight_Count++;
						}
						break;
				}
			}
			
			// render shadows
			if (renderShadows) RenderShadowPass_Directional(ref context, camera);
			
			// configure camera after shadows
			SetCameraShaderProperties(ref context, camera);

			// apply lighting settings
			cmd.Clear();

			if (directionalLight_Count > 0)
			{
				cmd.SetGlobalVector("directionalLight_Direction", directionalLight_Direction);
				cmd.SetGlobalVector("directionalLight_Color", directionalLight_Color);
				cmd.SetGlobalFloat("directionalLight_Bias", directionalLight_Bias);
				cmd.DisableShaderKeyword("REIGN_DIRECTIONAL_LIGHTS_DISABLE");
			}
			else
			{
				cmd.EnableShaderKeyword("REIGN_DIRECTIONAL_LIGHTS_DISABLE");
			}

			if (pointLight_Count > 0)
			{
				if (asset.sortPointLights) SortPointLights(camera, pointLight_Count);
				else CopyPointLightsToConsts(pointLight_Count);
				cmd.SetGlobalVectorArray("pointLight_Positions", pointLight_Positions_Const);
				cmd.SetGlobalVectorArray("pointLight_Colors", pointLight_Colors_Const);
				cmd.SetGlobalVectorArray("pointLight_Flags", pointLight_Flags_Const);
				cmd.SetGlobalFloat("pointLight_Count", Math.Min(pointLight_Count, pointLight_MaxConst));
				cmd.DisableShaderKeyword("REIGN_POINT_LIGHTS_DISABLE");
			}
			else
			{
				cmd.EnableShaderKeyword("REIGN_POINT_LIGHTS_DISABLE");
			}

			switch (asset.shadowType)
			{
				case ShadowType.Off:
					cmd.DisableShaderKeyword("REIGN_SHADOW_HARD");
					cmd.DisableShaderKeyword("REIGN_SHADOW_SOFT_BLUR");
					break;

				case ShadowType.Hard:
					cmd.EnableShaderKeyword("REIGN_SHADOW_HARD");
					cmd.DisableShaderKeyword("REIGN_SHADOW_SOFT_BLUR");
					break;

				case ShadowType.SoftBlur:
					cmd.DisableShaderKeyword("REIGN_SHADOW_HARD");
					cmd.EnableShaderKeyword("REIGN_SHADOW_SOFT_BLUR");
					break;
			}

			if (asset.shadowType != ShadowType.Off) cmd.SetGlobalVector("shadowColor", RenderSettings.subtractiveShadowColor);

			SetAmbient();
			context.ExecuteCommandBuffer(cmd);

			// set special data
			cmd.Clear();
            if (cameraResource.enableComposition) cmd.SetGlobalVector("targetSize", new Vector4(1.0f / cameraResource.widthComposited, 1.0f / cameraResource.heightComposited, cameraResource.widthComposited, cameraResource.heightComposited));
            else cmd.SetGlobalVector("targetSize", new Vector4(1.0f / cameraResource.widthTarget, 1.0f / cameraResource.heightTarget, cameraResource.widthTarget, cameraResource.heightTarget));
            cmd.SetGlobalMatrix("clipToWorld", cameraResource.clipToWorld);
			context.ExecuteCommandBuffer(cmd);
			context.Submit();

			// start opaque render pass
			bool seperateTransparentPass = cameraResource.enableComposition && (asset.compositionColorClone || asset.compositionDepthClone);
			StartRenderPass(context, cameraResource.renderPass_Opaque, cameraResource, false, renderShadows);
			DrawOpaque(camera, ref context, ref cullResults, specialRenderParams);
			if (!seperateTransparentPass)
			{
				cameraResource.SetFakeCompositedTextures(cmd);// set fake textures for compositing shaders
				DrawRefractive(camera, ref context, ref cullResults, specialRenderParams);
				DrawTransparent(camera, ref context, ref cullResults, specialRenderParams);
			}
			EndRenderPass(context);

			// enable depth-texture to be sampled
			if (seperateTransparentPass)
			{
				cmd.Clear();
				if (asset.compositionColorClone) cameraResource.ResolveCompositedColorTexture(cmd);
				if (asset.compositionDepthClone) cameraResource.ResolveCompositedDepthTexture(cmd);
				context.ExecuteCommandBuffer(cmd);
				context.Submit();

				// start transparent render pass
				StartRenderPass(context, cameraResource.renderPass_Transparent, cameraResource, true, renderShadows);
				DrawRefractive(camera, ref context, ref cullResults, specialRenderParams);
				DrawTransparent(camera, ref context, ref cullResults, specialRenderParams);
				EndRenderPass(context);
			}

			// compositing
			if (cameraResource.enableComposition)
			{
				Mesh blitMesh;
				#if UNITY_EDITOR
				if (isOpenGL)
				{
					blitMesh = BlitMesh.mesh;
				}
				else
				{
					blitMesh = (camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview) ? BlitMesh.meshFlipped : BlitMesh.mesh;
				}
				#else
				blitMesh = BlitMesh.mesh;
				#endif
				
				// grab initial target
				var finalTexture = cameraResource.colorTexture;
				int postProcessCount = cameraResource.postProcesses != null ? cameraResource.postProcesses.Length : 0;
				
				// pre-resolve MSAA texture ONLY if needed
				bool msaaResolved = false;
				if (cameraResource.msaa != MSAA_Level.Off)
				{
					if (!msaaTextureLoadSupported || postProcessCount != 0)// resolve if MSAA-Load not supported or PostProcess tasks are needed
					{
						cmd.Clear();
						cameraResource.ResolveCompositedMSAATexture(cmd, finalTexture, cameraResource.compositingTextures[0]);
						finalTexture = cameraResource.compositingTextures[0];
						msaaResolved = true;
						context.ExecuteCommandBuffer(cmd);
					}
				}
				else
				{
					msaaResolved = true;// consider resolved if MSAA not used
				}
				
				// Post-Processing
				if (postProcessCount != 0)
				{
					var postProcessSrc = finalTexture;
					var postProcessDst = cameraResource.compositingTextures[1];
					int compositingIndex = 0;
					foreach (var postProcess in cameraResource.postProcesses)
					{
						if (!postProcess.enabled || !postProcess.IsSupported(cameraResource.postProcessResources)) continue;

						#if UNITY_EDITOR
						if (!postProcess.previewInSceneView && camera.cameraType == CameraType.SceneView) continue;
						#endif

						postProcess.OnPostProcess(cameraResource.postProcessResources, cmd, context, postProcessSrc, postProcessDst);
						compositingIndex = 1 - compositingIndex;
						postProcessSrc = postProcessDst;
						postProcessDst = cameraResource.compositingTextures[compositingIndex];
					}
					finalTexture = postProcessSrc;
				}
				
				// copy final result
				cmd.Clear();
				if (msaaResolved)
				{
					Blit(finalTexture, cameraResource.cameraTargetTextureID, blitMesh:blitMesh);
				}
				else
				{
					var blitMode = BlitMode.Load;
					switch (cameraResource.msaa)
					{
						case MSAA_Level.X2: blitMode = BlitMode.MSAA_2X; break;
						case MSAA_Level.X4: blitMode = BlitMode.MSAA_4X; break;
						case MSAA_Level.X8: blitMode = BlitMode.MSAA_8X; break;
						default: Debug.LogError("Invalid MSAA BlitMode: " + cameraResource.msaa); break;
					}
					Blit(finalTexture, cameraResource.cameraTargetTextureID, blitMesh:blitMesh, mode:blitMode);
				}
				context.ExecuteCommandBuffer(cmd);
			}
			
			// XR
			#if UNITY_EDITOR || UNITY_STANDALONE
			if (xrRenderPassInfo.isXRActive && asset.xrPreview)
			{
				bool copyPreview = false;
				var eye = XRSettings.gameViewRenderMode;
				var viewport = camera.pixelRect;
				if (xrRenderPassInfo.eyePass < 0)
				{
					viewport = camera.pixelRect;
					copyPreview = eye == GameViewRenderMode.BothEyes;
				}
				else
				{
					viewport = camera.pixelRect;
					if (xrRenderPassInfo.eyePass == 0)
					{
						if (eye == GameViewRenderMode.BothEyes)
						{
							viewport.x = 0;
							viewport.width /= 2;
							copyPreview = true;
						}
						else if (eye == GameViewRenderMode.LeftEye)
						{
							copyPreview = true;
						}
					}
					else
					{
						if (eye == GameViewRenderMode.BothEyes)
						{
							viewport.x = viewport.width / 2;
							viewport.width /= 2;
							copyPreview = true;
						}
						else if (eye == GameViewRenderMode.RightEye)
						{
							copyPreview = true;
						}
					}

					if (eye == GameViewRenderMode.OcclusionMesh) copyPreview = true;
				}

				if (copyPreview)
				{
					cmd.Clear();
					Blit(cameraResource.cameraTargetTextureID, BuiltinRenderTextureType.None, viewport:viewport, blitMesh:BlitMesh.meshFlipped);
					context.ExecuteCommandBuffer(cmd);
				}
			}
			#endif

			// standard camera finish
			CameraFinish(ref context, camera, ref cullResults);
			
			// release temp resources
			cameraResource.UpdateEnd();
            context.Submit();
        }

		private void DrawOpaque(Camera camera, ref ScriptableRenderContext context, ref CullingResults cullResults, PerObjectData specialRenderParams)
		{
			// draw custom pre-opaque objects
			DrawCustom_PreOpaque?.Invoke(camera, cmd, context, cullResults);

			// draw custom opaque objects
			DrawCustomUnlitObjects(ref context, ref cullResults, QueueRange.Opaque, camera);

			// draw opaque objects
			DrawObjects(ref context, ref cullResults, lightModeID_Opaque, QueueRange.Opaque, camera, null, objectData:specialRenderParams);

			// draw custom post-opaque objects
			DrawCustom_PostOpaque?.Invoke(camera, cmd, context, cullResults);

			// clear skybox (after opaque)
            ClearSkybox(ref context, camera);
		}
		
		private void DrawRefractive(Camera camera, ref ScriptableRenderContext context, ref CullingResults cullResults, PerObjectData specialRenderParams)
		{
			// draw custom pre-refractive objects
			DrawCustom_PreRefractive?.Invoke(camera, cmd, context, cullResults);

			// draw refractive objects
			DrawObjects(ref context, ref cullResults, lightModeID_Refractive, QueueRange.Opaque, camera, null, objectData:specialRenderParams);

			// draw custom post-refractive objects
			DrawCustom_PostRefractive?.Invoke(camera, cmd, context, cullResults);
		}

		private void DrawTransparent(Camera camera, ref ScriptableRenderContext context, ref CullingResults cullResults, PerObjectData specialRenderParams)
		{
			// draw custom pre-transparent objects
            DrawCustom_PreTransparent?.Invoke(camera, cmd, context, cullResults);

			// draw custom transparent objects
			DrawCustomUnlitObjects(ref context, ref cullResults, QueueRange.Transparent, camera);

			// draw transparent objects
			DrawObjects(ref context, ref cullResults, lightModeID_Transparent, QueueRange.Transparent, camera, null, objectData:specialRenderParams);

			// draw custom post-transparent objects
			DrawCustom_PostTransparent?.Invoke(camera, cmd, context, cullResults);

			// draw unuspported objects & editor gizmos
			DrawErrorObjectsAndPreGizmos(ref context, ref cullResults, camera);
		}

		private void StartRenderPass(in ScriptableRenderContext context, in RenderPassDesc renderPassDesc, CameraResource cameraResource, bool transparentPass, bool renderShadows)
		{
			// get binding slice
			int slice = 0;
			if (xrRenderPassInfo.isXRActive)
			{
				if (xrRenderPassInfo.eyePass < 0) slice = RenderTargetIdentifier.AllDepthSlices;
				else slice = xrRenderPassInfo.parameter[xrRenderPassInfo.passIndex].textureArraySlice;
			}

			// configure
			var camera = cameraResource.camera;
            if (asset.useRenderPasses)
            {
				if (asset.renderPassesMultiCameraClear)
				{
					cmd.Clear();

					// enable targets
					if (renderPassDesc.renderTargets.Length >= 2) cmd.SetRenderTarget(renderPassDesc.renderTargets, renderPassDesc.renderTarget_Depth, 0, CubemapFace.Unknown, slice);
					else cmd.SetRenderTarget(renderPassDesc.renderTarget_First, renderPassDesc.renderTarget_Depth, 0, CubemapFace.Unknown, slice);

					// set viewport
					cmd.SetViewport(cameraResource.viewport);

					// clear
					ClearRenderPass(renderPassDesc);

					// invoke
					context.ExecuteCommandBuffer(cmd);
					context.Submit();
				}

				// start render pass
                context.BeginRenderPass(renderPassDesc.width, renderPassDesc.height, renderPassDesc.msaaSamples, renderPassDesc.attachments, renderPassDesc.depthIndex);
                context.BeginSubPass(renderPassDesc.attachmentIndices);

				// prep
				cmd.Clear();
				if (renderShadows) cmd.SetGlobalTexture("_ShadowTex", shadowTextureID, RenderTextureSubElement.Depth);// enable shadow texture
				cmd.SetViewport(cameraResource.viewport);// set viewport
				if (!transparentPass && xrRenderPassInfo.isXRActive) DrawOcclusionMesh(cameraResource);// draw occlusion mesh
				context.ExecuteCommandBuffer(cmd);
			}
			else
			{
				cmd.Clear();

				// foveated rendering
				if (!transparentPass)
				{
					if (xrRenderPassInfo.pass[xrRenderPassInfo.passIndex].foveatedRenderingInfo != IntPtr.Zero)
					{
						cmd.ConfigureFoveatedRendering(xrRenderPassInfo.pass[xrRenderPassInfo.passIndex].foveatedRenderingInfo);
						cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
					}
					else
					{
						cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
					}
				}
				
				// enable targets
				if (renderPassDesc.renderTargets.Length >= 2) cmd.SetRenderTarget(renderPassDesc.renderTargets, renderPassDesc.renderTarget_Depth, 0, CubemapFace.Unknown, slice);
				else cmd.SetRenderTarget(renderPassDesc.renderTarget_First, renderPassDesc.renderTarget_Depth, 0, CubemapFace.Unknown, slice);
				
				// enable shadow texture
				if (renderShadows) cmd.SetGlobalTexture("_ShadowTex", shadowTextureID, RenderTextureSubElement.Depth);

				// set viewport
                cmd.SetViewport(cameraResource.viewport);

                // clear
				ClearRenderPass(renderPassDesc);

				// draw occlusion mesh
				if (!transparentPass && xrRenderPassInfo.isXRActive) DrawOcclusionMesh(cameraResource);

				context.ExecuteCommandBuffer(cmd);
			}
        }

		private void ClearRenderPass(in RenderPassDesc renderPassDesc)
		{
			// clear color and depth (NOTE: only clear with first color-targets clear color for performance)
			bool clearColor = false, clearDepth = false;
			var firstClearColor = Color.clear;
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
						firstClearColor = target.backgroundColor;
					}
				}
			}

			if (clearColor || clearDepth)
			{
				cmd.ClearRenderTarget(clearDepth, clearColor, firstClearColor);
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

		private void CameraPrep(ref ScriptableRenderContext context, Camera camera, out CullingResults cullResults, out ScriptableCullingParameters cullingParameters)//, float shadowDistance)
		{
			// allow UI scene objects to be culled
			#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView) ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			#endif
			
			// get camera culled objects
			if (xrRenderPassInfo.isXRActive) xrSubsystem.GetCullingParameters(camera, xrRenderPassInfo.pass[0].cullingPassIndex, out cullingParameters);
			else if (!camera.TryGetCullingParameters(false, out cullingParameters)) Debug.LogError("Failed: TryGetCullingParameters");
			cullingParameters.maximumVisibleLights = 1 + pointLight_Max;// directional + point
			cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;// disable shadow culling
			cullingParameters.shadowDistance = 0;//shadowDistance;
			cullResults = context.Cull(ref cullingParameters);
		}

		private void CameraFinish(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullResults)
		{
			// make sure editor camera target textures are enabled
			#if UNITY_EDITOR
			cmd.Clear();
			if (camera.targetTexture) cmd.SetRenderTarget(camera.targetTexture);
			else cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
			context.ExecuteCommandBuffer(cmd);

			// draw post gizmos
			if (camera.cameraType == CameraType.SceneView && Handles.ShouldRenderGizmos())
			{
				context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
			}
			#endif
		}

		private void SetCameraShaderProperties(ref ScriptableRenderContext context, Camera camera)
		{
			// configure XR or normal pass
			if (xrRenderPassInfo.isXRActive)
			{
				if (xrRenderPassInfo.eyePass >= 0)
				{
					context.SetupCameraProperties(camera, true, xrRenderPassInfo.passIndex);
				}
				else
				{
					context.SetupCameraProperties(camera, true);
				}
			}
			else
			{
				context.SetupCameraProperties(camera, false);
			}
		}

		private void DrawOcclusionMesh(CameraResource cameraResource)
		{
			// clip invisible pixels via depth
			cmd.DrawOcclusionMesh(new RectInt((int)cameraResource.viewport.x, (int)cameraResource.viewport.y, (int)cameraResource.viewport.width, (int)cameraResource.viewport.height));
		}

		private void ClearSkybox(ref ScriptableRenderContext context, Camera camera)
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

		private void SetAmbient()
		{
			cmd.DisableShaderKeyword("REIGN_AMBIENT_MODE_DISABLE");
			cmd.DisableShaderKeyword("REIGN_AMBIENT_MODE_SKYBOX");
			cmd.DisableShaderKeyword("REIGN_AMBIENT_MODE_GRADIENT");
			cmd.DisableShaderKeyword("REIGN_AMBIENT_MODE_COLOR");
			if (asset.ambientMode == GlobalAmbientMode.Disable)
			{
				cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_DISABLE");
			}
			else if (asset.ambientMode == GlobalAmbientMode.Unity_SceneSettings)
			{
				var ambientMode = RenderSettings.ambientMode;// custom = disabled
				if (ambientMode == AmbientMode.Skybox)
				{
					cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_SKYBOX");
					float a = RenderSettings.ambientIntensity;
					cmd.SetGlobalVector("unity_AmbientSky", new Vector4(a, a, a, 0));
				}
				else if (ambientMode == AmbientMode.Trilight)
				{
					cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_GRADIENT");
					cmd.SetGlobalVector("unity_AmbientSky", RenderSettings.ambientSkyColor);
					cmd.SetGlobalVector("unity_AmbientEquator", RenderSettings.ambientEquatorColor);
					cmd.SetGlobalVector("unity_AmbientGround", RenderSettings.ambientGroundColor);
				}
				else if (ambientMode == AmbientMode.Flat)
				{
					cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_COLOR");
					cmd.SetGlobalVector("unity_AmbientSky", RenderSettings.ambientSkyColor);
				}
			}
			else
			{
				if (asset.ambientMode == GlobalAmbientMode.ReignEnv_Sky)
				{
					cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_SKYBOX");
					float a = ReignRP_Environment.global_ambientIntensity;
					cmd.SetGlobalVector("unity_AmbientSky", new Vector4(a, a, a, 0));
				}
				else if (asset.ambientMode == GlobalAmbientMode.ReignEnv_Gradient)
				{
					cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_GRADIENT");
					cmd.SetGlobalVector("unity_AmbientSky", ReignRP_Environment.global_ambientGradient_SkyColor);
					cmd.SetGlobalVector("unity_AmbientEquator", ReignRP_Environment.global_ambientGradient_EquatorColor);
					cmd.SetGlobalVector("unity_AmbientGround", ReignRP_Environment.global_ambientGradient_GroundColor);
				}
				else if (asset.ambientMode == GlobalAmbientMode.ReignEnv_Color)
				{
					cmd.EnableShaderKeyword("REIGN_AMBIENT_MODE_COLOR");
					cmd.SetGlobalVector("unity_AmbientSky", ReignRP_Environment.global_ambientColor);
				}
			}
		}

		private void DrawErrorObjectsAndPreGizmos(ref ScriptableRenderContext context, ref CullingResults cullResults, Camera camera)
		{
			// draw editor objects
			#if UNITY_EDITOR
			// draw material error objects
			foreach (string errorID in errorIDs) DrawObjects(ref context, ref cullResults, errorID, QueueRange.Any, camera, overrideMaterial:errorMaterial);

			// draw pre gizmos
			if (camera.cameraType == CameraType.SceneView && Handles.ShouldRenderGizmos())
			{
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
			#endif
		}

		private void DrawCustomUnlitObjects(ref ScriptableRenderContext context, ref CullingResults cullResults, QueueRange range, Camera camera)
		{
			DrawObjects(ref context, ref cullResults, "SRPDefaultUnlit", range, camera);
		}

		private void DrawCustomUnlitObjects(ref ScriptableRenderContext context, ref CullingResults cullResults, Camera camera)
		{
			DrawObjects(ref context, ref cullResults, "SRPDefaultUnlit", QueueRange.Opaque, camera);
			DrawObjects(ref context, ref cullResults, "SRPDefaultUnlit", QueueRange.Transparent, camera);
		}

		public static void DrawObjectsCustom(ScriptableRenderContext context, CullingResults cullResults, string lightModeID, QueueRange range, Camera camera)
		{
			singleton.DrawObjects(ref context, ref cullResults, lightModeID, range, camera);
		}

		public static void DrawObjectsCustom(ScriptableRenderContext context, CullingResults cullResults, string lightModeID, QueueRange range, Camera camera, Shader overrideShader = null, int overrideMaterialPassIndex = 0, Material overrideMaterial = null, PerObjectData objectData = PerObjectData.None)
		{
			singleton.DrawObjects(ref context, ref cullResults, lightModeID, range, camera, overrideShader, overrideMaterialPassIndex, overrideMaterial, objectData);
		}

		private void DrawObjects(ref ScriptableRenderContext context, ref CullingResults cullResults, string lightModeID, QueueRange range, Camera camera, Shader overrideShader = null, int overrideMaterialPassIndex = 0, Material overrideMaterial = null, PerObjectData objectData = PerObjectData.None)
		{
			// filter settings
			var filterSettings = FilteringSettings.defaultValue;
			filterSettings.renderQueueRange = QueueRangeToRenderQueueRange(range, out var sortingCriteria);

			// draw settings
			var sortSettings = new SortingSettings(camera);
			sortSettings.criteria = sortingCriteria;
			var drawSettings = new DrawingSettings(new ShaderTagId(lightModeID), sortSettings);
			drawSettings.overrideMaterial = overrideMaterial;
			drawSettings.overrideShader = overrideShader;
			drawSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
			drawSettings.perObjectData = objectData;
			drawSettings.enableDynamicBatching = true;
			drawSettings.enableInstancing = true;

			// draw objects
			var renderListParams = new RendererListParams(cullResults, drawSettings, filterSettings);
			var renderContext = context.CreateRendererList(ref renderListParams);
			cmd.Clear();
			cmd.DrawRendererList(renderContext);
			context.ExecuteCommandBuffer(cmd);
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
    }
}