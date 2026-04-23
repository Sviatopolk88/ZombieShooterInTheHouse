using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PolygonParticlesUrpMaterialConverter
{
    private const string TargetFolder = "Assets/Plugins/PolygonParticles";

    private const string UrpLit = "Universal Render Pipeline/Lit";
    private const string UrpUnlit = "Universal Render Pipeline/Unlit";
    private const string UrpParticlesLit = "Universal Render Pipeline/Particles/Lit";
    private const string UrpParticlesUnlit = "Universal Render Pipeline/Particles/Unlit";

    [MenuItem("Tools/PolygonParticles/Report Materials URP Compatibility")]
    public static void ReportMaterials()
    {
        foreach (Material material in LoadTargetMaterials())
        {
            string path = AssetDatabase.GetAssetPath(material);
            string shaderName = GetShaderName(material);
            string problem = GetProblemType(material);
            Debug.Log($"[PolygonParticles URP Report] {path} | shader: {shaderName} | status: {problem}", material);
        }
    }

    [MenuItem("Tools/PolygonParticles/Convert Materials To URP")]
    public static void ConvertMaterials()
    {
        Shader urpLit = Shader.Find(UrpLit);
        Shader urpUnlit = Shader.Find(UrpUnlit);
        Shader urpParticlesLit = Shader.Find(UrpParticlesLit);
        Shader urpParticlesUnlit = Shader.Find(UrpParticlesUnlit);

        if (urpLit == null || urpUnlit == null || urpParticlesLit == null || urpParticlesUnlit == null)
        {
            Debug.LogError(
                "[PolygonParticles URP] Conversion aborted. Required URP shaders were not found. " +
                $"Lit={urpLit != null}, Unlit={urpUnlit != null}, Particles/Lit={urpParticlesLit != null}, Particles/Unlit={urpParticlesUnlit != null}");
            return;
        }

        int changed = 0;
        int skipped = 0;
        List<string> manual = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (Material material in LoadTargetMaterials())
            {
                string path = AssetDatabase.GetAssetPath(material);

                try
                {
                    if (!NeedsConversion(material))
                    {
                        skipped++;
                        Debug.Log($"[PolygonParticles URP] Already URP-compatible, skipped: {path} | shader: {GetShaderName(material)}", material);
                        continue;
                    }

                    MaterialSnapshot snapshot = MaterialSnapshot.Capture(material);
                    string sourceShader = snapshot.ShaderName;
                    Shader targetShader = PickTargetShader(sourceShader, snapshot, urpLit, urpUnlit, urpParticlesLit, urpParticlesUnlit);

                    if (targetShader == null)
                    {
                        skipped++;
                        manual.Add($"{path} | shader: {sourceShader} | no target shader");
                        Debug.LogWarning($"[PolygonParticles URP] Could not convert: {path} | shader: {sourceShader}", material);
                        continue;
                    }

                    Undo.RecordObject(material, "Convert PolygonParticles material to URP");
                    material.shader = targetShader;
                    ApplySnapshot(material, snapshot, targetShader.name);
                    EditorUtility.SetDirty(material);

                    changed++;
                    Debug.Log($"[PolygonParticles URP] Converted: {path} | {sourceShader} -> {targetShader.name}", material);
                }
                catch (Exception ex)
                {
                    skipped++;
                    manual.Add($"{path} | shader: {GetShaderName(material)} | exception: {ex.Message}");
                    Debug.LogError($"[PolygonParticles URP] Failed to convert {path}: {ex}", material);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[PolygonParticles URP] Finished. Changed: {changed}, skipped: {skipped}, manual review: {manual.Count}");
        foreach (string item in manual)
            Debug.LogWarning($"[PolygonParticles URP] Manual review: {item}");
    }

    [MenuItem("Tools/PolygonParticles/Repair Converted Particle Material Settings")]
    public static void RepairConvertedParticleMaterials()
    {
        Shader urpParticlesLit = Shader.Find(UrpParticlesLit);
        Shader urpParticlesUnlit = Shader.Find(UrpParticlesUnlit);

        if (urpParticlesLit == null || urpParticlesUnlit == null)
        {
            Debug.LogError(
                "[PolygonParticles URP] Repair aborted. Required URP particle shaders were not found. " +
                $"Particles/Lit={urpParticlesLit != null}, Particles/Unlit={urpParticlesUnlit != null}");
            return;
        }

        int changed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (Material material in LoadTargetMaterials())
            {
                if (material.shader == null || !material.shader.name.StartsWith("Universal Render Pipeline/Particles/", StringComparison.Ordinal))
                    continue;

                string path = AssetDatabase.GetAssetPath(material);
                bool shouldBeLit = IsLightingLikely(material.name, MaterialSnapshot.Capture(material));
                Shader targetShader = shouldBeLit ? urpParticlesLit : urpParticlesUnlit;

                Undo.RecordObject(material, "Repair PolygonParticles URP particle material");

                if (material.shader != targetShader)
                    material.shader = targetShader;

                ParticleBlendMode blendMode = GuessParticleBlendMode(material);
                ConfigureParticleSurface(material, blendMode);
                EditorUtility.SetDirty(material);

                changed++;
                Debug.Log($"[PolygonParticles URP] Repaired particle settings: {path} | shader: {material.shader.name} | blend: {blendMode}", material);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[PolygonParticles URP] Particle repair finished. Updated: {changed}");
    }

    private static IEnumerable<Material> LoadTargetMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolder });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(TargetFolder + "/", StringComparison.OrdinalIgnoreCase))
                continue;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                yield return material;
        }
    }

    private static bool NeedsConversion(Material material)
    {
        string shaderName = GetShaderName(material);
        if (shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            return false;

        return true;
    }

    private static string GetProblemType(Material material)
    {
        string shaderName = GetShaderName(material);
        if (shaderName == "<missing>")
            return "missing shader";
        if (shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            return "ok";
        if (shaderName == "Standard" || shaderName.StartsWith("Legacy Shaders/", StringComparison.Ordinal) || shaderName == "Unlit/Texture")
            return "built-in shader";
        if (shaderName.StartsWith("Particles/", StringComparison.Ordinal) || shaderName.StartsWith("Mobile/", StringComparison.Ordinal))
            return "built-in particle/mobile shader";

        return "custom or non-URP shader";
    }

    private static Shader PickTargetShader(
        string sourceShader,
        MaterialSnapshot snapshot,
        Shader urpLit,
        Shader urpUnlit,
        Shader urpParticlesLit,
        Shader urpParticlesUnlit)
    {
        if (sourceShader == "Standard")
            return urpLit;

        if (sourceShader == "Unlit/Texture")
            return urpUnlit;

        if (IsParticleLike(sourceShader))
            return IsLightingLikely(sourceShader, snapshot) ? urpParticlesLit : urpParticlesUnlit;

        if (sourceShader.StartsWith("Legacy Shaders/", StringComparison.Ordinal))
            return urpLit;

        if (sourceShader.StartsWith("Mobile/", StringComparison.Ordinal))
            return IsLightingLikely(sourceShader, snapshot) ? urpParticlesLit : urpParticlesUnlit;

        return IsLightingLikely(sourceShader, snapshot) ? urpParticlesLit : urpParticlesUnlit;
    }

    private static bool IsParticleLike(string shaderName)
    {
        return shaderName.StartsWith("Particles/", StringComparison.Ordinal) ||
               shaderName.Contains("Particle", StringComparison.OrdinalIgnoreCase) ||
               shaderName.Contains("Additive", StringComparison.OrdinalIgnoreCase) ||
               shaderName.Contains("Blended", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLightingLikely(string shaderName, MaterialSnapshot snapshot)
    {
        if (shaderName.Contains("Unlit", StringComparison.OrdinalIgnoreCase))
            return false;

        return shaderName.Contains("Lit", StringComparison.OrdinalIgnoreCase) ||
               shaderName.Contains("VertexLit", StringComparison.OrdinalIgnoreCase) ||
               snapshot.HasNormalMap ||
               snapshot.Metallic > 0.001f;
    }

    private static void ApplySnapshot(Material material, MaterialSnapshot snapshot, string targetShaderName)
    {
        bool transparent = snapshot.IsTransparent;
        bool alphaClip = snapshot.IsAlphaClip && !snapshot.IsAdditive;

        SetTexture(material, "_BaseMap", snapshot.MainTexture, snapshot.MainTextureScale, snapshot.MainTextureOffset);
        SetColor(material, "_BaseColor", snapshot.BaseColor);
        SetColor(material, "_Color", snapshot.BaseColor);

        if (targetShaderName == UrpLit)
        {
            SetTexture(material, "_BumpMap", snapshot.BumpMap, snapshot.BumpScale, snapshot.BumpOffset);
            SetTexture(material, "_EmissionMap", snapshot.EmissionMap, snapshot.EmissionScale, snapshot.EmissionOffset);
            SetColor(material, "_EmissionColor", snapshot.EmissionColor);
            SetFloat(material, "_Metallic", snapshot.Metallic);
            SetFloat(material, "_Smoothness", snapshot.Smoothness);
        }

        if (targetShaderName == UrpParticlesLit || targetShaderName == UrpParticlesUnlit)
            ConfigureParticleSurface(material, GuessParticleBlendMode(material, snapshot));
        else
            ConfigureLitOrUnlitSurface(material, transparent, alphaClip, snapshot.IsAdditive, snapshot.IsPremultiply);

        EnableEmissionIfNeeded(material, snapshot);
        material.renderQueue = snapshot.CustomRenderQueue >= 0 ? snapshot.CustomRenderQueue : material.renderQueue;
    }

    private static ParticleBlendMode GuessParticleBlendMode(Material material)
    {
        string materialName = material.name ?? string.Empty;

        if (materialName.Contains("Multiply", StringComparison.OrdinalIgnoreCase) ||
            materialName.Contains("Dark", StringComparison.OrdinalIgnoreCase))
        {
            return ParticleBlendMode.Multiply;
        }

        if (materialName.Contains("Premultiply", StringComparison.OrdinalIgnoreCase))
            return ParticleBlendMode.Premultiply;

        if (materialName.Contains("Additive", StringComparison.OrdinalIgnoreCase))
            return ParticleBlendMode.Additive;

        if (material.HasProperty("_Blend"))
            return (ParticleBlendMode)Mathf.RoundToInt(material.GetFloat("_Blend"));

        return ParticleBlendMode.Alpha;
    }

    private static ParticleBlendMode GuessParticleBlendMode(Material material, MaterialSnapshot snapshot)
    {
        string materialName = material.name ?? string.Empty;

        if (materialName.Contains("Multiply", StringComparison.OrdinalIgnoreCase) ||
            materialName.Contains("Dark", StringComparison.OrdinalIgnoreCase))
        {
            return ParticleBlendMode.Multiply;
        }

        if (snapshot.IsAdditive)
            return ParticleBlendMode.Additive;

        if (snapshot.IsPremultiply)
            return ParticleBlendMode.Premultiply;

        return ParticleBlendMode.Alpha;
    }

    private static void ConfigureParticleSurface(Material material, ParticleBlendMode blendMode)
    {
        SetFloat(material, "_Surface", 1f);
        SetFloat(material, "_Blend", (float)blendMode);
        SetFloat(material, "_AlphaClip", 0f);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
        SetFloat(material, "_SrcBlend", GetParticleSrcBlend(blendMode));
        SetFloat(material, "_DstBlend", GetParticleDstBlend(blendMode));
        SetFloat(material, "_SrcBlendAlpha", GetParticleSrcBlendAlpha(blendMode));
        SetFloat(material, "_DstBlendAlpha", GetParticleDstBlendAlpha(blendMode));

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");

        if (blendMode == ParticleBlendMode.Multiply)
            material.EnableKeyword("_ALPHAMODULATE_ON");
        else
            material.DisableKeyword("_ALPHAMODULATE_ON");

        if (blendMode == ParticleBlendMode.Premultiply)
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        else
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    private static float GetParticleSrcBlend(ParticleBlendMode blendMode)
    {
        switch (blendMode)
        {
            case ParticleBlendMode.Premultiply:
                return (float)UnityEngine.Rendering.BlendMode.One;
            case ParticleBlendMode.Additive:
                return (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
            case ParticleBlendMode.Multiply:
                return (float)UnityEngine.Rendering.BlendMode.DstColor;
            default:
                return (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
        }
    }

    private static float GetParticleDstBlend(ParticleBlendMode blendMode)
    {
        switch (blendMode)
        {
            case ParticleBlendMode.Additive:
                return (float)UnityEngine.Rendering.BlendMode.One;
            case ParticleBlendMode.Multiply:
                return (float)UnityEngine.Rendering.BlendMode.Zero;
            default:
                return (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
        }
    }

    private static float GetParticleSrcBlendAlpha(ParticleBlendMode blendMode)
    {
        return blendMode == ParticleBlendMode.Multiply
            ? (float)UnityEngine.Rendering.BlendMode.Zero
            : (float)UnityEngine.Rendering.BlendMode.One;
    }

    private static float GetParticleDstBlendAlpha(ParticleBlendMode blendMode)
    {
        switch (blendMode)
        {
            case ParticleBlendMode.Additive:
                return (float)UnityEngine.Rendering.BlendMode.One;
            case ParticleBlendMode.Multiply:
                return (float)UnityEngine.Rendering.BlendMode.One;
            default:
                return (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
        }
    }

    private static void ConfigureLitOrUnlitSurface(Material material, bool transparent, bool alphaClip, bool additive, bool premultiply)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", transparent ? 1f : 0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);

        if (material.HasProperty("_Blend"))
        {
            float blend = 0f;
            if (additive)
                blend = 2f;
            else if (premultiply)
                blend = 4f;
            else if (transparent)
                blend = 0f;

            material.SetFloat("_Blend", blend);
        }

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", additive || premultiply ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);

        if (transparent)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }
        else if (alphaClip)
        {
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }
    }

    private enum ParticleBlendMode
    {
        Alpha = 0,
        Premultiply = 1,
        Additive = 2,
        Multiply = 3
    }

    private static void EnableEmissionIfNeeded(Material material, MaterialSnapshot snapshot)
    {
        if (snapshot.EmissionMap != null || snapshot.EmissionColor.maxColorComponent > 0.001f)
            material.EnableKeyword("_EMISSION");
        else
            material.DisableKeyword("_EMISSION");
    }

    private static string GetShaderName(Material material)
    {
        return material != null && material.shader != null ? material.shader.name : "<missing>";
    }

    private static void SetTexture(Material material, string property, Texture texture, Vector2 scale, Vector2 offset)
    {
        if (!material.HasProperty(property) || texture == null)
            return;

        material.SetTexture(property, texture);
        material.SetTextureScale(property, scale);
        material.SetTextureOffset(property, offset);
    }

    private static void SetColor(Material material, string property, Color color)
    {
        if (material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private readonly struct MaterialSnapshot
    {
        public readonly string ShaderName;
        public readonly Texture MainTexture;
        public readonly Vector2 MainTextureScale;
        public readonly Vector2 MainTextureOffset;
        public readonly Texture BumpMap;
        public readonly Vector2 BumpScale;
        public readonly Vector2 BumpOffset;
        public readonly Texture EmissionMap;
        public readonly Vector2 EmissionScale;
        public readonly Vector2 EmissionOffset;
        public readonly Color BaseColor;
        public readonly Color EmissionColor;
        public readonly float Metallic;
        public readonly float Smoothness;
        public readonly bool HasNormalMap;
        public readonly bool IsTransparent;
        public readonly bool IsAlphaClip;
        public readonly bool IsAdditive;
        public readonly bool IsPremultiply;
        public readonly int CustomRenderQueue;

        private MaterialSnapshot(Material material)
        {
            ShaderName = GetShaderName(material);

            MainTexture = GetFirstTexture(material, out MainTextureScale, out MainTextureOffset, "_BaseMap", "_MainTex", "_TintTexture", "_MainTexture");
            BumpMap = GetFirstTexture(material, out BumpScale, out BumpOffset, "_BumpMap");
            EmissionMap = GetFirstTexture(material, out EmissionScale, out EmissionOffset, "_EmissionMap");

            BaseColor = GetFirstColor(material, Color.white, "_BaseColor", "_Color", "_TintColor");
            EmissionColor = GetFirstColor(material, Color.black, "_EmissionColor", "_EmisColor");
            Metallic = GetFirstFloat(material, 0f, "_Metallic");
            Smoothness = GetFirstFloat(material, GetFirstFloat(material, 0.5f, "_Glossiness"), "_Smoothness");
            HasNormalMap = BumpMap != null;

            bool hasAlphaBlendKeyword = material.IsKeywordEnabled("_ALPHABLEND_ON") || material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
            bool hasAlphaTestKeyword = material.IsKeywordEnabled("_ALPHATEST_ON");
            IsAdditive = ShaderName.Contains("Additive", StringComparison.OrdinalIgnoreCase) || GetFirstFloat(material, -1f, "_DstBlend") == (float)UnityEngine.Rendering.BlendMode.One;
            IsPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON") || ShaderName.Contains("Premultiply", StringComparison.OrdinalIgnoreCase);
            IsAlphaClip = hasAlphaTestKeyword;
            IsTransparent = IsAdditive || IsPremultiply || hasAlphaBlendKeyword || BaseColor.a < 0.999f || material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
            CustomRenderQueue = material.rawRenderQueue;
        }

        public static MaterialSnapshot Capture(Material material)
        {
            return new MaterialSnapshot(material);
        }
    }

    private static Texture GetFirstTexture(Material material, out Vector2 scale, out Vector2 offset, params string[] properties)
    {
        foreach (string property in properties)
        {
            if (!material.HasProperty(property))
                continue;

            Texture texture = material.GetTexture(property);
            if (texture == null)
                continue;

            scale = material.GetTextureScale(property);
            offset = material.GetTextureOffset(property);
            return texture;
        }

        scale = Vector2.one;
        offset = Vector2.zero;
        return null;
    }

    private static Color GetFirstColor(Material material, Color fallback, params string[] properties)
    {
        foreach (string property in properties)
        {
            if (material.HasProperty(property))
                return material.GetColor(property);
        }

        return fallback;
    }

    private static float GetFirstFloat(Material material, float fallback, params string[] properties)
    {
        foreach (string property in properties)
        {
            if (material.HasProperty(property))
                return material.GetFloat(property);
        }

        return fallback;
    }
}
