using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Reign.SRP
{
	[StructLayout(LayoutKind.Sequential)]
    public struct Vector4h
    {
        public ushort x, y, z, w;

        public Vector4h(float x, float y, float z, float w)
        {
            this.x = Mathf.FloatToHalf(x);
            this.y = Mathf.FloatToHalf(y);
            this.z = Mathf.FloatToHalf(z);
            this.w = Mathf.FloatToHalf(w);
        }

        public Vector4h(Vector4 value)
        {
            this.x = Mathf.FloatToHalf(value.x);
            this.y = Mathf.FloatToHalf(value.y);
            this.z = Mathf.FloatToHalf(value.z);
            this.w = Mathf.FloatToHalf(value.w);
        }
    }

	class XRRenderPassInfo
	{
		public bool isXRActive;
		public int eyePass;
		public XRDisplaySubsystem.XRRenderPass pass;
		public XRDisplaySubsystem.XRRenderParameter parameter;
	}

    public partial class ReignRP
    {
        public static void SetTextureSamplerState(Texture texture, FilterMode filter, TextureWrapMode wrap, int anisoFiltering = 0)
		{
			if (texture == null) return;
			texture.anisoLevel = anisoFiltering;
			texture.filterMode = filter;
			texture.wrapMode = wrap;
		}

		public static void ChangeSwapChainResolution(Resolution resolution, FullScreenMode fullscreenMode)
		{
			#if UNITY_STANDALONE
				#if UNITY_2022_3_OR_NEWER
				Screen.SetResolution(resolution.width, resolution.height, fullscreenMode, resolution.refreshRateRatio);
				#else
				Screen.SetResolution(resolution.width, resolution.height, fullscreenMode, resolution.refreshRate);
				#endif
			#else
				#if UNITY_2022_3_OR_NEWER
				if (Screen.orientation == ScreenOrientation.LandscapeLeft && resolution.width < resolution.height) Screen.SetResolution(resolution.height, resolution.width, fullscreenMode, resolution.refreshRateRatio);
				else Screen.SetResolution(resolution.width, resolution.height, fullscreenMode, resolution.refreshRateRatio);
				#else
				if (Screen.orientation == ScreenOrientation.LandscapeLeft && resolution.width < resolution.height) Screen.SetResolution(resolution.height, resolution.width, fullscreenMode, resolution.refreshRate);
				else Screen.SetResolution(resolution.width, resolution.height, fullscreenMode, resolution.refreshRate);
				#endif
			#endif
		}

		public static void TryInitMaterial(ref Material material, Shader shader)
		{
			if (material == null && shader != null) material = new Material(shader);
		}

		public static RenderQueueRange QueueRangeToRenderQueueRange(QueueRange range, out SortingCriteria sortingCriteria)
		{
			switch (range)
			{
				case QueueRange.Any:
				sortingCriteria = SortingCriteria.None;
				return RenderQueueRange.all;

				case QueueRange.Opaque:
				sortingCriteria = SortingCriteria.CommonOpaque;
				return RenderQueueRange.opaque;

				case QueueRange.Transparent:
				sortingCriteria = SortingCriteria.CommonTransparent;
				return RenderQueueRange.transparent;
			}
			throw new NotImplementedException("QueueRange not supported: " + range);
		}

		private static RenderTextureFormat GetTextureFormatInternal(CommonTextureFormat format)
		{
			switch (format)
			{
				case CommonTextureFormat.UINT_RGB565: return RenderTextureFormat.RGB565;
				case CommonTextureFormat.UINT_16: return RenderTextureFormat.ARGB4444;
				case CommonTextureFormat.UINT_32: return RenderTextureFormat.ARGB32;
				case CommonTextureFormat.UINT_A2_RGB10: return RenderTextureFormat.ARGB2101010;
				case CommonTextureFormat.UFloat_10: return RenderTextureFormat.RGB111110Float;
				case CommonTextureFormat.Float_16: return RenderTextureFormat.ARGBHalf;
				case CommonTextureFormat.Float_32: return RenderTextureFormat.ARGBFloat;
				default: throw new NotSupportedException("Render Texture format not supported: " + format.ToString());
			}
		}

		public static RenderTextureFormat GetTextureFormat(CommonTextureFormat format, CommonTextureFormat[] fallbacks = null)
		{
			var unityFormat = GetTextureFormatInternal(format);
			bool isSupported = SystemInfo.SupportsRenderTextureFormat(unityFormat);
			if (!isSupported && fallbacks != null)
			{
				foreach (var fallback in fallbacks)
				{
					unityFormat = GetTextureFormatInternal(format);
					if (SystemInfo.SupportsRenderTextureFormat(unityFormat))
					{
						return unityFormat;
					}
				}
			}
			
			if (!isSupported) unityFormat = RenderTextureFormat.ARGB32;// if format not avaliable, default to portable
			return unityFormat;
		}

		private static GraphicsFormat GetTextureGraphicsFormatInternal(CommonTextureFormat format)
		{
			switch (format)
			{
				case CommonTextureFormat.UINT_RGB565: return GraphicsFormat.B5G6R5_UNormPack16;
				case CommonTextureFormat.UINT_16: return GraphicsFormat.B4G4R4A4_UNormPack16;
				case CommonTextureFormat.UINT_32: return GraphicsFormat.R8G8B8A8_UNorm;
				case CommonTextureFormat.UINT_A2_RGB10: return GraphicsFormat.A2B10G10R10_UNormPack32;
				case CommonTextureFormat.UFloat_10: return GraphicsFormat.B10G11R11_UFloatPack32;
				case CommonTextureFormat.Float_16: return GraphicsFormat.R16G16B16A16_SFloat;
				case CommonTextureFormat.Float_32: return GraphicsFormat.R32G32B32A32_SFloat;
				default: throw new NotSupportedException("Graphics Texture format not supported: " + format.ToString());
			}
		}

		public static GraphicsFormat GetTextureGraphicsFormat(CommonTextureFormat format, GraphicsFormatUsage usage, CommonTextureFormat[] fallbacks = null)
		{
			var unityFormat = GetTextureGraphicsFormatInternal(format);
			bool isSupported = SystemInfo.IsFormatSupported(unityFormat, usage);
			if (!isSupported && fallbacks != null)
			{
				foreach (var fallback in fallbacks)
				{
					unityFormat = GetTextureGraphicsFormatInternal(format);
					if (SystemInfo.IsFormatSupported(unityFormat, usage))
					{
						return unityFormat;
					}
				}
			}

			if (!isSupported) unityFormat = GraphicsFormat.R8G8B8A8_UNorm;// if format not avaliable, default to portable
			return unityFormat;
		}

		public static bool GetMaxTexture2DSize(int width, int height, out int texWidth, out int texHeight)
		{
			return GetMaxTextureSize(SystemInfo.maxTextureSize, width, height, 1, out texWidth, out texHeight, out _);
		}

		public static bool GetMaxTextureArraySize(int width, int height, out int texWidth, out int texHeight, out int maxSlices)
		{
			maxSlices = SystemInfo.maxTextureArraySlices;
			return GetMaxTextureSize(SystemInfo.maxTextureSize, width, height, 1, out texWidth, out texHeight, out _);
		}

		public static bool GetMaxTexture3DSize(int width, int height, out int texWidth, out int texHeight)
		{
			return GetMaxTextureSize(SystemInfo.maxTexture3DSize, width, height, 1, out texWidth, out texHeight, out _);
		}

		public static bool GetMaxTexture3DSize(int width, int height, int depth, out int texWidth, out int texHeight, out int texDepth)
		{
			return GetMaxTextureSize(SystemInfo.maxTexture3DSize, width, height, 1, out texWidth, out texHeight, out texDepth);
		}

		public static bool GetMaxTextureSize(int maxSize, int width, int height, int depth, out int texWidth, out int texHeight, out int texDepth)
		{
			if (width > maxSize || height > maxSize || depth > maxSize)
			{
				int largestSize = width >= height ? width : height;
				largestSize = largestSize >= depth ? largestSize : depth;

				double largestSizeF = largestSize;
				texWidth = (int)((width / largestSizeF) * width);
				texHeight = (int)((height / largestSizeF) * height);
				texDepth = (int)((depth / largestSizeF) * depth);

				return false;
			}
			else
			{
				texWidth = width;
				texHeight = height;
				texDepth = depth;
				return true;
			}
		}

		public static unsafe void ZeroMemory(void* ptr, int size)
		{
			var data = (byte*)ptr;
			for (int i = 0; i != size; ++i) data[i] = 0;
		}

		private static RenderTexture GetTemporaryRenderTexture(RenderTextureDescriptor desc)
        {
            desc.width = Mathf.Max(1, desc.width);
            desc.height = Mathf.Max(1, desc.height);

            var texture = RenderTexture.GetTemporary(desc);
            if (!texture.IsCreated()) texture.Create();
            return texture;
        }

		private static void ReleaseTempRenderTexture(ref RenderTexture texture)
		{
			if (texture)
			{
                try
                {
					RenderTexture.ReleaseTemporary(texture);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
				texture = null;
            }
        }

        private static void DisposeTexture(ref Texture2D texture)
		{
			if (texture)
			{
                try
                {
					GameObject.DestroyImmediate(texture);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
				texture = null;
            }
        }

        private static void DisposeNativeArray<T>(in NativeArray<T> array) where T : struct
		{
			if (array.IsCreated)
			{
                try
                {
					array.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

		private void CopyTexture(Texture srcTexture, RenderTextureSubElement srcElement, int srcMipLvl, RenderTexture dstTexture, int dstMipLvl, Material copyMaterial, int copyMaterialPass)
		{
			var blitMesh = BlitMesh.mesh;
			cmd.SetGlobalVector("srcRect", new Vector4(0, 0, 1, 1));
			cmd.SetGlobalVector("dstRect", new Vector4(0, 0, 1, 1));
			cmd.SetGlobalFloat("srcMipLvl", srcMipLvl);
			cmd.SetRenderTarget(dstTexture, dstMipLvl);
			cmd.SetGlobalTexture("_SrcTex", srcTexture, srcElement);
			cmd.DrawMesh(blitMesh, Matrix4x4.identity, copyMaterial, 0, copyMaterialPass);
		}
    }
}
