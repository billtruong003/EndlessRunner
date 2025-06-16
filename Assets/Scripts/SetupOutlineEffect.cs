#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.Linq;

public class SetupOutlineEffect : MonoBehaviour
{
    [MenuItem("Tools/Setup Outline Effect")]
    static void SetupOutline()
    {
        // Find the URP Asset
        var urpAssets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
            .Select(guid => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();

        if (urpAssets.Count == 0)
        {
            Debug.LogError("No URP Asset found! Please create one first.");
            return;
        }

        var urpAsset = urpAssets[0];
        Debug.Log($"Found URP Asset: {urpAsset.name}");

        // Enable required textures
        var serializedObject = new SerializedObject(urpAsset);
        serializedObject.FindProperty("m_RequireDepthTexture").boolValue = true;
        serializedObject.FindProperty("m_RequireOpaqueTexture").boolValue = true;
        serializedObject.ApplyModifiedProperties();

        Debug.Log("✓ Enabled Depth and Opaque textures");

        // Get the renderer
        var rendererDataList = urpAsset.rendererDataList;
        if (rendererDataList == null || rendererDataList.Length == 0)
        {
            Debug.LogError("No renderer data found in URP Asset!");
            return;
        }

        var rendererData = rendererDataList[0] as UniversalRendererData;
        if (rendererData == null)
        {
            Debug.LogError("Renderer data is not UniversalRendererData!");
            return;
        }

        // Check if OutlineFeature already exists
        var rendererFeatures = rendererData.rendererFeatures;
        bool hasOutlineFeature = rendererFeatures.Any(f => f != null && f.GetType().Name == "OutlineFeature");

        if (!hasOutlineFeature)
        {
            // Create OutlineFeature instance
            var outlineFeature = ScriptableObject.CreateInstance("OutlineFeature") as ScriptableRendererFeature;
            if (outlineFeature != null)
            {
                outlineFeature.name = "Outline Feature";

                // Add to renderer
                AssetDatabase.AddObjectToAsset(outlineFeature, rendererData);
                AssetDatabase.SaveAssets();

                // Add to renderer features list
                var serializedRenderer = new SerializedObject(rendererData);
                var featuresProperty = serializedRenderer.FindProperty("m_RendererFeatures");
                featuresProperty.arraySize++;
                featuresProperty.GetArrayElementAtIndex(featuresProperty.arraySize - 1).objectReferenceValue = outlineFeature;
                serializedRenderer.ApplyModifiedProperties();

                Debug.Log("✓ Added Outline Feature to renderer");
            }
            else
            {
                Debug.LogError("Failed to create OutlineFeature instance!");
                return;
            }
        }
        else
        {
            Debug.Log("✓ Outline Feature already exists");
        }

        // Set as active pipeline
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        Debug.Log("✓ Set as active render pipeline");

        EditorUtility.SetDirty(urpAsset);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Outline Effect Setup Complete! ===");
        Debug.Log("You should now see outlines in the Game view (not Scene view)");
        Debug.Log("Adjust settings in: " + AssetDatabase.GetAssetPath(rendererData));
    }
}
#endif