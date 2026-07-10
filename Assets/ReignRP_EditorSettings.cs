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
            Configure();
        }

        [InitializeOnLoadMethod]
        private static void Configure()
        {
            ConfigureOpenXRFoveation(BuildTargetGroup.Standalone);
            ConfigureOpenXRFoveation(BuildTargetGroup.Android);
            ConfigureLayers();
        }

        private static void ConfigureOpenXRFoveation(BuildTargetGroup target)
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

        private static void ConfigureLayers()
        {
            SetLayerName(new LayerMaskElement(30, "ReignShadow"), new LayerMaskElement(31, "ReignShadowClip"));
        }

        struct LayerMaskElement
        {
            public int index;
            public string name;

            public LayerMaskElement(int index, string name)
            {
                this.index = index;
                this.name = name;
            }
        }

        private static void SetLayerName(params LayerMaskElement[] layers)
        {
            // get resources
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tagManager.Update();
            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null)
            {
                Debug.LogError("Could not find layers property in TagManager.");
                return;
            }

            // update
            bool changeNeed = false;
            foreach (var layer in layers)
            {
                if (layer.index >= 0 && layer.index < layersProp.arraySize)
                {
                    var layerProp = layersProp.GetArrayElementAtIndex(layer.index);
                    if (layerProp.stringValue != layer.name)
                    {
                        if (!string.IsNullOrEmpty(layerProp.stringValue))
                        {
                            Debug.LogError($"Layer {layer.index} is reserved for ReignRP (clear its value)");
                            continue;
                        }

                        changeNeed = true;
                        layerProp.stringValue = layer.name;
                        Debug.Log($"Set Layer {layer.index} → \"{layer.name}\"");
                    }
                }
            }

            // save
            if (changeNeed)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(tagManager.targetObject);
            }
        }
    }
}
#endif