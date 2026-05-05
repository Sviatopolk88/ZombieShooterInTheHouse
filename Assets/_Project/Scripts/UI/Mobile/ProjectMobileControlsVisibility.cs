using _Project.Scripts.GameFlow;
using _Project.Scripts.Systems.Platform;
using UnityEngine;

namespace _Project.Scripts.UI.Mobile
{
    public sealed class ProjectMobileControlsVisibility : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Корневой объект экранных элементов мобильного управления, который можно редактировать прямо в сцене.")]
        private GameObject mobileControlsRoot;

        [SerializeField]
        [Tooltip("Показывать мобильное управление во время Play Mode в редакторе для ручной проверки layout.")]
        private bool showInEditorPlayMode;

        [SerializeField]
        [Tooltip("Скрывать мобильное управление, когда проект перевёл ввод игрока в UI-режим.")]
        private bool hideInUiMode = true;

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
            if (mobileControlsRoot == null)
            {
                return;
            }

            bool shouldShow = ShouldShowControls();
            if (mobileControlsRoot.activeSelf != shouldShow)
            {
                mobileControlsRoot.SetActive(shouldShow);
            }
        }

        private bool ShouldShowControls()
        {
#if UNITY_EDITOR
            if (showInEditorPlayMode)
            {
                return IsGameplayInputAllowed();
            }
#endif

            return ProjectPlatformProvider.IsMobile && IsGameplayInputAllowed();
        }

        private bool IsGameplayInputAllowed()
        {
            if (!hideInUiMode)
            {
                return true;
            }

            return CursorStateService.Instance == null || CursorStateService.Instance.IsGameplayMode;
        }
    }
}
