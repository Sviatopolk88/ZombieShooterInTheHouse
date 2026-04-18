using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPWebGLOptimizer
{
    private const string MenuRoot = "Tools/URP WebGL/";
    private const string AssetPath = "Assets/Settings/URP_WebGL.asset";
    private const string RendererPath = "Assets/Settings/URP_WebGL_Renderer.asset";

    [MenuItem(MenuRoot + "Create or Update Minimal URP for WebGL")]
    public static void CreateOrUpdateMinimalUrpForWebGL()
    {
        EnsureFolder("Assets/Settings");

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
        if (urp == null)
        {
            urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(urp, AssetPath);
        }

        ConfigureRenderer(renderer);
        ConfigureUrpAsset(urp, renderer);
        ConfigureGraphicsSettings(urp);
        ConfigureQualitySettings(urp);
        ConfigurePlayerSettings();
        CleanupVolumeProfiles();

        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(urp);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "URP WebGL optimization applied.\n" +
            $"URP Asset: {AssetPath}\n" +
            $"Renderer: {RendererPath}\n" +
            "Check Player Settings manually for anything your project must keep (exceptions, stripping, API level, compression)."
        );
    }

    [MenuItem(MenuRoot + "Strip PostFX from All Volume Profiles")]
    public static void CleanupVolumeProfiles()
    {
        var guids = AssetDatabase.FindAssets("t:VolumeProfile");
        int changedProfiles = 0;
        int removedComponents = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
                continue;

            bool changed = false;
            var remove = new List<VolumeComponent>();

            foreach (var c in profile.components)
            {
                if (c == null)
                    continue;

                var typeName = c.GetType().Name;
                if (ShouldRemoveVolumeComponent(typeName))
                    remove.Add(c);
            }

            foreach (var c in remove)
            {
                profile.components.Remove(c);
                UnityEngine.Object.DestroyImmediate(c, true);
                removedComponents++;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(profile);
                changedProfiles++;
            }
        }

        if (changedProfiles > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"VolumeProfile cleanup complete. Profiles changed: {changedProfiles}, components removed: {removedComponents}");
    }

    private static void ConfigureRenderer(UniversalRendererData renderer)
    {
        var so = new SerializedObject(renderer);

        // Remove renderer features such as SSAO, decals, render objects, fullscreen features.
        TryClearArrayProperty(so, "m_RendererFeatures");
        TryClearArrayProperty(so, "m_RendererFeatureMap");

        // Conservative defaults for a lightweight WebGL renderer.
        TrySetEnumByName(so, "m_RenderingMode", "Forward");
        TrySetBool(so, "m_DepthPrimingMode", false);
        TrySetBool(so, "m_AccurateGbufferNormals", false);
        TrySetBool(so, "m_IntermediateTextureMode", false);
        TrySetBool(so, "m_ShadowTransparentReceive", false);
        TrySetBool(so, "m_CopyDepthMode", false);
        TrySetBool(so, "m_UseNativeRenderPass", false);
        TrySetBool(so, "m_UseAdaptivePerformance", false);

        // Rendering layers and decals related properties can vary by version.
        TrySetBool(so, "m_RenderingLayers", false);
        TrySetBool(so, "m_UseRenderingLayers", false);
        TrySetBool(so, "m_DecalLayers", false);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureUrpAsset(UniversalRenderPipelineAsset urp, UniversalRendererData renderer)
    {
        var so = new SerializedObject(urp);

        // Point asset to our single renderer.
        BindRendererToPipelineAsset(urp, renderer, so);

        // General rendering.
        TrySetBool(so, "m_RequireDepthTexture", false);
        TrySetBool(so, "m_RequireOpaqueTexture", false);
        TrySetBool(so, "m_SupportsHDR", false);
        TrySetBool(so, "m_SupportsMainLightShadows", false);
        TrySetBool(so, "m_SupportsAdditionalLightShadows", false);
        TrySetBool(so, "m_SupportsAdditionalLights", false);
        TrySetBool(so, "m_SupportsMixedLighting", false);
        TrySetBool(so, "m_SupportsLightCookies", false);
        TrySetBool(so, "m_SupportsSoftShadows", false);
        TrySetBool(so, "m_UseFastSRGBLinearConversion", false);
        TrySetBool(so, "m_SupportsDynamicBatching", true);
        TrySetBool(so, "m_ConservativeEnclosingSphere", false);
        TrySetBool(so, "m_EnableLODCrossFade", false);
        TrySetBool(so, "m_SupportDataDrivenLensFlare", false);
        TrySetBool(so, "m_SupportProbeVolume", false);
        TrySetBool(so, "m_EnableRenderGraph", false);
        TrySetBool(so, "m_SupportScreenSpaceShadows", false);
        TrySetBool(so, "m_StoreActionsOptimization", true);

        // Reflection probe options.
        TrySetBool(so, "m_ReflectionProbeBlending", false);
        TrySetBool(so, "m_ReflectionProbeBoxProjection", false);

        // Terrain / grass related features.
        TrySetBool(so, "m_SupportsTerrainHoles", false);
        TrySetBool(so, "m_SupportsTerrainAdaptiveProbeVolumes", false);

        // MSAA and render scale.
        TrySetInt(so, "m_MSAA", 1); // Off / 1x
        TrySetFloat(so, "m_RenderScale", 1.0f);

        // Light limits.
        TrySetInt(so, "m_MainLightRenderingMode", 0);
        TrySetInt(so, "m_AdditionalLightsRenderingMode", 0);
        TrySetInt(so, "m_AdditionalLightsPerObjectLimit", 0);

        // Shadow settings.
        TrySetInt(so, "m_MainLightShadowmapResolution", 256);
        TrySetInt(so, "m_AdditionalLightsShadowmapResolution", 256);
        TrySetInt(so, "m_ShadowDistance", 0);
        TrySetInt(so, "m_CascadeCount", 1);
        TrySetFloat(so, "m_Cascade2Split", 0.25f);
        TrySetFloat(so, "m_Cascade3Split", 0.1f);
        TrySetVector3(so, "m_Cascade4Split", new Vector3(0.067f, 0.2f, 0.467f));

        so.ApplyModifiedPropertiesWithoutUndo();

        // Camera defaults where the API is stable enough.
        TrySetPublicProperty(urp, "upscalingFilter", EnumByName("UpscalingFilterSelection", "Auto"));
        TrySetPublicProperty(urp, "fsrOverrideSharpness", 0.0f);
    }

    private static void ConfigureGraphicsSettings(UniversalRenderPipelineAsset urp)
    {
        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = urp;
        // Do not touch Always Included Shaders here.
        // This list is not exposed through a stable public API across Unity versions.

        ConfigureUrpGlobalStrippingSettings();
    }

    private static void ConfigureQualitySettings(UniversalRenderPipelineAsset urp)
    {
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;

            try
            {
                QualitySettings.SetQualityLevel(i, false);
                SetQualityRenderPipeline(i, urp);
            }
            catch
            {
                // Ignore version/platform-specific issues.
            }
        }
    }

    private static void ConfigurePlayerSettings()
    {
        // Safe editor-side build-size optimizations.
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
        SetBestApiCompatibilityLevelForWebGL();
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = false;
    }
    private static void SetBestApiCompatibilityLevelForWebGL()
    {
        try
        {
            var apiType = typeof(ApiCompatibilityLevel);
            var names = Enum.GetNames(apiType);

            string preferred = names.FirstOrDefault(n =>
                string.Equals(n, "NET_Standard_2_1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "NET_Standard", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, ".NET_Standard_2_1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, ".NET_Standard", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(preferred))
            {
                var value = (ApiCompatibilityLevel)Enum.Parse(apiType, preferred);
                PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, value);
            }
        }
        catch
        {
            // Ignore if this Unity version exposes a different API compatibility surface.
        }
    }

    private static void ConfigureUrpGlobalStrippingSettings()

    {
        // URP Global Settings asset is internal/private in some versions.
        // We update it via SerializedObject reflection when available.
        var urpEditorAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Unity.RenderPipelines.Universal.Editor");
        if (urpEditorAssembly == null)
            return;

        var utilType = urpEditorAssembly.GetType("UnityEditor.Rendering.Universal.UniversalRenderPipelineGlobalSettingsUtils");
        if (utilType == null)
            return;

        var method = utilType.GetMethod("Ensure", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                  ?? utilType.GetMethod("GetOrCreateUniversalRenderPipelineGlobalSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                  ?? utilType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                      .FirstOrDefault(m => m.ReturnType != null && m.ReturnType.Name.Contains("UniversalRenderPipelineGlobalSettings"));

        if (method == null)
            return;

        var globalSettings = method.GetParameters().Length == 0 ? method.Invoke(null, null) : null;
        if (globalSettings == null)
            return;

        var so = new SerializedObject((UnityEngine.Object)globalSettings);
        bool changed = false;

        changed |= TrySetBool(so, "m_StripDebugVariants", true);
        changed |= TrySetBool(so, "m_StripUnusedPostProcessingVariants", true);
        changed |= TrySetBool(so, "m_StripUnusedVariants", true);
        changed |= TrySetBool(so, "m_StripScreenCoordOverrideVariants", true);

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty((UnityEngine.Object)globalSettings);
        }
    }

    private static void BindRendererToPipelineAsset(UniversalRenderPipelineAsset urp, UniversalRendererData renderer, SerializedObject so)
    {
        // Try serialized properties first.
        if (TryAssignObjectReference(so, "m_DefaultRenderer", renderer))
        {
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        TryAssignRendererList(so, renderer);
        so.ApplyModifiedPropertiesWithoutUndo();

        // Fallbacks through reflection for version differences.
        TrySetPublicProperty(urp, "scriptableRendererData", renderer);
        TrySetPublicProperty(urp, "rendererDataList", new ScriptableRendererData[] { renderer });
        TrySetPublicProperty(urp, "defaultRendererIndex", 0);
    }

    private static bool TryAssignRendererList(SerializedObject so, UniversalRendererData renderer)
    {
        string[] candidates =
        {
            "m_RendererDataList",
            "m_Renderers",
            "m_RendererData"
        };

        foreach (var name in candidates)
        {
            var prop = so.FindProperty(name);
            if (prop == null)
                continue;

            if (prop.isArray)
            {
                prop.arraySize = 1;
                var element = prop.GetArrayElementAtIndex(0);
                if (element != null && element.propertyType == SerializedPropertyType.ObjectReference)
                {
                    element.objectReferenceValue = renderer;
                    return true;
                }
            }
            else if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = renderer;
                return true;
            }
        }

        return false;
    }

    private static bool ShouldRemoveVolumeComponent(string typeName)
    {
        string[] removeNames =
        {
            "Bloom",
            "ChromaticAberration",
            "ColorAdjustments",
            "DepthOfField",
            "FilmGrain",
            "LensDistortion",
            "LiftGammaGain",
            "MotionBlur",
            "PaniniProjection",
            "ShadowsMidtonesHighlights",
            "SplitToning",
            "Tonemapping",
            "Vignette",
            "WhiteBalance",
            "ChannelMixer"
        };

        return removeNames.Contains(typeName, StringComparer.Ordinal);
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        var parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void SetQualityRenderPipeline(int qualityIndex, RenderPipelineAsset asset)
    {
        var method = typeof(QualitySettings).GetMethod(
            "SetRenderPipelineAssetAt",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(int), typeof(RenderPipelineAsset) },
            null);

        if (method != null)
        {
            method.Invoke(null, new object[] { qualityIndex, asset });
            return;
        }

        var property = typeof(QualitySettings).GetProperty("renderPipeline", BindingFlags.Public | BindingFlags.Static);
        property?.SetValue(null, asset);
    }

    private static bool TryAssignObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
            return false;

        prop.objectReferenceValue = value;
        return true;
    }

    private static bool TryClearArrayProperty(SerializedObject so, string propertyName)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null || !prop.isArray)
            return false;

        prop.ClearArray();
        return true;
    }

    private static bool TrySetBool(SerializedObject so, string propertyName, bool value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
            return false;

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Boolean:
                prop.boolValue = value;
                return true;
            case SerializedPropertyType.Integer:
                prop.intValue = value ? 1 : 0;
                return true;
            default:
                return false;
        }
    }

    private static bool TrySetInt(SerializedObject so, string propertyName, int value)
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

    private static bool TrySetFloat(SerializedObject so, string propertyName, float value)
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

    private static bool TrySetVector3(SerializedObject so, string propertyName, Vector3 value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
            return false;

        if (prop.propertyType == SerializedPropertyType.Vector3)
        {
            prop.vector3Value = value;
            return true;
        }

        return false;
    }

    private static bool TrySetEnumByName(SerializedObject so, string propertyName, string enumName)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null || prop.propertyType != SerializedPropertyType.Enum)
            return false;

        for (int i = 0; i < prop.enumDisplayNames.Length; i++)
        {
            if (string.Equals(prop.enumDisplayNames[i], enumName, StringComparison.OrdinalIgnoreCase))
            {
                prop.enumValueIndex = i;
                return true;
            }
        }

        for (int i = 0; i < prop.enumNames.Length; i++)
        {
            if (string.Equals(prop.enumNames[i], enumName, StringComparison.OrdinalIgnoreCase))
            {
                prop.enumValueIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TrySetPublicProperty(object target, string propertyName, object value)
    {
        if (target == null)
            return false;

        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop == null || !prop.CanWrite)
            return false;

        try
        {
            if (value == null)
            {
                prop.SetValue(target, null);
                return true;
            }

            if (prop.PropertyType.IsAssignableFrom(value.GetType()))
            {
                prop.SetValue(target, value);
                return true;
            }

            if (prop.PropertyType.IsEnum && value is Enum e)
            {
                prop.SetValue(target, e);
                return true;
            }
        }
        catch
        {
            // Ignore version-specific property mismatch.
        }

        return false;
    }

    private static object EnumByName(string typeName, string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetTypes().FirstOrDefault(t => t.Name == typeName && t.IsEnum);
            if (type == null)
                continue;

            try
            {
                return Enum.Parse(type, name);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
