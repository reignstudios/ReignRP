using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	public sealed class TubeDisplayGlass : ReignRP_Upscaler
	{
		public Shader shader;
		private Material material;

		public float scale = 1.1f;
		public float warp = 0.5f;
		public float bevel = 0.25f;

		private void Start()
		{
			// needed for enabled to show in editor
		}

		public override void OnUpscale(ReignRP_UpscalerResources resources, CommandBuffer cmd, in ScriptableRenderContext context, RenderTexture src, RenderTargetIdentifier dst)
		{
			// validate resources
			if (shader == null)
			{
				Debug.LogError("TubeDisplay resource is null");
				return;
			}

			// make sure init
			if (material == null) material = new Material(shader);

			// ensure src sampler state
			ReignRP.SetTextureSamplerState(src, FilterMode.Point, TextureWrapMode.Clamp);

			// clear cmd
			cmd.Clear();

			// blit scanlines
			SetControlPoints();
			cmd.Blit(src, dst, material, 0);

			// execute cmd
			context.ExecuteCommandBuffer(cmd);
		}

		private static Vector4 ToUV(Vector4 v)
		{
			return new Vector4(v.x + .5f, v.y + .5f);
		}

		private void SetControlPoints()
		{
			float p = .5f * scale;
			float w = (p / 3f) * warp;

			// Row V = 0
			var _P00 = ToUV(new Vector4(-p, -p));
			var _P10 = ToUV(new Vector4(-w - bevel, -p));
			var _P20 = ToUV(new Vector4(w + bevel, -p));
			var _P30 = ToUV(new Vector4(p, -p));

			// Row V = 1/3
			var _P01 = ToUV(new Vector4(-p, -w - bevel));
			var _P11 = ToUV(new Vector4(-w, -w));// inner
			var _P21 = ToUV(new Vector4(w, -w));// inner
			var _P31 = ToUV(new Vector4(p, -w - bevel));

			// Row V = 2/3
			var _P02 = ToUV(new Vector4(-p, w + bevel));
			var _P12 = ToUV(new Vector4(-w, w));// inner
			var _P22 = ToUV(new Vector4(w, w));// inner
			var _P32 = ToUV(new Vector4(p, w + bevel));

			// Row V = 1
			var _P03 = ToUV(new Vector4(-p, p));
			var _P13 = ToUV(new Vector4(-w - bevel, p));
			var _P23 = ToUV(new Vector4(w + bevel, p));
			var _P33 = ToUV(new Vector4(p, p));

			material.SetVector("_P00", _P00);
			material.SetVector("_P10", _P10);
			material.SetVector("_P20", _P20);
			material.SetVector("_P30", _P30);

			material.SetVector("_P01", _P01);
			material.SetVector("_P11", _P11);
			material.SetVector("_P21", _P21);
			material.SetVector("_P31", _P31);

			material.SetVector("_P02", _P02);
			material.SetVector("_P12", _P12);
			material.SetVector("_P22", _P22);
			material.SetVector("_P32", _P32);

			material.SetVector("_P03", _P03);
			material.SetVector("_P13", _P13);
			material.SetVector("_P23", _P23);
			material.SetVector("_P33", _P33);
		}
	}
}
