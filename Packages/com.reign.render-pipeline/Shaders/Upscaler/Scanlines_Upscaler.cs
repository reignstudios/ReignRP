using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	public sealed class Scanlines_Upscaler : ReignRP_Upscaler
	{
		public Shader shader;
		private Material material;

		public Texture2D mask;
		public float brightness = 0, contrast = 1, pow = 1, postContrast = 1;
		public float maskScale = 2;
		public float bloomPosX, bloomNegX, bloomPosY, bloomNegY;

		private void Start()
		{
			// needed for enabled to show in editor
		}

		public override void OnUpscale(ReignRP_UpscalerResources resources, CommandBuffer cmd, in ScriptableRenderContext context, RenderTexture src, RenderTargetIdentifier dst)
		{
			// validate resources
			if (shader == null)
			{
				Debug.LogError("Scanlines resource is null");
				return;
			}

			// make sure init
			if (material == null) material = new Material(shader);

			// ensure src sampler state
			ReignRP.SetTextureSamplerState(src, FilterMode.Bilinear, TextureWrapMode.Clamp);

			// clear cmd
			cmd.Clear();

			// get temps
			var desc = new RenderTextureDescriptor(resources.width, resources.height, src.format, 0, 1);
			var maskedTexture = RenderTexture.GetTemporary(desc);

			// blit scanlines
			material.SetTexture("_MaskTex", mask);
			material.SetVector("args", new Vector4(brightness, contrast, pow, postContrast));
			material.SetFloat("maskScale", maskScale);
			material.SetVector("bloomCounts", new Vector4(bloomPosX, bloomNegX, bloomPosY, bloomNegY));
			var camera = resources.camera;
			cmd.SetGlobalVector("upscaleTargetSize", new Vector4(1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight, camera.pixelWidth, camera.pixelHeight));
			cmd.Blit(src, maskedTexture, material, 0);
			cmd.Blit(maskedTexture, dst, material, 1);

			// execute cmd
			context.ExecuteCommandBuffer(cmd);

			// release temps
			RenderTexture.ReleaseTemporary(maskedTexture);
		}
	}
}
