using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
	[Serializable, CreateAssetMenu(menuName = "Reign/ReignRenderPipelineResources")]
	public class ReignRenderPipelineResources : ScriptableObject
	{
		[Serializable]
		public sealed class ShaderResources
		{
			public Shader litShader;
			public Shader terrainGrassShader;// TerrainGrass shader
			public Shader terrainGrassBillboardShader;// TerrainGrass shader
			public Shader blitShader;
		}

		[Serializable]
		public sealed class MaterialResources
		{
			public Material litMaterial;
		}

		[Serializable]
		public sealed class MeshResources
		{
			public Mesh skyboxMesh;
			public Mesh pointLightMesh;
			public Mesh pointLightCookieMesh;
		}

        [Serializable]
        public sealed class TextureResources
        {
            public Texture ditherTexture;
        }

        public ShaderResources shaders;
		public MaterialResources materials;
		public MeshResources meshes;
		public TextureResources textures;
    }
}