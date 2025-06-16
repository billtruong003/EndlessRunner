using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace EndlessRunner.Editor
{
    public static class URPRenderGraphToggle
    {
        [MenuItem("Tools/URP/Toggle Render Graph")]
        private static void ToggleRenderGraph()
        {
            // Get the URP Global Settings asset through GraphicsSettings
            var globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>();
            if (globalSettings == null)
            {
                Debug.LogError("No Universal Render Pipeline Global Settings found.");
                return;
            }

            // Try to find the property by checking common names
            var propertyInfo = globalSettings.GetType().GetProperty("enableRenderGraph", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (propertyInfo == null)
            {
                propertyInfo = globalSettings.GetType().GetProperty("m_EnableRenderGraph", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (propertyInfo == null)
            {
                Debug.LogError("Could not find 'enableRenderGraph' or 'm_EnableRenderGraph' property. This script may be incompatible with your URP version.");
                return;
            }

            bool currentState = (bool)propertyInfo.GetValue(globalSettings);
            propertyInfo.SetValue(globalSettings, !currentState);
            EditorUtility.SetDirty(globalSettings);
            AssetDatabase.SaveAssets();

            Debug.Log($"Render Graph is now {(!currentState ? "ENABLED" : "DISABLED")} in Universal Render Pipeline Global Settings");
        }
    }
}