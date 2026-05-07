using Reign.SRP;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/*[ExecuteInEditMode]
public class ExposeRenderPipeline : MonoBehaviour
{
    public Texture2D exposedTexture_Colors;

    //Color[] colors;
    NativeArray<Vec4h> colors;

	private unsafe void Update()
    {
        if (exposedTexture_Colors == null)
        {
            exposedTexture_Colors = new Texture2D(128, 64, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);

            //colors = new Color[exposedTexture_Colors.width * exposedTexture_Colors.height];
            //for (int i = 0; i != colors.Length; ++i) colors[i] = new Color(1, 0, 0, 1);
            //exposedTexture_Colors.SetPixels(colors);
            //exposedTexture_Colors.Apply();

            //if (colors.IsCreated) colors.Dispose();
            //colors = new NativeArray<Color>(exposedTexture_Colors.width * exposedTexture_Colors.height, Allocator.Temp);
            //for (int i = 0; i != colors.Length; ++i) colors[i] = new Color(1, 0, 0, 1);
            //exposedTexture_Colors.SetPixelData<Color>(colors, 0);
            //exposedTexture_Colors.Apply();

            if (colors.IsCreated) colors.Dispose();
            colors = new NativeArray<Vec4h>(exposedTexture_Colors.width * exposedTexture_Colors.height, Allocator.Persistent);
            var colorsPtr = (Vec4h*)colors.GetUnsafePtr<Vec4h>();
            for (int i = 0; i != colors.Length; ++i) colorsPtr[i] = new Vec4h(0, 1, 0, 1);
            exposedTexture_Colors.SetPixelData<Vec4h>(colors, 0);
            exposedTexture_Colors.Apply();
        }
    }

	private void OnDestroy()
	{
		if (colors.IsCreated) colors.Dispose();
	}
}*/

[ExecuteInEditMode]
public class ExposeRenderPipeline : MonoBehaviour
{
    public Texture2D exposedTexture_Count, exposedTexture_Positions, exposedTexture_Colors;

	private unsafe void Update()
    {
        //exposedTexture_Count = ReignRenderPipeline.exposedTexture_Count;
        //exposedTexture_Positions = ReignRenderPipeline.exposedTexture_Positions;
        //exposedTexture_Colors = ReignRenderPipeline.exposedTexture_Colors;
    }
}