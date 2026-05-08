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

        public RenderPassDesc(int width, int height, RenderPassDescTarget[] targets)
        {
            isInit = true;

            this.width = width;
            this.height = height;
            this.targets = targets;
            renderTarget_First = BuiltinRenderTextureType.None;
            renderTarget_Depth = BuiltinRenderTextureType.None;
            firstIndex = -1;
            depthIndex = -1;

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
                if (target.clear && !ReignRenderPipelineAsset.singleton.renderPassesMultiCameraClear) attchment.ConfigureClear(target.backgroundColor);
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
            if (target.clear && !ReignRenderPipelineAsset.singleton.renderPassesMultiCameraClear) attchment.ConfigureClear(target.backgroundColor);
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

    public partial class ReignRenderPipeline
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
			public Camera camera;
			public ReignRenderPipeline pipeline;
			private ReignRenderPipelineAsset asset;

            public RenderTexture cameraTargetTexture;
            public RenderTargetIdentifier cameraTargetTextureID, cameraTargetDepthTextureID;
            public RenderTextureFormat cameraTargetFormat;
            public int cameraTargetDepth;

            public RenderTexture depthTexture, depthTextureClone;
			public RenderTexture colorTexture, normalTexture, compositingFinalTexture;
			public RenderTexture velocityTexture;

            public RenderTargetIdentifier depthTextureID, depthTextureCloneID;
			public RenderTargetIdentifier colorTextureID, normalTextureID, compositingFinalTextureID;
            public RenderTargetIdentifier velocityTextureID;
            public RenderPassDesc forwardRenderPass_Opaque, forwardRenderPass_Transparent;
            public RenderPassDesc deferredRenderPass_Opaque, deferredRenderPass_Transparent;
			public int width, height, widthComposited, heightComposited, widthRenderTarget, heightRenderTarget;
            public float texelWidth, texelHeight;
			public Matrix4x4 cameraViewProj_Last;
			public Matrix4x4 clipToWorld;

            public Matrix4x4 viewMat, projMat, viewProjMat;
            private CommonTextureFormat[] colorTextureFallbacks;

            public CameraResource(Camera camera, ReignRenderPipeline pipeline)
			{
				this.camera = camera;
				this.pipeline = pipeline;
				asset = pipeline.asset;
				width = camera.pixelWidth;
				height = camera.pixelHeight;
                widthComposited = width / asset.compositionDivision;
                heightComposited = height / asset.compositionDivision;
                widthRenderTarget = -1;
                heightRenderTarget = -1;
                texelWidth = 1f / width;
                texelHeight = 1f / height;
                
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
                if (camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview) compositionDepthBit = 24;
                #endif
                return compositionDepthBit;
            }

			public void UpdateStart()
			{
				frame = 0;

                // current camera specs
                width = camera.pixelWidth;
				height = camera.pixelHeight;
                widthComposited = width / asset.compositionDivision;
                heightComposited = height / asset.compositionDivision;

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
                cameraTargetTexture = camera.targetTexture;
                if (!cameraTargetTexture)
                {
                    if (xrRenderPassInfo.isXRActive)
                    {
                        cameraTargetTextureID = xrRenderPassInfo.pass.renderTarget;
                        cameraTargetDepthTextureID = xrRenderPassInfo.pass.renderTarget;
                        cameraTargetFormat = xrRenderPassInfo.pass.renderTargetDesc.colorFormat;
                        cameraTargetDepth = xrRenderPassInfo.pass.renderTargetDesc.depthBufferBits;
                        widthRenderTarget = xrRenderPassInfo.pass.renderTargetDesc.width;
                        heightRenderTarget = xrRenderPassInfo.pass.renderTargetDesc.height;
                    }
                    else
                    {
                        cameraTargetTextureID = BuiltinRenderTextureType.CameraTarget;// swap-buffer
                        cameraTargetDepthTextureID = BuiltinRenderTextureType.Depth;// swap-buffer
                        cameraTargetFormat = asset.hdr ? RenderTextureFormat.ARGB2101010 : RenderTextureFormat.ARGB32;
                        cameraTargetDepth = 24;
                        widthRenderTarget = Screen.width;
                        heightRenderTarget = Screen.height;
                    }
                }
                else
                {
                    cameraTargetTextureID = cameraTargetTexture;
                    cameraTargetDepthTextureID = cameraTargetTexture;
                    cameraTargetFormat = cameraTargetTexture.format;
                    cameraTargetDepth = cameraTargetTexture.depth;
                    widthRenderTarget = cameraTargetTexture.width;
                    heightRenderTarget = cameraTargetTexture.height;
                }

                // compositing
                if (asset.enableComposition)
				{
                    int compositionDepthBit = GetCompositedDepthBit();

                    // depth texture
                    var desc = new RenderTextureDescriptor(widthComposited, heightComposited, RenderTextureFormat.Depth, compositionDepthBit);
                    desc.msaaSamples = (int)asset.compositionMSAA;
				    depthTexture = GetTemporaryRenderTexture(desc);
				    depthTextureID = depthTexture;
                    SetTextureSamplerState(depthTexture, FilterMode.Point, TextureWrapMode.Clamp, false);

                    // depth texture clone
                    if (asset.compositionDepthClone)
                    {
                        depthTextureClone = GetTemporaryRenderTexture(desc);
				        depthTextureCloneID = depthTextureClone;
                        SetTextureSamplerState(depthTextureClone, FilterMode.Point, TextureWrapMode.Clamp, false);
                    }

					// color texture
					desc = new RenderTextureDescriptor(widthComposited, heightComposited, GetCompositionTextureFormat(asset.compositionColorFormat, colorTextureFallbacks), 0);
                    desc.msaaSamples = (int)asset.compositionMSAA;
					colorTexture = GetTemporaryRenderTexture(desc);
					colorTextureID = colorTexture;
                    SetTextureSamplerState(colorTexture, FilterMode.Point, TextureWrapMode.Clamp, false);

                    // render-pass desc
                    if (!forwardRenderPass_Opaque.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, false, true, clearDepth, backgroundColor),
                            new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, false, true, clearColor, backgroundColor)
                        };
                        forwardRenderPass_Opaque = new RenderPassDesc(widthComposited, heightComposited, targets);
                    }
                    else
                    {
                        forwardRenderPass_Opaque.UpdateSize(widthComposited, heightComposited);
                        forwardRenderPass_Opaque.UpdateTarget(new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, false, true, clearDepth, backgroundColor), 0);
                        forwardRenderPass_Opaque.UpdateTarget(new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, false, true, clearColor, backgroundColor), 1);
                    }

                    if (!forwardRenderPass_Transparent.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, true, true),
                            new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, true, true)
                        };
                        forwardRenderPass_Transparent = new RenderPassDesc(widthComposited, heightComposited, targets);
                    }
                    else
                    {
                        forwardRenderPass_Transparent.UpdateSize(widthComposited, heightComposited);
                        forwardRenderPass_Transparent.UpdateTarget(new RenderPassDescTarget(depthTextureID, depthTexture.format, depthTexture.depth, true, true), 0);
                        forwardRenderPass_Transparent.UpdateTarget(new RenderPassDescTarget(colorTextureID, colorTexture.format, 0, true, true), 1);
                    }
				}
                else
                {
                    // render-pass desc
                    if (!forwardRenderPass_Opaque.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, false, true, clearDepth, backgroundColor),
                            new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, false, true, clearColor, backgroundColor)
                        };
                        forwardRenderPass_Opaque = new RenderPassDesc(widthRenderTarget, heightRenderTarget, targets);
                    }
                    else
                    {
                        forwardRenderPass_Opaque.UpdateSize(widthRenderTarget, heightRenderTarget);
                        forwardRenderPass_Opaque.UpdateTarget(new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, false, true, clearDepth, backgroundColor), 0);
                        forwardRenderPass_Opaque.UpdateTarget(new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, false, true, clearColor, backgroundColor), 1);
                    }

                    if (!forwardRenderPass_Transparent.isInit)
                    {
                        var targets = new RenderPassDescTarget[2]
                        {
                            new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, true, true),
                            new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, true, true)
                        };
                        forwardRenderPass_Transparent = new RenderPassDesc(widthRenderTarget, heightRenderTarget, targets);
                    }
                    else
                    {
                        forwardRenderPass_Transparent.UpdateSize(widthRenderTarget, heightRenderTarget);
                        forwardRenderPass_Transparent.UpdateTarget(new RenderPassDescTarget(cameraTargetDepthTextureID, RenderTextureFormat.Depth, cameraTargetDepth, true, true), 0);
                        forwardRenderPass_Transparent.UpdateTarget(new RenderPassDescTarget(cameraTargetTextureID, cameraTargetFormat, 0, true, true), 1);
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
				ReleaseTempRenderTexture(ref normalTexture);
				ReleaseTempRenderTexture(ref velocityTexture);
				ReleaseTempRenderTexture(ref compositingFinalTexture);

                if (fullDispose)
                {
                    forwardRenderPass_Opaque.Dispose();
                    forwardRenderPass_Transparent.Dispose();
                    deferredRenderPass_Opaque.Dispose();
                    deferredRenderPass_Transparent.Dispose();
                }
            }

            public void ResolveCompositedDepthTexture(CommandBuffer cmd)
            {
                cmd.CopyTexture(depthTextureID, depthTextureCloneID);
                cmd.SetGlobalTexture("_CameraDepthTexture", depthTextureCloneID, RenderTextureSubElement.Depth);
            }
        }
    }
}
