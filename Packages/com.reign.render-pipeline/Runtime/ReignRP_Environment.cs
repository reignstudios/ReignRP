using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reign.SRP
{
    [ExecuteInEditMode]
    public class ReignRP_Environment : MonoBehaviour
    {
        internal static float global_ambientIntensity;
        [Range(0f, 8f)] public float ambientSkyIntensity = 1.0f;
        
        internal static Color global_ambientGradient_SkyColor;
        internal static Color global_ambientGradient_EquatorColor;
        internal static Color global_ambientGradient_GroundColor;
        [Space(10)] [ColorUsage(true, true)] public Color ambientGradient_SkyColor = RenderSettings.ambientSkyColor;
        [ColorUsage(true, true)]public Color ambientGradient_EquatorColor = RenderSettings.ambientEquatorColor;
        [ColorUsage(true, true)]public Color ambientGradient_GroundColor = RenderSettings.ambientGroundColor;
        
        internal static Color global_ambientColor;
        [Space(10)] [ColorUsage(true, true)] public Color ambientColor = RenderSettings.ambientSkyColor;

        private void Update()
        {
            if (gameObject.scene != SceneManager.GetActiveScene()) return;
            
            // ambient
            global_ambientIntensity = ambientSkyIntensity;
            
            global_ambientGradient_SkyColor = ambientGradient_SkyColor;
            global_ambientGradient_EquatorColor = ambientGradient_EquatorColor;
            global_ambientGradient_GroundColor = ambientGradient_GroundColor;
            
            global_ambientColor = ambientColor;
        }
    }
}
