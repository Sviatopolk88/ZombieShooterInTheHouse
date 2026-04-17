using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ZombieAnimationClipExporter
{
    private const string ClipsFolder = "Assets/_Project/Animations/Zombie/Clips";
    private const string ControllerPath = "Assets/_Project/Animations/Zombie/Controller/ZombieAnimator.controller";

    [MenuItem("Tools/Zombie/Export Local Animation Clips")]
    public static void Export()
    {
        ExportInternal();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Локальные клипы зомби экспортированы в Assets/_Project/Animations/Zombie/Clips.");
    }

    private static void ExportInternal()
    {
        Directory.CreateDirectory(ClipsFolder);

        AnimationClip idleClip = DuplicateClip(
            "Assets/Zombie_Animations/Animations/Zombie_Idle_01.FBX",
            "Zombie_Idle_01",
            Path.Combine(ClipsFolder, "Zombie_Idle.anim").Replace("\\", "/"));

        AnimationClip walkClip = DuplicateClip(
            "Assets/Zombie_Animations/Animations/Zombie_Walk_01_Forward_InPlace.fbx",
            "Zombie_Walk_01_Forward_InPlace",
            Path.Combine(ClipsFolder, "Zombie_Walk.anim").Replace("\\", "/"));

        AnimationClip attackClip = DuplicateClip(
            "Assets/Zombie_Animations/Animations/Zombie_Attack01.FBX",
            "Zombie_Attack01",
            Path.Combine(ClipsFolder, "Zombie_Attack.anim").Replace("\\", "/"));

        AnimationClip hitClip = DuplicateClip(
            "Assets/Zombie_Animations/Animations/Zombie_HitReact_Head.fbx",
            "Zombie_HitReact_Head",
            Path.Combine(ClipsFolder, "Zombie_Hit.anim").Replace("\\", "/"));

        AnimationClip deathClip = DuplicateClip(
            "Assets/Zombie_Animations/Animations/Zombie_Idle_Death.fbx",
            "Zombie_Idle_Death",
            Path.Combine(ClipsFolder, "Zombie_Death.anim").Replace("\\", "/"));

        UpdateController(idleClip, walkClip, attackClip, hitClip, deathClip);
    }

    private static AnimationClip DuplicateClip(string sourcePath, string clipName, string outputPath)
    {
        AnimationClip sourceClip = LoadClip(sourcePath, clipName);

        if (sourceClip == null)
        {
            throw new InvalidOperationException($"Не найден клип '{clipName}' по пути '{sourcePath}'.");
        }

        AnimationClip duplicate = new AnimationClip
        {
            name = Path.GetFileNameWithoutExtension(outputPath)
        };

        EditorUtility.CopySerialized(sourceClip, duplicate);
        duplicate.name = Path.GetFileNameWithoutExtension(outputPath);

        if (File.Exists(outputPath))
        {
            AssetDatabase.DeleteAsset(outputPath);
        }

        AssetDatabase.CreateAsset(duplicate, outputPath);

        AnimationClip importedDuplicate = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
        CopyClipSettings(sourceClip, importedDuplicate);
        EditorUtility.SetDirty(importedDuplicate);
        return importedDuplicate;
    }

    private static AnimationClip LoadClip(string assetPath, string clipName)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == clipName);
    }

    private static void CopyClipSettings(AnimationClip sourceClip, AnimationClip targetClip)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        AnimationUtility.SetAnimationClipSettings(targetClip, settings);
    }

    private static void UpdateController(
        AnimationClip idleClip,
        AnimationClip walkClip,
        AnimationClip attackClip,
        AnimationClip hitClip,
        AnimationClip deathClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null || controller.layers.Length == 0)
        {
            throw new InvalidOperationException($"Не найден Animator Controller по пути '{ControllerPath}'.");
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            switch (childState.state.name)
            {
                case "Idle":
                    childState.state.motion = idleClip;
                    break;
                case "Walk":
                    childState.state.motion = walkClip;
                    break;
                case "Attack":
                    childState.state.motion = attackClip;
                    break;
                case "Hit":
                    childState.state.motion = hitClip;
                    break;
                case "Death":
                    childState.state.motion = deathClip;
                    break;
            }
        }

        EditorUtility.SetDirty(controller);
    }
}
