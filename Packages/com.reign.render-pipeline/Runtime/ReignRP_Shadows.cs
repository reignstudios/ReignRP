using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
    public partial class ReignRP
    {
		private GameObject shadowGameObject;
		private Camera shadowCamera;
		private Transform shadowTransform;

		public RenderTexture shadowTexture;
		private RenderTargetIdentifier shadowTextureID;

        private void RenderShadowPass_Directional(ref ScriptableRenderContext context, Camera camera)
		{
			// only render shadows for supported views
			var cameraType = camera.cameraType;
			if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
			{
				// TODO: disable shadow shader paths
				return;
			}

			// create shadow texture if needed
			if (!shadowTexture)
			{
				int rez = (int)asset.shadowResolution;
				shadowTexture = new RenderTexture(rez, rez, 16, RenderTextureFormat.Default, 1);
				shadowTexture.useMipMap = false;
				shadowTexture.autoGenerateMips = false;
				shadowTexture.Create();
				shadowTextureID = shadowTexture;
			}

			// create shadow camera if needed
			if (!shadowGameObject)
			{
				shadowGameObject = GameObject.Find("ReignRP Shadow Camera");
				if (!shadowGameObject) shadowGameObject = new GameObject("ReignRP Shadow Camera");
				shadowGameObject.hideFlags = HideFlags.HideAndDontSave;

				shadowCamera = shadowGameObject.GetComponent<Camera>();
				if (!shadowCamera) shadowCamera = shadowGameObject.AddComponent<Camera>();
				shadowCamera.enabled = false;
				shadowCamera.targetTexture = shadowTexture;
				shadowCamera.orthographic = true;
				shadowCamera.orthographicSize = 10;
				shadowCamera.nearClipPlane = 0.1f;
				shadowCamera.farClipPlane = 20;

				shadowTransform = shadowGameObject.transform;
			}

			// configure shadow camera
			shadowTransform.SetPositionAndRotation(directionalLight_Position, directionalLight_Rotation);

			// draw shadows
			var shadowShader = asset.resources.shaders.shadowShader;
			if (shadowShader)
			{
				// cull objects
				if (!shadowCamera.TryGetCullingParameters(false, out var cullingParameters)) Debug.LogError("Failed: TryGetCullingParameters for directional shadow");
				cullingParameters.maximumVisibleLights = 0;
				cullingParameters.cullingOptions = CullingOptions.None;// don't cull anything special
				cullingParameters.shadowDistance = 0;
				var cullResults = context.Cull(ref cullingParameters);

				// draw shadow objects
				cmd.Clear();
				cmd.SetGlobalMatrix("shadowMatrix", GL.GetGPUProjectionMatrix(shadowCamera.projectionMatrix, true) * shadowCamera.worldToCameraMatrix);
				cmd.SetRenderTarget(shadowTextureID);
				cmd.ClearRenderTarget(true, true, Color.white);
				context.ExecuteCommandBuffer(cmd);
				DrawObjects(ref context, ref cullResults, lightModeID_Opaque, QueueRange.Opaque, shadowCamera, overrideShader:shadowShader, overrideMaterialPassIndex:0);
			}
		}
    }
}
