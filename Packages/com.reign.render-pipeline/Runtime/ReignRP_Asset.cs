using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace Reign.SRP
{
	[CreateAssetMenu(menuName = "Reign/ReignRP_Asset")]
	public class ReignRP_Asset : RenderPipelineAsset
    {
		public static ReignRP_Asset singleton { get; private set; }

		public bool hdr;
		public bool useRenderPasses = false;
		[Tooltip("If UseRenderPasses is on and you have multiple cameras that clear the same target buffer")]
		public bool renderPassesMultiCameraClear = false;

		public int compositionDivision = 1, fullscreenSwapchainResolutionDivision = 1;
		public int maxLights = -1;
		[Tooltip("Uses closes lights to camera")]
		public bool sortPointLights = false;

		public bool enableComposition = false;
		public bool compositionDepthClone = false;
		public MSAA_Level compositionMSAA = MSAA_Level.Off;
		public CommonTextureFormat compositionColorFormat = CommonTextureFormat.UINT_A2_RGB10;
		public DepthBit compositionDepthBit = DepthBit.Bit24;

		public ShadowType shadowType = ShadowType.Hard;
		public ShadowRez shadowResolution = ShadowRez.Rez_1024;
		public ShadowCascades shadowCascades = ShadowCascades.x1;
		public Vector4 shadowCascadePlanes = new Vector4(5, 10, 20, 40);

		public GlobalAmbientMode ambientMode = GlobalAmbientMode.Unity_SceneSettings;

		[Tooltip("Culls & processes Reflection Probes (disable to increase performance)")]
		public bool enableReflectionProbes = true;
		[Tooltip("Culls & processes Lightmaps (disable to increase performance)")]
		public bool enableLightmaps = true;
		[Tooltip("Processes Motion Vectors (disable to increase performance)")]
		public bool enableMotionVectors = false;

		[Tooltip("Show VR preview in App Window or Editor (ignored on mobile HMDs platforms)")]
		public bool xrPreview = true;
		public GameViewRenderMode xrPreviewMode = GameViewRenderMode.BothEyes;

		public ReignRenderPipelineResources resources;
		public override string renderPipelineShaderTag => "ReignRP";
		public override Type pipelineType => typeof(ReignRP);
		public override Shader terrainDetailGrassShader => (resources != null && resources.shaders != null) ? resources.shaders.terrainGrassShader : base.terrainDetailGrassShader;
		public override Shader terrainDetailGrassBillboardShader => (resources != null && resources.shaders != null) ? resources.shaders.terrainGrassBillboardShader : base.terrainDetailGrassBillboardShader;
		public override Shader defaultShader => (resources != null && resources.shaders != null) ? resources.shaders.litShader : base.defaultShader;
		public override Material defaultMaterial => (resources != null && resources.shaders != null) ? new Material(resources.shaders.litShader) : base.defaultMaterial;

		private void Awake()
		{
			ValidateSettings();
		}

		private void OnEnable()
		{
			ValidateSettings();
		}

		protected override void OnValidate()
		{
			ValidateSettings();
			base.OnValidate();
		}

		internal void ValidateSettings()
		{
			if (compositionDivision < 1) compositionDivision = 1;
			else if (compositionDivision > 8) compositionDivision = 8;

			if (fullscreenSwapchainResolutionDivision < 1) fullscreenSwapchainResolutionDivision = 1;
			else if (fullscreenSwapchainResolutionDivision > 8) fullscreenSwapchainResolutionDivision = 8;
		}

		protected override RenderPipeline CreatePipeline()
        {
			singleton = this;
            return new ReignRP(this);
        }
    }

	public enum MSAA_Level
	{
		Off = 1,
		X2 = 2,
		X4 = 4,
		x8 = 8
	}

	/*public enum SkinningMode
	{
		UnityDefault,
		GPU_TextureFloat,
		GPU_TextureHalf,
		GPU_Constants
	}*/

	public enum ShadowRez
	{
		Rez_256,
		Rez_512,
		Rez_1024,
		Rez_2048,
		Rez_4096,
		Rez_8192,
		Rez_16384
	}

	public enum ShadowCascades
	{
		x1 = 1,
		x2,
		x3,
		x4
	}

	public enum ShadowType
	{
		/// <summary>
		/// No shadows
		/// </summary>
		Off,

		/// <summary>
		/// Fast and ugly with point shadow sampling
		/// </summary>
		Hard,

		/// <summary>
		/// Fast and ugly with linear shadow sampling
		/// </summary>
		HardLinear,

		/// <summary>
		/// Same as 'Hard' with slight AA around edges on some platforms
		/// </summary>
		HardAA,

		/// <summary>
		/// Single-pass diffused edges
		/// </summary>
		SoftDiffused,

		/// <summary>
		/// Single-pass blured edges
		/// </summary>
		SoftBlur
	}

	public enum CommonTextureFormat
	{
		/// <summary>
		/// ARGB(5, 6, 5) 16-bit format
		/// </summary>
		[InspectorName("UINT_RGB565")]
		UINT_RGB565,

		/// <summary>
		/// ARGB(4,4,4,4) 16-bit format
		/// </summary>
		[InspectorName("UINT_16")]
		UINT_16,

		/// <summary>
		/// Standard ARGB(8,8,8,8) 32-bit format
		/// </summary>
		[InspectorName("UINT_32")]
		UINT_32,

		/// <summary>
		/// ARGB(2,10,10,10) 32-bit format
		/// </summary>
		[InspectorName("UINT_A2_RGB10")]
		UINT_A2_RGB10,

		/// <summary>
		/// 10-bit (11, 11, 10) 32-bit HDR
		/// </summary>
		[InspectorName("UFloat_10")]
		UFloat_10,

		/// <summary>
		/// 16-bit per channel HDR
		/// </summary>
		[InspectorName("Float_16")]
		Float_16,

		/// <summary>
		/// 32-bit per channel HDR
		/// </summary>
		[InspectorName("Float_32")]
		Float_32
	}

	public enum DepthBit
	{
		Bit16 = 16,
		Bit24 = 24,
		Bit32 = 32
	}

	public enum QueueRange
	{
		Any,
		Opaque,
		Transparent
	}

	public enum GlobalAmbientMode
	{
		Disable,
		Unity_SceneSettings,
		ReignEnv_Sky,
		ReignEnv_Gradient,
		ReignEnv_Color
	}
}