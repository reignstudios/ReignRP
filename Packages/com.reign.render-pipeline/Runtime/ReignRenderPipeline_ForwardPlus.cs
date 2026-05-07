using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reign.SRP
{
    public partial class ReignRenderPipeline
    {
		private unsafe void ProcessForwardPlusLights(CameraResource cameraResource, int pointLight_Count, out Vector4 textureSizes)
		{
			bool pointLight_32BitTexturesEnabled = cameraResource.pointLight_32BitTexturesEnabled;

			// grab texture sizes
			int cellWidth = cameraResource.pointLightTexture_Count.width;
			int cellHeight = cameraResource.pointLightTexture_Count.height;
			int positionWidth = cameraResource.pointLightTexture_Positions.width;
			int positionHeight = cameraResource.pointLightTexture_Positions.height;

			// grab & clear CPU texture buffers
			var countArray = (byte*)cameraResource.pointLightArrayPtr_Count.GetUnsafePtr<byte>();
			var colorsArray = (Vector4h*)cameraResource.pointLightArrayPtr_Colors.GetUnsafePtr<Vector4h>();
			ZeroMemory(countArray, cameraResource.pointLightArrayPtr_Count.Length * sizeof(byte));
			//ZeroMemory(colorsArray, cameraResource.pointLightArrayPtr_Colors.Length * sizeof(Vec4h));// NOTE: only needed for testing

			Vector4* positionsArray = null;
			Vector4h* positionsArrayH = null;
			if (pointLight_32BitTexturesEnabled)
			{
				positionsArray = (Vector4*)cameraResource.pointLightArrayPtr_Positions32.GetUnsafePtr<Vector4>();
				//ZeroMemory(positionsArray, cameraResource.pointLightArrayPtr_Positions32.Length * sizeof(Vector4));// NOTE: only needed for testing
			}
			else
			{
				positionsArrayH = (Vector4h*)cameraResource.pointLightArrayPtr_Positions.GetUnsafePtr<Vector4h>();
				//ZeroMemory(positionsArrayH, cameraResource.pointLightArrayPtr_Positions.Length * sizeof(Vec4h));// NOTE: only needed for testing
			}

			// fill processed lights into texture buffers
			int maxLightsPerCell = asset.forwardPlusCellSize * asset.forwardPlusCellSize;
			for (int l = 0; l != pointLight_Count; ++l)
			{
				ref var light = ref forwardPlusVisibleLights_Point[l];

				var rect = light.screenRect;
				int startX = (int)(rect.x * cellWidth);
				int endX = (int)((rect.x + rect.width) * cellWidth) + 1;
				int startY = (int)(rect.y * cellHeight);
				int endY = (int)((rect.y + rect.height) * cellHeight) + 1;

				startX = Mathf.Max(startX, 0);
				endX = Mathf.Min(endX, cellWidth);
				startY = Mathf.Max(startY, 0);
				endY = Mathf.Min(endY, cellHeight);

				if (!pointLight_32BitTexturesEnabled)
				{
					pointLight_PositionsH[l] = new Vector4h(pointLight_Positions[l]);
					pointLight_ColorsH[l] = new Vector4h(pointLight_Colors[l]);
				}

				for (int y = startY; y < endY; ++y)
				{
					for (int x = startX; x < endX; ++x)
					{
						int i = x + (y * cellWidth);
						byte count = countArray[i];
						if (count >= maxLightsPerCell) continue;

						// calculate pixel index
						int cellSize = asset.forwardPlusCellSize;
						int pixelIndex = ((x * cellSize) + (y * positionWidth * cellSize)) +// cell region
							((count % cellSize) + ((count / cellSize) * positionWidth));// pixel within cell region

						// set pixel light data
						if (pointLight_32BitTexturesEnabled) positionsArray[pixelIndex] = pointLight_Positions[l];
						else positionsArrayH[pixelIndex] = pointLight_PositionsH[l];
						colorsArray[pixelIndex] = pointLight_ColorsH[l];

						// increase light count in cell
						countArray[i] = ++count;
					}
				}
			}
			
			// update texture buffers
			cameraResource.pointLightTexture_Count.SetPixelData(cameraResource.pointLightArrayPtr_Count, 0);
			cameraResource.pointLightTexture_Colors.SetPixelData(cameraResource.pointLightArrayPtr_Colors, 0);
			cameraResource.pointLightTexture_Count.Apply();
			cameraResource.pointLightTexture_Colors.Apply();
			if (pointLight_32BitTexturesEnabled) cameraResource.pointLightTexture_Positions.SetPixelData(cameraResource.pointLightArrayPtr_Positions32, 0);
			else cameraResource.pointLightTexture_Positions.SetPixelData(cameraResource.pointLightArrayPtr_Positions, 0);
			cameraResource.pointLightTexture_Positions.Apply();

			// return texture size information
			textureSizes = new Vector4(cellWidth, cellHeight, positionWidth, positionHeight);
		}
    }
}
