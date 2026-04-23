using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPWebGLConfigurator
{
    private const string SettingsFolder = "Assets/Settings";
    private const string UrpAssetPath = "Assets/Settings/URP_WebGL.asset";
    private const string RendererPath = "Assets/Settings/URP_WebGL_Renderer.asset";

    [MenuItem("Tools/URP WebGL/Apply Balanced WebGL URP")]
    public static void ApplyBalancedWebGLURP()
    {
        EnsureFolder(SettingsFolder);

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (urp == null)
        {
            urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(urp, UrpAssetPath);
        }

        ConfigureRenderer(renderer);
        ConfigureUrp(urp, renderer);
        ConfigureGraphics(urp);
        ConfigureQuality(urp);
        ConfigurePlayer();

        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(urp);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "URP WebGL config applied.\n" +
            $"URP Asset: {UrpAssetPath}\n" +
            $"Renderer: {RendererPath}\n" +
            "Main Light: ON\n" +
            "Additional Lights: ON\n" +
            "Main Light Shadows: ON\n" +
            "Additional Light Shadows: OFF"
        );
    }

    [MenuItem("Tools/URP WebGL/Remove All Renderer Features")]
    public static void RemoveAllRendererFeatures()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            Debug.LogWarning("Renderer asset not found. First run: Tools/URP WebGL/Apply Balanced WebGL URP");
            return;
        }

        var so = new SerializedObject(renderer);
        bool changed = false;

        changed |= ClearArray(so, "m_RendererFeatures");
        changed |= ClearArray(so, "m_RendererFeatureMap");

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Debug.Log("All renderer features removed.");
        }
        else
        {
            Debug.Log("No renderer features found or field names differ in this Unity version.");
        }
    }

    private static void ConfigureRenderer(UniversalRendererData renderer)
    {
        var so = new SerializedObject(renderer);

        // Remove heavy optional features like SSAO/Decals/RenderObjects if present.
        ClearArray(so, "m_RendererFeatures");
        ClearArray(so, "m_RendererFeatureMap");

        // Best-effort flags, skipped silently if field names differ.
        SetBool(so, "m_UseNativeRenderPass", false);
        SetBool(so, "m_UseRenderingLayers", false);
        SetBool(so, "m_RenderingLayers", false);
        SetBool(so, "m_DecalLayers", false);
        SetBool(so, "m_AccurateGbufferNormals", false);
        SetBool(so, "m_IntermediateTextureMode", false);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureUrp(UniversalRenderPipelineAsset urp, UniversalRendererData renderer)
    {
        var so = new SerializedObject(urp);

        BindRenderer(so, renderer);

        // Keep lighting, but lightweight.
        SetBool(so, "m_SupportsMainLightShadows", true);
        SetBool(so, "m_SupportsAdditionalLights", true);
        SetBool(so, "m_SupportsAdditionalLightShadows", false);
        SetBool(so, "m_SupportsSoftShadows", false);
        SetBool(so, "m_SupportsMixedLighting", true);

        // Disable expensive extras.
        SetBool(so, "m_RequireDepthTexture", false);
        SetBool(so, "m_RequireOpaqueTexture", false);
        SetBool(so, "m_SupportsHDR", false);
        SetBool(so, "m_SupportsLightCookies", false);
        SetBool(so, "m_ReflectionProbeBlending", false);
        SetBool(so, "m_ReflectionProbeBoxProjection", false);
        SetBool(so, "m_EnableLODCrossFade", false);
        SetBool(so, "m_SupportsTerrainHoles", false);
        SetBool(so, "m_SupportDataDrivenLensFlare", false);
        SetBool(so, "m_SupportProbeVolume", false);
        SetBool(so, "m_EnableRenderGraph", false);

        // Rendering / AA.
        SetInt(so, "m_MSAA", 1); // Off / 1x
        SetFloat(so, "m_RenderScale", 1.0f);

        // Light limits.
        SetInt(so, "m_MainLightRenderingMode", 1);
        SetInt(so, "m_AdditionalLightsRenderingMode", 1);
        SetInt(so, "m_AdditionalLightsPerObjectLimit", 4);

        // Shadows: cheap but usable.
        SetInt(so, "m_MainLightShadowmapResolution", 512);
        SetInt(so, "m_AdditionalLightsShadowmapResolution", 256);
        SetFloat(so, "m_ShadowDistance", 20f);
        SetInt(so, "m_CascadeCount", 1);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureGraphics(UniversalRenderPipelineAsset urp)
    {
        GraphicsSettings.defaultRenderPipeline = urp;
    }

    private static void ConfigureQuality(UniversalRenderPipelineAsset urp)
    {
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            TrySetQualityRenderPipeline(i, urp);
        }
    }

    private static void ConfigurePlayer()
    {
#pragma warning disable CS0618
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
#pragma warning restore CS0618

        try
        {
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        }
        catch
        {
            // Ignore if API differs.
        }

        try
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
        }
        catch
        {
            // Ignore if API differs.
        }
    }

    private static void BindRenderer(SerializedObject so, UniversalRendererData renderer)
    {
        // Try common serialized property names across URP versions.
        string[] objectProps =
        {
            "m_DefaultRenderer",
            "m_RendererData"
        };

        foreach (var propName in objectProps)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = renderer;
            }
        }

        string[] arrayProps =
        {
            "m_RendererDataList",
            "m_Renderers"
        };

        foreach (var propName in arrayProps)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = 1;
                var el = prop.GetArrayElementAtIndex(0);
                if (el != null && el.propertyType == SerializedPropertyType.ObjectReference)
                {
                    el.objectReferenceValue = renderer;
                }
            }
        }

        SetInt(so, "m_DefaultRendererIndex", 0);
        SetInt(so, "m_DefaultRendererDataIndex", 0);
    }

    private static bool ClearArray(SerializedObject so, string propertyName)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null || !prop.isArray)
            return false;

        prop.ClearArray();
        return true;
    }

    private static bool SetBool(SerializedObject so, string propertyName, bool value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
            return false;

        if (prop.propertyType == SerializedPropertyType.Boolean)
        {
            prop.boolValue = value;
            return true;
        }

        if (prop.propertyType == SerializedPropertyType.Integer)
        {
            prop.intValue = value ? 1 : 0;
            return true;
        }

        return false;
    }

    private static bool SetInt(SerializedObject so, string propertyName, int value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
            return false;

        if (prop.propertyType == SerializedPropertyType.Integer)
        {
            prop.intValue = value;
            return true;
        }

        if (prop.propertyType == SerializedPropertyType.Enum)
        {
            prop.enumValueIndex = Mathf.Clamp(value, 0, Math.Max(0, prop.enumDisplayNames.Length - 1));
            return true;
        }

        return false;
    }

    private static bool SetFloat(SerializedObject so, string propertyName, float value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
            return false;

        if (prop.propertyType == SerializedPropertyType.Float)
        {
            prop.floatValue = value;
            return true;
        }

        return false;
    }

    private static void TrySetQualityRenderPipeline(int qualityIndex, RenderPipelineAsset asset)
    {
        var method = typeof(QualitySettings).GetMethod(
            "SetRenderPipelineAssetAt",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static,
            null,
            new[] { typeof(int), typeof(RenderPipelineAsset) },
            null);

        if (method != null)
        {
            method.Invoke(null, new object[] { qualityIndex, asset });
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}