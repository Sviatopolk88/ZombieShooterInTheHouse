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
        public static bool IsMobile => GetPlatformKind() == ProjectPlatformKind.Mobile;

        public static bool IsDesktop => GetPlatformKind() == ProjectPlatformKind.Desktop;

        private static ProjectPlatformKind GetPlatformKind()
        {
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

        private enum ProjectPlatformKind
        {
            Desktop,
            Mobile
        }
    }
}
