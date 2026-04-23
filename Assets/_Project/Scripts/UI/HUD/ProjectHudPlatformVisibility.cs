using UnityEngine;

namespace _Project.Scripts.UI.HUD
{
    public sealed class ProjectHudPlatformVisibility : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Корневой объект HUD-элементов, которые должны быть видны и на ПК, и на мобильных устройствах.")]
        private GameObject commonHudRoot;

        [SerializeField]
        [Tooltip("Корневой объект HUD-элементов, которые должны быть видны только в ПК-версии.")]
        private GameObject desktopHudRoot;

        [SerializeField]
        [Tooltip("Корневой объект HUD-элементов, которые должны быть видны только на мобильных устройствах.")]
        private GameObject mobileHudRoot;

        [SerializeField]
        [Tooltip("Считать устройство мобильным, если Unity сообщает о поддержке touch-ввода.")]
        private bool showMobileWhenTouchSupported = true;

        [SerializeField]
        [Tooltip("Показывать мобильную ветку HUD во время Play Mode в редакторе для ручной проверки layout.")]
        private bool showMobileInEditorPlayMode;

        private void Awake()
        {
            ApplyVisibility();
        }

        private void OnEnable()
        {
            ApplyVisibility();
        }

        private void Update()
        {
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            bool mobileHudActive = ShouldUseMobileHud();

            SetActiveIfNeeded(commonHudRoot, true);
            SetActiveIfNeeded(desktopHudRoot, !mobileHudActive);
            SetActiveIfNeeded(mobileHudRoot, mobileHudActive);
        }

        private bool ShouldUseMobileHud()
        {
#if UNITY_EDITOR
            if (showMobileInEditorPlayMode)
            {
                return true;
            }
#endif

            return Application.isMobilePlatform || (showMobileWhenTouchSupported && UnityEngine.Input.touchSupported);
        }

        private static void SetActiveIfNeeded(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
