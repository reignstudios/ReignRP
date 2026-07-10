using UnityEditor;
using UnityEngine;

namespace Reign.SRP
{
	[InitializeOnLoad]
	static class ReignRP_Gismo
	{
		static ReignRP_Gismo()
		{
			SceneView.duringSceneGui += DrawCustomGizmos;
		}

		private static void DrawCustomGizmos(SceneView view)
		{
			if (!Handles.ShouldRenderGizmos()) return;

			// Check if any Light is selected
            bool lightSelected = false;
            foreach (var obj in Selection.gameObjects)
            {
                if (obj != null && obj.GetComponent<Light>() != null)
                {
                    lightSelected = true;
                    break;
                }
            }

            if (!lightSelected) return;

			// draw shadow box
			var reign = ReignRP.singleton;
			if (ReignRP.singleton != null)
			{
				Handles.color = Color.yellow;
				var forward = reign.directionalLight_Rotation * Vector3.forward;
				float scale = Mathf.Max(reign.directionalLight_ShadowScale.x, reign.directionalLight_ShadowScale.y);
				Handles.matrix = Matrix4x4.TRS(reign.directionalLight_Position + (forward * ((reign.directionalLight_ShadowScale.z * .5f) + reign.directionalLight_ShadowNearPlane)), reign.directionalLight_Rotation, new Vector3(scale, scale, reign.directionalLight_ShadowScale.z));
				Handles.DrawWireCube(Vector3.zero, Vector3.one);
			}
		}
	}
}
