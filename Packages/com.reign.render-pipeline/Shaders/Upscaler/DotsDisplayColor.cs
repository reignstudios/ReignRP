using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	public sealed class DotsDisplayColor : ReignRP_Upscaler
	{
		public Shader shader;
		private Material material;

		public int displayBit = 56;
		
		public Texture2D mask;
		public int maskScale = 1;

		public Texture2D background;

		public int shadowSamples = 4;

		private void Start()
		{
			// needed for enabled to show in editor
		}

		public override void OnUpscale(ReignRP_UpscalerResources resources, CommandBuffer cmd, in ScriptableRenderContext context, RenderTexture src, RenderTargetIdentifier dst)
		{
			// validate resources
			if (shader == null)
			{
				Debug.LogError("DotsDisplay resource is null");
				return;
			}

			// make sure init
			if (material == null) material = new Material(shader);

			// clear cmd
			cmd.Clear();

			// get dots rez based on mask scale
			int dotsWidth = resources.width / (mask.width * maskScale);
			int dotsHeight = resources.height / (mask.height * maskScale);

			// size source to dots size
			ReignRP.SetTextureSamplerState(src, FilterMode.Bilinear, TextureWrapMode.Clamp);
			var desc = new RenderTextureDescriptor(dotsWidth, dotsHeight, RenderTextureFormat.Default, 0, 1);
			var dotsSource = RenderTexture.GetTemporary(desc);
			ReignRP.SetTextureSamplerState(dotsSource, FilterMode.Point, TextureWrapMode.Clamp);
			cmd.Blit(src, dotsSource, material, 0);

			// force shadow samples to at least 1
			if (shadowSamples <= 0) shadowSamples = 1;

			// blit scanlines
			material.SetTexture("_MaskTex", mask);
			material.SetTexture("_BackgroundTex", background);
			material.SetFloat("shadowSamples", shadowSamples);
			material.SetFloat("displayBit", displayBit);
			cmd.SetGlobalVector("upscaleTargetSize", new Vector4(1.0f / resources.width, 1.0f / resources.height, resources.width, resources.height));
			cmd.Blit(dotsSource, dst, material, 1);

			// execute cmd
			context.ExecuteCommandBuffer(cmd);

			// release temps
			RenderTexture.ReleaseTemporary(dotsSource);
		}
	}
}