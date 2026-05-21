using _Project.Scripts.Systems.Platform;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Development
{
    public static class DevelopmentPlatformToolsMenu
    {
        private const string AutoMenuPath = "Tools/Development/Platform/Auto";
        private const string PcMenuPath = "Tools/Development/Platform/PC";
        private const string MobileMenuPath = "Tools/Development/Platform/Mobile";

        [MenuItem(AutoMenuPath)]
        private static void SetAuto()
        {
            SetOverride(ProjectPlatformProvider.ProjectPlatformEditorOverride.Auto);
        }

        [MenuItem(PcMenuPath)]
        private static void SetPc()
        {
            SetOverride(ProjectPlatformProvider.ProjectPlatformEditorOverride.PC);
        }

        [MenuItem(MobileMenuPath)]
        private static void SetMobile()
        {
            SetOverride(ProjectPlatformProvider.ProjectPlatformEditorOverride.Mobile);
        }

        [MenuItem(AutoMenuPath, true)]
        private static bool ValidateAuto()
        {
            SetMenuChecks();
            return true;
        }

        [MenuItem(PcMenuPath, true)]
        private static bool ValidatePc()
        {
            SetMenuChecks();
            return true;
        }

        [MenuItem(MobileMenuPath, true)]
        private static bool ValidateMobile()
        {
            SetMenuChecks();
            return true;
        }

        private static void SetOverride(ProjectPlatformProvider.ProjectPlatformEditorOverride editorOverride)
        {
            ProjectPlatformProvider.SetEditorOverride(editorOverride);
            SetMenuChecks();

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();

            Debug.Log($"Development platform override: {editorOverride}");
        }

        private static void SetMenuChecks()
        {
            ProjectPlatformProvider.ProjectPlatformEditorOverride current = ProjectPlatformProvider.EditorOverride;

            Menu.SetChecked(AutoMenuPath, current == ProjectPlatformProvider.ProjectPlatformEditorOverride.Auto);
            Menu.SetChecked(PcMenuPath, current == ProjectPlatformProvider.ProjectPlatformEditorOverride.PC);
            Menu.SetChecked(MobileMenuPath, current == ProjectPlatformProvider.ProjectPlatformEditorOverride.Mobile);
        }
    }
}
