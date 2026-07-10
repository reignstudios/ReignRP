using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Reign.SRP.Editor
{
	public static class TextureTools
	{
		private const string combineTextures = "Assets/Reign/Combine Textures";

		[MenuItem(combineTextures, true)]
		private static bool CombineTextures_Validate()
		{
			var objs = Selection.objects;
			//return objs.Length == 2 && objs.All(x => x.GetType() == typeof(Texture2D));
			return objs.Length == 3 && objs.Count(x => x.GetType() == typeof(Texture2D)) == 2 && objs.Count(x => x.GetType() == typeof(Material)) == 1;
		}

		[MenuItem(combineTextures, false)]
		private static void CombineTextures()
		{
			// grab source textures
			var objs = Selection.objects;
			//var colorTexture = (Texture2D)objs[0];
			//var aoTexture = (Texture2D)objs[1];
			var textures = objs.Where(x => x is Texture2D).ToArray();
			var colorTexture = (Texture2D)textures[0];
			var aoTexture = (Texture2D)textures[1];
			var material = (Material)objs.First(x => x is Material);
			var targetTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);

			var activeTarget = RenderTexture.active;
			try
			{
				using (var cmd = new CommandBuffer())
				{
					material.SetTexture("_MainTex2", aoTexture);
					cmd.Blit(colorTexture, targetTexture, material);
					Graphics.ExecuteCommandBuffer(cmd);


				}
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
			finally
			{
				RenderTexture.active = activeTarget;
			}
		}

		/*[MenuItem(combineTextures, false)]
		private static void CombineTextures()
		{
			// grab source textures
			var objs = Selection.objects;
			var baseTexture = (Texture2D)objs[0];
			var aoTexture = (Texture2D)objs[1];
			var baseColors = baseTexture.GetPixels();
			var aoColors = aoTexture.GetPixels();

			// merge source textures
			var mergedTexture = new Texture2D(2, 2, baseTexture.format, false);
			for (int i = 0; i != baseColors.Length; ++i)
			{
				baseColors[i].a = (aoColors[i].r + aoColors[i].g + aoColors[i].b) / 3.0f;
			}
			mergedTexture.SetPixels(baseColors);

			// determind encoding method & file ext
			byte[] mergedData;
			string ext;
			switch (baseTexture.format)
			{
				case TextureFormat.RGBA32:
				{
					ext = ".tga";
					mergedData = mergedTexture.EncodeToTGA();
					break;
				}

				default: throw new NotSupportedException("Unsupported texture format:" + baseTexture.format.ToString());
			}
			// save file
			string path = AssetDatabase.AssetPathToGUID(Selection.assetGUIDs[0]);
			string filename = Path.Combine(Application.dataPath, path + ext);
			//File.WriteAllBytes(filename, mergedData);
		}*/
	}
}