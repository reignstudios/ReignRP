using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	[RequireComponent(typeof(Camera))]
	public abstract class ReignRP_PostProcess : MonoBehaviour
	{
		public bool previewInSceneView = true;

		public virtual bool IsSupported(ReignRP_PostProcessResources resources)
        {
			return true;
        }

		public abstract void OnPostProcess(ReignRP_PostProcessResources resources, CommandBuffer cmd, in ScriptableRenderContext context, RenderTexture src, RenderTexture dst);

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

	public class ReignRP_PostProcessResources
	{
		public int width { get; private set; }
		public int height { get; private set; }
		public Camera camera { get; private set; }
		public RenderTexture colorTexture { get; private set; }
		//public Texture velocityTexture { get; private set; }

		internal void Update
		(
			int width, int height,
			Camera camera,
			RenderTexture colorTexture
			//Texture velocityTexture
		)
		{
			this.width = width;
			this.height = height;

			this.camera = camera;

			this.colorTexture = colorTexture;
			//this.velocityTexture = velocityTexture;
		}
	}
}