#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Reign.SRP.Editor
{
    sealed class ReignRP_EditorSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;// MetaQuestFeatureBuildHooks is 2.

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platformGroup != BuildTargetGroup.Android) return;
            ConfigureAndroidOpenXRFoveation();
        }

        [InitializeOnLoadMethod]
        private static void ConfigureAndroidOpenXRFoveation()
        {
            Configure(BuildTargetGroup.Standalone);
            Configure(BuildTargetGroup.Android);
        }

        private static void Configure(BuildTargetGroup target)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (settings == null) return;

            // set SPR/ReignRP to handle foveated rendering
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            settings.foveatedRenderingApi = OpenXRSettings.BackendFovationApi.SRPFoveation;
            EditorUtility.SetDirty(settings);

            // correct OpenXR Quest Meta settings
            var metaQuestFeature = settings.GetFeature<MetaQuestFeature>();
            if (metaQuestFeature != null)
            {
                var serialized = new SerializedObject(metaQuestFeature);
                serialized.FindProperty("m_foveatedRenderingApi").enumValueIndex = (int)OpenXRSettings.BackendFovationApi.SRPFoveation;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(metaQuestFeature);
            }

            // enable OpenXR foveated feature
            var foveatedFeature = settings.GetFeature<FoveatedRenderingFeature>();
            if (foveatedFeature != null)
            {
                foveatedFeature.enabled = true;
                EditorUtility.SetDirty(foveatedFeature);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif