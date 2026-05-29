using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	public enum Bloom_Type
	{
		Normal,
		Radial
	}

	public enum Bloom_ViewType
	{
		FinalImage,
		HighPass,
		HighPassBlured_Composited,
		HighPassBlured_1x,
		HighPassBlured_2x,
		HighPassBlured_4x,
		HighPassBlured_8x,
		HighPassBlured_16x
	}

	public enum Bloom_SampleQuality
	{
		Low,
		Med,
		High
	}

	public enum Bloom_BlurLevel
	{
		Blured_1x,
		Blured_2x,
		Blured_4x,
		Blured_8x,
		Blured_16x
	}

	public class Bloom : ReignRP_PostProcess
	{
		private const int pass_Blur_HighPass = 0;
		private const int pass_Blur_Radial = 1;
		private const int pass_Blur_Classic = 2;
		private const int pass_SizeDownCopy = 3;
		private const int pass_Composite = 4;

		public Shader bloomShader;
		private Material material;

		public Bloom_Type type = Bloom_Type.Normal;
		public Bloom_ViewType viewType = Bloom_ViewType.FinalImage;
		public Bloom_BlurLevel blurLevel = Bloom_BlurLevel.Blured_16x;

		[Range(0, 1)]
		public float highPassRange = 1;

		[Tooltip("Iterations increases sample count")]
		[Range(1, 32)]
		public int blurIterations = 10;

		public float blurStrength = 1;
		public float sampleTexelOffset = 0.5f;

		[Tooltip("float texMul = pow(falloffStart, falloff)")]
		[Range(.1f, 1.0f)]
		public float falloffStart = .9f;

		[Tooltip("texMul = pow(texMul, falloff)")]
		[Range(1, 10)]
		public float falloff = 1;

		[Range(0, 1)]
		public float strength = 1;

		private void Start()
		{
			// needed for enabled to show in editor
		}

		public override void OnPostProcess(ReignRP_PostProcessResources resources, CommandBuffer cmd, in ScriptableRenderContext context, RenderTexture src, RenderTexture dst)
		{
			// validate resources
			if (bloomShader == null)
			{
				Debug.LogError("Bloom resource is null");
				return;
			}

			// make sure init
			if (material == null) material = new Material(bloomShader);

			// ensure src sampler state
			ReignRP.SetTextureSamplerState(src, FilterMode.Point, TextureWrapMode.Clamp);

			// clear cmd
			cmd.Clear();

			// get temp resources
			var textureDesc = new RenderTextureDescriptor(resources.width, resources.height, resources.colorTexture.format, 0, 1)
			{
				dimension = TextureDimension.Tex2D,
				stencilFormat = GraphicsFormat.None
			};
			var texture_1x = RenderTexture.GetTemporary(textureDesc);
			var texture2_1x = RenderTexture.GetTemporary(textureDesc);
			ReignRP.SetTextureSamplerState(texture_1x, FilterMode.Bilinear, TextureWrapMode.Clamp);
			ReignRP.SetTextureSamplerState(texture2_1x, FilterMode.Bilinear, TextureWrapMode.Clamp);

			RenderTexture texture_2x = null, texture2_2x = null;
			if (blurLevel >= Bloom_BlurLevel.Blured_2x)
			{
				textureDesc.width /= 2;
				textureDesc.height /= 2;
				texture_2x = RenderTexture.GetTemporary(textureDesc);
				texture2_2x = RenderTexture.GetTemporary(textureDesc);
				ReignRP.SetTextureSamplerState(texture_2x, FilterMode.Bilinear, TextureWrapMode.Clamp);
				ReignRP.SetTextureSamplerState(texture2_2x, FilterMode.Bilinear, TextureWrapMode.Clamp);
			}

			RenderTexture texture_4x = null, texture2_4x = null;
			if (blurLevel >= Bloom_BlurLevel.Blured_4x)
			{
				textureDesc.width /= 2;
				textureDesc.height /= 2;
				texture_4x = RenderTexture.GetTemporary(textureDesc);
				texture2_4x = RenderTexture.GetTemporary(textureDesc);
				ReignRP.SetTextureSamplerState(texture_4x, FilterMode.Bilinear, TextureWrapMode.Clamp);
				ReignRP.SetTextureSamplerState(texture2_4x, FilterMode.Bilinear, TextureWrapMode.Clamp);
			}

			RenderTexture texture_8x = null, texture2_8x = null;
			if (blurLevel >= Bloom_BlurLevel.Blured_8x)
			{
				textureDesc.width /= 2;
				textureDesc.height /= 2;
				texture_8x = RenderTexture.GetTemporary(textureDesc);
				texture2_8x = RenderTexture.GetTemporary(textureDesc);
				ReignRP.SetTextureSamplerState(texture_8x, FilterMode.Bilinear, TextureWrapMode.Clamp);
				ReignRP.SetTextureSamplerState(texture2_8x, FilterMode.Bilinear, TextureWrapMode.Clamp);
			}

			RenderTexture texture_16x = null, texture2_16x = null;
			if (blurLevel >= Bloom_BlurLevel.Blured_16x)
			{
				textureDesc.width /= 2;
				textureDesc.height /= 2;
				texture_16x = RenderTexture.GetTemporary(textureDesc);
				texture2_16x = RenderTexture.GetTemporary(textureDesc);
				ReignRP.SetTextureSamplerState(texture_16x, FilterMode.Bilinear, TextureWrapMode.Clamp);
				ReignRP.SetTextureSamplerState(texture2_16x, FilterMode.Bilinear, TextureWrapMode.Clamp);
			}

			// high-pass
			cmd.SetGlobalFloat("highPassRange", highPassRange);
			cmd.Blit(src, texture_1x, material, pass_Blur_HighPass);
			if (viewType == Bloom_ViewType.HighPass)
			{
				cmd.Blit(texture_1x, dst, material, pass_SizeDownCopy);
				context.ExecuteCommandBuffer(cmd);

				RenderTexture.ReleaseTemporary(texture_1x);
				RenderTexture.ReleaseTemporary(texture2_1x);

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_2x);
					RenderTexture.ReleaseTemporary(texture2_2x);
				}

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_4x);
					RenderTexture.ReleaseTemporary(texture2_4x);
				}

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_8x);
					RenderTexture.ReleaseTemporary(texture2_8x);
				}

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_16x);
					RenderTexture.ReleaseTemporary(texture2_16x);
				}

				return;
			}

			var hNorm = new Vector2(1, 0);
			var vNorm = new Vector2(0, 1);
			var h = new Vector4(hNorm.x, hNorm.y, blurIterations, (1f / ((blurIterations * 2) + 1)) * blurStrength);
			var v = new Vector4(vNorm.x, vNorm.y, blurIterations, (1f / ((blurIterations * 2) + 1)) * blurStrength);
			var r = new Vector4(1, 1, blurIterations, blurStrength);
			var r2 = new Vector4(sampleTexelOffset, 0, 0, 0);
			cmd.SetGlobalVector("args2", r2);

			// =========================================
			// 1x
			// =========================================
			if (type == Bloom_Type.Radial)
			{
				// blur radial
				cmd.SetGlobalVector("args", r);
				cmd.Blit(texture_1x, texture2_1x, material, pass_Blur_Radial);

				// size-down
				if (blurLevel >= Bloom_BlurLevel.Blured_2x) cmd.Blit(texture2_1x, texture_2x, material, pass_SizeDownCopy);
			}
			else
			{
				// blur horizontal
				cmd.SetGlobalVector("args", h);
				cmd.Blit(texture_1x, texture2_1x, material, pass_Blur_Classic);

				// blur vertical
				cmd.SetGlobalVector("args", v);
				cmd.Blit(texture2_1x, texture_1x, material, pass_Blur_Classic);

				// size-down
				if (blurLevel >= Bloom_BlurLevel.Blured_2x) cmd.Blit(texture_1x, texture_2x, material, pass_SizeDownCopy);
			}

			// high-pass-blured preview
			if (viewType == Bloom_ViewType.HighPassBlured_1x)
			{
				cmd.Blit(type == Bloom_Type.Normal ? texture_1x : texture2_1x, dst, material, pass_SizeDownCopy);
				cmd.SetRenderTarget(dst);
				context.ExecuteCommandBuffer(cmd);

				RenderTexture.ReleaseTemporary(texture_1x);
				RenderTexture.ReleaseTemporary(texture2_1x);

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_2x);
					RenderTexture.ReleaseTemporary(texture2_2x);
				}

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_4x);
					RenderTexture.ReleaseTemporary(texture2_4x);
				}

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_8x);
					RenderTexture.ReleaseTemporary(texture2_8x);
				}

				if (blurLevel >= Bloom_BlurLevel.Blured_2x)
				{
					RenderTexture.ReleaseTemporary(texture_16x);
					RenderTexture.ReleaseTemporary(texture2_16x);
				}

				return;
			}

			// =========================================
			// 2x
			// =========================================
			if (blurLevel >= Bloom_BlurLevel.Blured_4x)
			{
				if (type == Bloom_Type.Radial)
				{
					// blur radial
					cmd.SetGlobalVector("args", r);
					cmd.Blit(texture_2x, texture2_2x, material, pass_Blur_Radial);

					// size-down
					if (blurLevel >= Bloom_BlurLevel.Blured_4x) cmd.Blit(texture2_2x, texture_4x, material, pass_SizeDownCopy);
				}
				else
				{
					// blur horizontal
					cmd.SetGlobalVector("args", h);
					cmd.Blit(texture_2x, texture2_2x, material, pass_Blur_Classic);

					// blur vertical
					cmd.SetGlobalVector("args", v);
					cmd.Blit(texture2_2x, texture_2x, material, pass_Blur_Classic);

					// size-down
					if (blurLevel >= Bloom_BlurLevel.Blured_4x) cmd.Blit(texture_2x, texture_4x, material, pass_SizeDownCopy);
				}

				// high-pass-blured preview
				if (viewType == Bloom_ViewType.HighPassBlured_2x)
				{
					cmd.Blit(type == Bloom_Type.Normal ? texture_2x : texture2_2x, dst, material, pass_SizeDownCopy);
					context.ExecuteCommandBuffer(cmd);

					RenderTexture.ReleaseTemporary(texture_1x);
					RenderTexture.ReleaseTemporary(texture2_1x);

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_2x);
						RenderTexture.ReleaseTemporary(texture2_2x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_4x);
						RenderTexture.ReleaseTemporary(texture2_4x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_8x);
						RenderTexture.ReleaseTemporary(texture2_8x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_16x);
						RenderTexture.ReleaseTemporary(texture2_16x);
					}

					return;
				}
			}

			// =========================================
			// 4x
			// =========================================
			if (blurLevel >= Bloom_BlurLevel.Blured_4x)
			{
				if (type == Bloom_Type.Radial)
				{
					// blur radial
					cmd.SetGlobalVector("args", r);
					cmd.Blit(texture_4x, texture2_4x, material, pass_Blur_Radial);

					// size-down
					if (blurLevel >= Bloom_BlurLevel.Blured_8x) cmd.Blit(texture2_4x, texture_8x, material, pass_SizeDownCopy);
				}
				else
				{
					// blur horizontal
					cmd.SetGlobalVector("args", h);
					cmd.Blit(texture_4x, texture2_4x, material, pass_Blur_Classic);

					// blur vertical
					cmd.SetGlobalVector("args", v);
					cmd.Blit(texture2_4x, texture_4x, material, pass_Blur_Classic);

					// size-down
					if (blurLevel >= Bloom_BlurLevel.Blured_8x) cmd.Blit(texture_4x, texture_8x, material, pass_SizeDownCopy);
				}

				// high-pass-blured preview
				if (viewType == Bloom_ViewType.HighPassBlured_4x)
				{
					cmd.Blit(type == Bloom_Type.Normal ? texture_4x : texture2_4x, dst, material, pass_SizeDownCopy);
					context.ExecuteCommandBuffer(cmd);

					RenderTexture.ReleaseTemporary(texture_1x);
					RenderTexture.ReleaseTemporary(texture2_1x);

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_2x);
						RenderTexture.ReleaseTemporary(texture2_2x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_4x);
						RenderTexture.ReleaseTemporary(texture2_4x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_8x);
						RenderTexture.ReleaseTemporary(texture2_8x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_16x);
						RenderTexture.ReleaseTemporary(texture2_16x);
					}

					return;
				}
			}

			// =========================================
			// 8x
			// =========================================
			if (blurLevel >= Bloom_BlurLevel.Blured_8x)
			{
				if (type == Bloom_Type.Radial)
				{
					// blur radial
					cmd.SetGlobalVector("args", r);
					cmd.Blit(texture_8x, texture2_8x, material, pass_Blur_Radial);

					// size-down
					if (blurLevel >= Bloom_BlurLevel.Blured_16x) cmd.Blit(texture2_8x, texture_16x, material, pass_SizeDownCopy);
				}
				else
				{
					// blur horizontal
					cmd.SetGlobalVector("args", h);
					cmd.Blit(texture_8x, texture2_8x, material, pass_Blur_Classic);

					// blur vertical
					cmd.SetGlobalVector("args", v);
					cmd.Blit(texture2_8x, texture_8x, material, pass_Blur_Classic);

					// size-down
					if (blurLevel >= Bloom_BlurLevel.Blured_16x) cmd.Blit(texture_8x, texture_16x, material, pass_SizeDownCopy);
				}

				// high-pass-blured preview
				if (viewType == Bloom_ViewType.HighPassBlured_8x)
				{
					cmd.Blit(type == Bloom_Type.Normal ? texture_8x : texture2_8x, dst, material, pass_SizeDownCopy);
					context.ExecuteCommandBuffer(cmd);

					RenderTexture.ReleaseTemporary(texture_1x);
					RenderTexture.ReleaseTemporary(texture2_1x);

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_2x);
						RenderTexture.ReleaseTemporary(texture2_2x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_4x);
						RenderTexture.ReleaseTemporary(texture2_4x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_8x);
						RenderTexture.ReleaseTemporary(texture2_8x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_16x);
						RenderTexture.ReleaseTemporary(texture2_16x);
					}

					return;
				}
			}

			// =========================================
			// 16x
			// =========================================
			if (blurLevel >= Bloom_BlurLevel.Blured_16x)
			{
				if (type == Bloom_Type.Radial)
				{
					// blur radial
					cmd.SetGlobalVector("args", r);
					cmd.Blit(texture_16x, texture2_16x, material, pass_Blur_Radial);
				}
				else
				{
					// blur horizontal
					cmd.SetGlobalVector("args", h);
					cmd.Blit(texture_16x, texture2_16x, material, pass_Blur_Classic);

					// blur vertical
					cmd.SetGlobalVector("args", v);
					cmd.Blit(texture2_16x, texture_16x, material, pass_Blur_Classic);
				}

				// high-pass-blured preview
				if (viewType == Bloom_ViewType.HighPassBlured_16x)
				{
					cmd.Blit(type == Bloom_Type.Normal ? texture_16x : texture2_16x, dst, material, pass_SizeDownCopy);
					context.ExecuteCommandBuffer(cmd);

					RenderTexture.ReleaseTemporary(texture_1x);
					RenderTexture.ReleaseTemporary(texture2_1x);

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_2x);
						RenderTexture.ReleaseTemporary(texture2_2x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_4x);
						RenderTexture.ReleaseTemporary(texture2_4x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_8x);
						RenderTexture.ReleaseTemporary(texture2_8x);
					}

					if (blurLevel >= Bloom_BlurLevel.Blured_2x)
					{
						RenderTexture.ReleaseTemporary(texture_16x);
						RenderTexture.ReleaseTemporary(texture2_16x);
					}

					return;
				}
			}

			// composite
			if (type == Bloom_Type.Normal)
			{
				if (blurLevel >= Bloom_BlurLevel.Blured_1x) cmd.SetGlobalTexture("_MainTex1", texture_1x);
				if (blurLevel >= Bloom_BlurLevel.Blured_2x) cmd.SetGlobalTexture("_MainTex2", texture_2x);
				if (blurLevel >= Bloom_BlurLevel.Blured_4x) cmd.SetGlobalTexture("_MainTex4", texture_4x);
				if (blurLevel >= Bloom_BlurLevel.Blured_8x) cmd.SetGlobalTexture("_MainTex8", texture_8x);
				if (blurLevel >= Bloom_BlurLevel.Blured_16x) cmd.SetGlobalTexture("_MainTex16", texture_16x);
			}
			else
			{
				if (blurLevel >= Bloom_BlurLevel.Blured_1x) cmd.SetGlobalTexture("_MainTex1", texture2_1x);
				if (blurLevel >= Bloom_BlurLevel.Blured_2x) cmd.SetGlobalTexture("_MainTex2", texture2_2x);
				if (blurLevel >= Bloom_BlurLevel.Blured_4x) cmd.SetGlobalTexture("_MainTex4", texture2_4x);
				if (blurLevel >= Bloom_BlurLevel.Blured_8x) cmd.SetGlobalTexture("_MainTex8", texture2_8x);
				if (blurLevel >= Bloom_BlurLevel.Blured_16x) cmd.SetGlobalTexture("_MainTex16", texture2_16x);
			}
			float arg1 = Mathf.Pow(falloffStart, falloff);
			float arg2 = Mathf.Pow(arg1, falloff);
			float arg3 = Mathf.Pow(arg2, falloff);
			float arg4 = Mathf.Pow(arg3, falloff);
			float arg5 = Mathf.Pow(arg4, falloff);
			cmd.SetGlobalVector("mulArgs1", new Vector4(arg1, arg2, arg3, arg4));
			cmd.SetGlobalVector("mulArgs2", new Vector4(arg5, (1f / (float)(blurLevel + 1)) * strength));
			string mode = (viewType != Bloom_ViewType.HighPassBlured_Composited) ? "MODE_NORMAL" : "MODE_HIGHPASS_ONLY";
			string lvl;
			switch (blurLevel)
			{
				case Bloom_BlurLevel.Blured_1x:
					lvl = "LVL_1X";
					break;

				case Bloom_BlurLevel.Blured_2x:
					lvl = "LVL_2X";
					break;

				case Bloom_BlurLevel.Blured_4x:
					lvl = "LVL_4X";
					break;

				case Bloom_BlurLevel.Blured_8x:
					lvl = "LVL_8X";
					break;

				case Bloom_BlurLevel.Blured_16x:
					lvl = "LVL_16X";
					break;

				default: throw new NotImplementedException();
			}
			cmd.EnableShaderKeyword(mode);
			cmd.EnableShaderKeyword(lvl);
			cmd.Blit(src, dst, material, pass_Composite);
			cmd.DisableShaderKeyword(mode);
			cmd.DisableShaderKeyword(lvl);

			// execute cmd
			context.ExecuteCommandBuffer(cmd);

			// release temp resources
			RenderTexture.ReleaseTemporary(texture_1x);
			RenderTexture.ReleaseTemporary(texture2_1x);

			if (blurLevel >= Bloom_BlurLevel.Blured_2x)
			{
				RenderTexture.ReleaseTemporary(texture_2x);
				RenderTexture.ReleaseTemporary(texture2_2x);
			}

			if (blurLevel >= Bloom_BlurLevel.Blured_2x)
			{
				RenderTexture.ReleaseTemporary(texture_4x);
				RenderTexture.ReleaseTemporary(texture2_4x);
			}

			if (blurLevel >= Bloom_BlurLevel.Blured_2x)
			{
				RenderTexture.ReleaseTemporary(texture_8x);
				RenderTexture.ReleaseTemporary(texture2_8x);
			}

			if (blurLevel >= Bloom_BlurLevel.Blured_2x)
			{
				RenderTexture.ReleaseTemporary(texture_16x);
				RenderTexture.ReleaseTemporary(texture2_16x);
			}
		}
	}
}