using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
    [RequireComponent(typeof(Camera))]
    public abstract class ReignRP_Upscaler : MonoBehaviour
    {
        public bool previewInSceneView = true;

        public virtual bool IsSupported(ReignRP_UpscalerResources resources)
        {
			return true;
        }

        public abstract void OnUpscale(ReignRP_UpscalerResources resources, CommandBuffer cmd, in ScriptableRenderContext context, RenderTexture src, RenderTargetIdentifier dst);
        public static Mesh GetBlitMesh() => ReignRP_PostProcess.GetBlitMesh();
    }
}
