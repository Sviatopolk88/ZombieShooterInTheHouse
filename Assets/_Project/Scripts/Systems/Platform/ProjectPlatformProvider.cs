using UnityEngine;
using YG;

namespace _Project.Scripts.Systems.Platform
{
    /// <summary>
    /// Единый project-side источник определения класса устройства.
    /// Предпочитает данные PluginYG2 и использует Unity fallback только если окружение SDK ещё недоступно.
    /// </summary>
    public static class ProjectPlatformProvider
    {
#if UNITY_EDITOR
        private const string EditorOverridePrefsKey = "ZombieShooter.ProjectPlatformProvider.EditorOverride";
#endif

        public static bool IsMobile => GetPlatformKind() == ProjectPlatformKind.Mobile;

        public static bool IsDesktop => GetPlatformKind() == ProjectPlatformKind.Desktop;

#if UNITY_EDITOR
        public static ProjectPlatformEditorOverride EditorOverride
        {
            get
            {
                int value = PlayerPrefs.GetInt(EditorOverridePrefsKey, (int)ProjectPlatformEditorOverride.Auto);
                return System.Enum.IsDefined(typeof(ProjectPlatformEditorOverride), value)
                    ? (ProjectPlatformEditorOverride)value
                    : ProjectPlatformEditorOverride.Auto;
            }
        }

        public static void SetEditorOverride(ProjectPlatformEditorOverride editorOverride)
        {
            if (editorOverride == ProjectPlatformEditorOverride.Auto)
            {
                PlayerPrefs.DeleteKey(EditorOverridePrefsKey);
            }
            else
            {
                PlayerPrefs.SetInt(EditorOverridePrefsKey, (int)editorOverride);
            }

            PlayerPrefs.Save();
        }
#endif

        private static ProjectPlatformKind GetPlatformKind()
        {
#if UNITY_EDITOR
            if (TryGetEditorOverride(out ProjectPlatformKind editorPlatformKind))
            {
                return editorPlatformKind;
            }
#endif

            if (TryGetPlatformFromYg(out ProjectPlatformKind platformKind))
            {
                return platformKind;
            }

            return Application.isMobilePlatform
                ? ProjectPlatformKind.Mobile
                : ProjectPlatformKind.Desktop;
        }

        private static bool TryGetPlatformFromYg(out ProjectPlatformKind platformKind)
        {
            platformKind = ProjectPlatformKind.Desktop;

            if (YG2.envir == null)
            {
                return false;
            }

            string deviceType = YG2.envir.deviceType;
            if (string.IsNullOrWhiteSpace(deviceType))
            {
                return TryGetPlatformFromFlags(out platformKind);
            }

            switch (deviceType.Trim().ToLowerInvariant())
            {
                case "mobile":
                case "tablet":
                    platformKind = ProjectPlatformKind.Mobile;
                    return true;
                case "desktop":
                case "tv":
                    platformKind = ProjectPlatformKind.Desktop;
                    return true;
                default:
                    return TryGetPlatformFromFlags(out platformKind);
            }
        }

        private static bool TryGetPlatformFromFlags(out ProjectPlatformKind platformKind)
        {
            platformKind = ProjectPlatformKind.Desktop;

            if (YG2.envir == null)
            {
                return false;
            }

            if (YG2.envir.isMobile || YG2.envir.isTablet)
            {
                platformKind = ProjectPlatformKind.Mobile;
                return true;
            }

            if (YG2.envir.isDesktop || YG2.envir.isTV)
            {
                platformKind = ProjectPlatformKind.Desktop;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private static bool TryGetEditorOverride(out ProjectPlatformKind platformKind)
        {
            switch (EditorOverride)
            {
                case ProjectPlatformEditorOverride.PC:
                    platformKind = ProjectPlatformKind.Desktop;
                    return true;
                case ProjectPlatformEditorOverride.Mobile:
                    platformKind = ProjectPlatformKind.Mobile;
                    return true;
                default:
                    platformKind = ProjectPlatformKind.Desktop;
                    return false;
            }
        }
#endif

        private enum ProjectPlatformKind
        {
            Desktop,
            Mobile
        }

#if UNITY_EDITOR
        public enum ProjectPlatformEditorOverride
        {
            Auto = -1,
            PC = 0,
            Mobile = 1
        }
#endif
    }
}
