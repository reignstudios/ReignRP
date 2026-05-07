using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	[RequireComponent(typeof(Camera))]
	public abstract class ReignPostProcess : MonoBehaviour
	{
		public bool previewInSceneView = true;

		public virtual bool IsSupported(ReignPostProcessResources resources)
        {
			return true;
        }

		public abstract void OnPostProcess(ReignPostProcessResources resources, CommandBuffer cmd, ref ScriptableRenderContext context, Texture src, RenderTexture dst);

		private static Mesh mesh;
		public static Mesh GetBlitMesh()
		{
			if (mesh != null) return mesh;

			float size = 1;
			var vertices = new Vector3[4]
			{
				new Vector3(-size, -size, 0),
				new Vector3(-size, size, 0),
				new Vector3(size, size, 0),
				new Vector3(size, -size, 0)
			};

			int top = 1, bottom = 0;
			if (SystemInfo.graphicsUVStartsAtTop)
			{
				top = 0;
				bottom = 1;
			}
			var uvs = new Vector2[4]
			{
				new Vector2(0, bottom),
				new Vector2(0, top),
				new Vector2(1, top),
				new Vector2(1, bottom)
			};

			var indices = new int[6]
			{
				0, 1, 2,
				0, 2, 3
			};

			mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.SetIndices(indices, MeshTopology.Triangles, 0);
			mesh.Optimize();
			return mesh;
		}
	}

	public class ReignPostProcessResources
	{
		public readonly int width, height;
		public readonly Camera camera;
		public readonly Texture baseColorTexture, materialTexture, normalTexture, emissionTexture;
		public readonly Texture screenSpaceRoughnessTex, velocityTexture;
		public readonly GraphicsFormat lightFormat;

		public ReignPostProcessResources
		(
			int width, int height,
			Camera camera,
			Texture baseColorTexture, Texture materialTexture, Texture normalTexture, Texture emissionTexture,
			Texture screenSpaceRoughnessTex, Texture velocityTexture,
			GraphicsFormat lightFormat
		)
		{
			this.width = width;
			this.height = height;

			this.camera = camera;

			this.baseColorTexture = baseColorTexture;
			this.materialTexture = materialTexture;
			this.normalTexture = normalTexture;
			this.emissionTexture = emissionTexture;

			this.screenSpaceRoughnessTex = screenSpaceRoughnessTex;
			this.velocityTexture = velocityTexture;

			this.lightFormat = lightFormat;
		}
	}
}