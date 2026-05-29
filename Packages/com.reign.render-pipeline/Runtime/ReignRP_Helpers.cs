using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Reign.SRP
{
    struct RenderPassDescTarget
    {
        public RenderTargetIdentifier renderTarget;
        public RenderTextureFormat renderTargetFormat;
        public int renderTargetDepth;
        public bool load, store;
        public bool clear;
        public Color backgroundColor;

        public RenderPassDescTarget(RenderTargetIdentifier renderTarget, RenderTextureFormat renderTargetFormat, int renderTargetDepth, bool load, bool store, bool clear, Color backgroundColor)
        {
            this.renderTarget = renderTarget;
            this.renderTargetFormat = renderTargetFormat;
            this.renderTargetDepth = renderTargetDepth;
            this.load = load;
            this.store = store;
            this.clear = clear;
            this.backgroundColor = backgroundColor;
        }

        public RenderPassDescTarget(RenderTargetIdentifier renderTarget, RenderTextureFormat renderTargetFormat, int renderTargetDepth, bool load, bool store)
        : this(renderTarget, renderTargetFormat, renderTargetDepth, load, store, false, Color.clear)
        { }

        public RenderPassDescTarget(RenderTexture renderTexture, bool load, bool store)
        : this(renderTexture, renderTexture.format, renderTexture.depth, load, store, false, Color.clear)
        { }
    }

    struct RenderPassDesc
    {
        public bool isInit { get; private set; }

        public int width, height;
        public RenderTargetIdentifier renderTarget_First, renderTarget_Depth;
        public RenderTargetIdentifier[] renderTargets;
        public RenderPassDescTarget[] targets;
        public NativeArray<AttachmentDescriptor> attachments;
        public NativeArray<int> attachmentIndices;
        public int[] renderTargetMappings;
        public int firstIndex, depthIndex;
        public int msaaSamples;

        public RenderPassDesc(int width, int height, RenderPassDescTarget[] targets, int msaaSamples)
        {
            isInit = true;

            this.width = width;
            this.height = height;
            this.targets = targets;
            renderTarget_First = BuiltinRenderTextureType.None;
            renderTarget_Depth = BuiltinRenderTextureType.None;
            firstIndex = -1;
            depthIndex = -1;
            this.msaaSamples = msaaSamples;

            // check if depth index exists
            for (int i = 0; i != targets.Length; ++i)
            {
                if (targets[i].renderTargetDepth >= 1)
                {
                    depthIndex = i;
                    break;
                }
            }

            // alloc resources
            bool firstSet = false;
            int renderTargetIndex = 0;
            int renderTargetCount = depthIndex >= 0 ? targets.Length - 1 : targets.Length;
            attachments = new NativeArray<AttachmentDescriptor>(targets.Length, Allocator.Persistent);
            renderTargets = new RenderTargetIdentifier[renderTargetCount];
            attachmentIndices = new NativeArray<int>(renderTargetCount, Allocator.Persistent);
            renderTargetMappings = new int[targets.Length];
            for (int i = 0; i != targets.Length; ++i)
            {
                ref var target = ref targets[i];

                var attchment = new AttachmentDescriptor(target.renderTargetFormat);
                attchment.ConfigureTarget(target.renderTarget, target.load, target.store);
                if (target.clear && !ReignRP_Asset.singleton.renderPassesMultiCameraClear) attchment.ConfigureClear(target.backgroundColor);
                attachments[i] = attchment;

                if (i != depthIndex)
                {
                    renderTargetMappings[i] = renderTargetIndex;
                    renderTargets[renderTargetIndex] = target.renderTarget;
                    attachmentIndices[renderTargetIndex] = i;
                    renderTargetIndex++;
                    if (!firstSet)
                    {
                        firstSet = true;
                        firstIndex = i;
                        renderTarget_First = target.renderTarget;
                    }
                }
                else
                {
                    renderTargetMappings[i] = -1;
                }

                if (i == depthIndex)
                {
                    renderTarget_Depth = target.renderTarget;
                }
            }
        }

        public void UpdateSize(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void UpdateTarget(RenderPassDescTarget target, int index)
        {
            int mappingIndex = renderTargetMappings[index];
            if (mappingIndex >= 0 && mappingIndex < renderTargets.Length) renderTargets[mappingIndex] = target.renderTarget;

            var attchment = new AttachmentDescriptor(target.renderTargetFormat);
            attchment.ConfigureTarget(target.renderTarget, true, true);
            if (target.clear && !ReignRP_Asset.singleton.renderPassesMultiCameraClear) attchment.ConfigureClear(target.backgroundColor);
            attachments[index] = attchment;
            targets[index] = target;
            if (firstIndex == index) renderTarget_First = target.renderTarget;
            if (depthIndex == index) renderTarget_Depth = target.renderTarget;
        }

        public void Dispose()
        {
            isInit = false;
            renderTargets = null;

            if (attachments.IsCreated)
            {
                attachments.Dispose();
            }

            if (attachmentIndices.IsCreated)
            {
                attachmentIndices.Dispose();
            }
        }
    }

    public partial class ReignRP
    {
        private struct ShaderVars
        {
            public int time, sinTime, cosTime, deltaTime, timeParams;
        }

		private class CameraDataComparer : IComparer<Camera>
        {
            public int Compare(Camera lhs, Camera rhs)
            {
                return (int)lhs.depth - (int)rhs.depth;
            }
        }

        private class CameraResource
		{
			public int frame;
			public readonly Camera camera;
			public readonly ReignRP pipeline;
			private readonly ReignRP_Asset asset;
            public bool enableComposition;

            public ReignRP_PostProcessResources postProcessResources;
            public ReignRP_PostProcess[] postProcesses;

            public RenderTexture cameraTargetTexture;
            public RenderTargetIdentifier cameraTargetTextureID, cameraTargetDepthTextureID;
            public RenderTextureFormat cameraTargetFormat;
            public int cameraTargetDepth;

            public RenderTexture depthTexture, depthTextureClone;
			public RenderTexture colorTexture, colorTextureClone;
            public RenderTexture[] compositingTextures;
			//public RenderTexture velocityTexture;

            public RenderTargetIdentifier depthTextureID, depthTextureCloneID;
			public RenderTargetIdentifier colorTextureID, colorTextureCloneID;
            public RenderTargetIdentifier[] compositingTexturesID;
            public RenderTargetIdentifier velocityTextureID;
            public RenderPassDesc renderPass_Opaque, renderPass_Transparent;
			public int widthTarget, heightTarget, widthComposited, heightComposited;
            public Rect viewport;
            public float texelWidth, texelHeight;
			public Matrix4x4 cameraViewProj_Last;
			public Matrix4x4 clipToWorld;

            public Matrix4x4 viewMat, projMat, viewProjMat;
            private CommonTextureFormat[] colorTextureFallbacks;

            public CameraResource(Camera camera, ReignRP pipeline)
			{
				this.camera = camera;
				this.pipeline = pipeline;
				asset = pipeline.asset;
                
                colorTextureFallbacks = new CommonTextureFormat[]
                {
                    CommonTextureFormat.Float_16,
                    CommonTextureFormat.UFloat_10,
                    CommonTextureFormat.UINT_A2_RGB10,
                };
            }

            private static RenderTextureFormat GetCompositionTextureFormat(CommonTextureFormat format, CommonTextureFormat[] fallbacks)
            {
                if (format == CommonTextureFormat.UINT_32) return RenderTextureFormat.ARGB32;// no need to test if this format is selected
                if (format == CommonTextureFormat.UINT_RGB565 || format == CommonTextureFormat.UINT_16)
                {
                    return GetTextureFormat(format);
                }
                else
                {
                    return GetTextureFormat(format, fallbacks);
                }
            }

            private int GetCompositedDepthBit()
            {
                int compositionDepthBit = (int)asset.compositionDepthBit;
                #if UNITY_EDITOR
                if (camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview) compositionDepthBit = 24;// assume 24
                #endif
                return compositionDepthBit;
            }

			public void UpdateStart()
			{
				frame = 0;
                enableComposition = asset.enableComposition && (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView);

				// calculate special matricies
				cameraViewProj_Last = camera.previousViewProjectionMatrix;
                viewMat = camera.worldToCameraMatrix;
                projMat = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                viewProjMat = projMat * viewMat;
                clipToWorld = viewProjMat.inverse;

                // camera target info
                var clearMode = camera.clearFlags;
                bool clearDepth = clearMode != CameraClearFlags.Nothing;
                bool clearColor = clearMode == CameraClearFlags.Color;
                Color backgroundColor = camera.backgroundColor;
                var xrRenderPassInfo = pipeline.xrRenderPassInfo;
                viewport = camera.rect;
                cameraTargetTexture = camera.targetTexture;
                if (!cameraTargetTexture)
                {
                    if (xrRenderPassInfo.isXRActive)
                    {
                        cameraTargetTextureID = xrRenderPassInfo.pass.renderTarget;
                        cameraTargetDepthTextureID = xrRenderPassInfo.pass.renderTarget;
                        cameraTargetFormat = xrRenderPassInfo.pass.renderTargetDesc.colorFormat;
                        cameraTargetDepth = xrRenderPassInfo.pass.renderTargetDesc.depthBufferBits;
                        widthTarget = xrRenderPassInfo.pass.renderTargetDesc.width;
                        heightTarget = xrRenderPassInfo.pass.renderTargetDesc.height;
                        viewport = xrRenderPassInfo.parameter.viewport;
                    }
                    else
                    {
                        cameraTargetTextureID = BuiltinRenderTextureType.CameraTarget;// swap-buffer
                        cameraTargetDepthTextureID = BuiltinRenderTextureType.Depth;// swap-buffer
                        cameraTargetFormat = asset.hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;// assume defaults
                        cameraTargetDepth = 24;// assume 24
                        widthTarget = Screen.width;
                        heightTarget = Screen.height;
                    }
                }
                else
                {
                    cameraTargetTextureID = cameraTargetTexture;
                    cameraTargetDepthTextureID = cameraTargetTexture;
                    cameraTargetFormat = cameraTargetTexture.format;
                    cameraTargetDepth = cameraTargetTexture.depth;
                    widthTarget = cameraTargetTexture.width;
                    heightTarget = cameraTargetTexture.height;
                    if (xrRenderPassInfo.isXRActive)
                    {
                        viewport = xrRenderPassInfo.parameter.viewport;
                    }
                }

                if (xrRenderPassInfo.isXRActive)
                {
                    camera.worldToCameraMatrix = xrRenderPassInfo.parameter.view;
				    camera.projectionMatrix = xrRenderPassInfo.parameter.projection;
                }

                // compositing
                if (enableComposition)
				{
                    widthComposited = widthTarget / asset.compositionDivision;
                    heightComposited = heightTarget / asset.compositionDivision;
                    
                    viewport.x *= widthComposited;
                    viewport.y *= heightComposited;
                    viewport.width *= widthComposited;
                    viewport.height *= heightComposited;
                    
                    texelWidth = 1f / widthComposited;
                    texelHeight = 1f / heightComposited;
                    
                    // depth texture
                    int compositionDepthBit = GetCompositedDepthBit();
                    var desc = new RenderTextureDescriptor(widthComposited, heightComposited, RenderTextureFormat.Depth, compositionDepthBit, 1);
                    desc.stencilFormat = GraphicsFormat.None;
                    desc.msaaSamples = (int)asset.compositionMSAA;
                    desc.bindMS = msaaTextureLoadSupported && asset.compositionMSAA != MSAA_Level.Off;
				    depthTexture = GetTemporaryRenderTexture(desc);
				    depthTextureID = depthTexture;
                    SetTextureSamplerState(depthTexture, FilterMode.Point, TextureWrapMode.Clamp);

                    // depth texture clone
                    if (asset.compositionDepthClone)
                    {
                        depthTextureClone = GetTemporaryRenderTexture(desc);
				        depthTextureCloneID = depthTextureClone;
                        SetTextureSamplerState(depthTextureClone, FilterMode.Point, TextureWrapMode.Clamp);
                    }

					// color texture
					desc = new RenderTextureDescriptor(widthComposited, heightComposited, GetCompositionTextureFormat(asset.compositionColorFormat, colorTextureFallbacks), 0, 1);
                    desc.stencilFormat = GraphicsFormat.None;
                    desc.msaaSamples = (int)asset.compositionMSAA;
                    desc.bindMS = msaaTextureLoadSupported && asset.compositionMSAA != MSAA_Level.Off;
					colorTexture = GetTemporaryRenderTexture(desc);
					colorTextureID = colorTexture;
                    SetTextureSamplerState(colorTexture, FilterMode.Point, TextureWrapMode.Clamp);
                    
                    // color texture clone
                    if (asset.compositionColorClone)
                    {
                        desc.msaaSamples = 1;// no MSAA on clone textures
                        desc.bindMS = false;
                        if (asset.compositionColorCloneBlurredMipmaps)
                        {
                            desc.useMipMap = true;
                            desc.mipCount = Texture.GenerateAllMips;
                            desc.autoGenerateMips = false;
                        }
                        colorTextureClone = GetTemporaryRenderTexture(desc);
                        colorTextureCloneID = colorTextureClone;
                        SetTextureSamplerState(colorTextureClone, asset.compositionColorCloneBlurredMipmaps ? FilterMode.Trilinear : FilterMode.Point, TextureWrapMode.Clamp);
                    }
                    
                    // compositing textures
                    desc.mipCount = 1;// no mipmaps on final textures
                    desc.useMipMap = false;
                    desc.msaaSamples = 1;// no MSAA on final textures
                    desc.bindMS = false;
                    if (compositingTextures == null)
                    {
                        compositingTextures = new RenderTexture[2];
                        compositingTexturesID = new RenderTargetIdentifier[2];
                    }
                    for (int i = 0; i != 2; ++i)
                    {
                        compositingTextures[i] = GetTemporaryRenderTexture(desc);
                        compositingTexturesID[i] = compositingTextures[i];
                        SetTextureSamplerState(compositingTextures[i], FilterMode.Point, TextureWrapMode.Clamp);
                    }

                    // render-pass desc
                    if (!renderPass_Opaque.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, false, true, clearDepth, backgroundColor),
                            new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, false, true, clearColor, backgroundColor)
                        };
                        renderPass_Opaque = new RenderPassDesc(widthComposited, heightComposited, targets, (int)asset.compositionMSAA);
                    }
                    else
                    {
                        renderPass_Opaque.UpdateSize(widthComposited, heightComposited);
                        renderPass_Opaque.UpdateTarget(new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, false, true, clearDepth, backgroundColor), 0);
                        renderPass_Opaque.UpdateTarget(new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, false, true, clearColor, backgroundColor), 1);
                    }

                    if (!renderPass_Transparent.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, true, true),
                            new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, true, true)
                        };
                        renderPass_Transparent = new RenderPassDesc(widthComposited, heightComposited, targets, (int)asset.compositionMSAA);
                    }
                    else
                    {
                        renderPass_Transparent.UpdateSize(widthComposited, heightComposited);
                        renderPass_Transparent.UpdateTarget(new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, true, true), 0);
                        renderPass_Transparent.UpdateTarget(new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, true, true), 1);
                    }

                    // post-process resources
                    if (postProcessResources == null) postProcessResources = new ReignRP_PostProcessResources();
                    postProcessResources.Update(widthComposited, heightComposited, camera, colorTexture);
                    if (refreshPostProcessState)
                    {
                        #if UNITY_EDITOR
                        if (camera.cameraType == CameraType.SceneView)
                        {
                            var scenePostProcesses = new List<ReignRP_PostProcess>();
                            foreach (var p in GameObject.FindObjectsByType<ReignRP_PostProcess>(FindObjectsSortMode.None))
                            {
                                if (!p.previewInSceneView || !p.enabled) continue;

                                var obj = p.gameObject;
                                var c = obj.GetComponent<Camera>();
                                if (!obj.activeInHierarchy || (c && c.targetTexture)) continue;
                                
                                scenePostProcesses.Add(p);
                            }
                            postProcesses = scenePostProcesses.ToArray();
                        }
                        else
                        {
                            postProcesses = camera.GetComponents<ReignRP_PostProcess>();
                        }
                        #else
                        postProcesses = camera.GetComponents<ReignRP_PostProcess>();
                        #endif
                    }
				}
                else
                {
                    viewport.x *= widthTarget;
                    viewport.y *= heightTarget;
                    viewport.width *= widthTarget;
                    viewport.height *= heightTarget;
                    
                    texelWidth = 1f / widthTarget;
                    texelHeight = 1f / heightTarget;
                    
                    // render-pass desc
                    if (!renderPass_Opaque.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, false, true, clearDepth, backgroundColor),
                            new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, false, true, clearColor, backgroundColor)
                        };
                        renderPass_Opaque = new RenderPassDesc(widthTarget, heightTarget, targets, 1);
                    }
                    else
                    {
                        renderPass_Opaque.UpdateSize(widthTarget, heightTarget);
                        renderPass_Opaque.UpdateTarget(new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, false, true, clearDepth, backgroundColor), 0);
                        renderPass_Opaque.UpdateTarget(new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, false, true, clearColor, backgroundColor), 1);
                    }

                    if (!renderPass_Transparent.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, true, true),
                            new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, true, true)
                        };
                        renderPass_Transparent = new RenderPassDesc(widthTarget, heightTarget, targets, 1);
                    }
                    else
                    {
                        renderPass_Transparent.UpdateSize(widthTarget, heightTarget);
                        renderPass_Transparent.UpdateTarget(new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, true, true), 0);
                        renderPass_Transparent.UpdateTarget(new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, true, true), 1);
                    }
                }
            }

			public void UpdateEnd()
			{
                ReleaseBuffers(false);
			}

			public void ReleaseBuffers(bool fullDispose)
			{
                ReleaseTempRenderTexture(ref depthTexture);
                ReleaseTempRenderTexture(ref depthTextureClone);
				ReleaseTempRenderTexture(ref colorTexture);
                ReleaseTempRenderTexture(ref colorTextureClone);
				//ReleaseTempRenderTexture(ref velocityTexture);
                if (compositingTextures != null)
                {
                    for (int i = 0; i != compositingTextures.Length; ++i) ReleaseTempRenderTexture(ref compositingTextures[i]);
                }

                if (fullDispose)
                {
                    renderPass_Opaque.Dispose();
                    renderPass_Transparent.Dispose();
                }
            }
            
            public void ResolveCompositedColorTexture(CommandBuffer cmd)
            {
                // copy texture to clone
                if (asset.compositionMSAA == MSAA_Level.Off) cmd.CopyTexture(colorTextureID, 0, 0, colorTextureCloneID, 0, 0);
                else ResolveCompositedMSAATexture(cmd, colorTexture, colorTextureClone);

                // blur texture mipmaps
                if (asset.compositionColorCloneBlurredMipmaps) pipeline.BlurRoughnessTexture(colorTextureClone, RenderTextureSubElement.Color, compositingTextures[0]);

                // set texture
                cmd.SetGlobalTexture("_CameraColorTexture", colorTextureCloneID, RenderTextureSubElement.Color);
                cmd.SetGlobalFloat("mipmaps_CameraColorTexture", colorTextureClone.mipmapCount);
            }

            public void ResolveCompositedDepthTexture(CommandBuffer cmd)
            {
                cmd.CopyTexture(depthTextureID, depthTextureCloneID);
                cmd.SetGlobalTexture("_CameraDepthTexture", depthTextureCloneID, RenderTextureSubElement.Depth);
            }
            
            public void ResolveCompositedMSAATexture(CommandBuffer cmd, RenderTexture src, RenderTexture dst)
            {
                cmd.SetRenderTarget(src);
                cmd.ResolveAntiAliasedSurface(src, dst);
            }
        }
    }
}
