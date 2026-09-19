using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	public sealed class Scanlines_Upscaler : ReignRP_Upscaler
	{
		public Shader shader;
		private Material material;

		public Texture2D mask;
		public float brightness = 0, contrast = 1, pow = 1;
		public float maskScale = 2;

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

			// blit scanlines
			material.SetTexture("_MaskTex", mask);
			material.SetVector("args", new Vector4(brightness, contrast, pow, maskScale));
			var camera = resources.camera;
			cmd.SetGlobalVector("upscaleTargetSize", new Vector4(1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight, camera.pixelWidth, camera.pixelHeight));
			cmd.Blit(src, dst, material, 0);

			// execute cmd
			context.ExecuteCommandBuffer(cmd);
		}
	}
}
