using _Project.Scripts.Gameplay.Rescue;
using _Project.Scripts.Localization;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace _Project.Scripts.UI.HUD
{
    /// <summary>
    /// Минимальный HUD-представитель прогресса спасения в Main-сцене.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RescueHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text label;

        private LevelRescueController controller;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponent<Text>();
            }
        }

        private void OnEnable()
        {
            LevelRescueController.ActiveControllerChanged += OnActiveControllerChanged;
            YG2.onSwitchLang += OnSwitchLanguage;
            AttachToController(LevelRescueController.Active);
            RefreshLabel();
        }

        private void OnDisable()
        {
            LevelRescueController.ActiveControllerChanged -= OnActiveControllerChanged;
            YG2.onSwitchLang -= OnSwitchLanguage;
            AttachToController(null);
        }

        private void OnActiveControllerChanged(LevelRescueController newController)
        {
            AttachToController(newController);
            RefreshLabel();
        }

        private void AttachToController(LevelRescueController newController)
        {
            if (controller == newController)
            {
                return;
            }

            if (controller != null)
            {
                controller.CountsChanged -= RefreshLabel;
            }

            controller = newController;

            if (controller != null)
            {
                controller.CountsChanged += RefreshLabel;
            }
        }

        private void RefreshLabel()
        {
            if (label == null)
            {
                return;
            }

            if (controller == null)
            {
                label.text = ProjectLocalizationYG.FormatRescueProgress(0, 0);
                return;
            }

            label.text = ProjectLocalizationYG.FormatRescueProgress(controller.RescuedCount, controller.TotalCount);
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshLabel();
        }
    }
}
