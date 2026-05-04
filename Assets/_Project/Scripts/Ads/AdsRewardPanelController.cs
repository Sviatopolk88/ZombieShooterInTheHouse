using _Project.Scripts.GameFlow;
using _Project.Scripts.Localization;
using Modules.AdsCore;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Ads
{
    [DisallowMultipleComponent]
    public sealed class AdsRewardPanelController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Корневой объект панели награды, размещённый на Canvas сцены _Main.")]
        private GameObject panelRoot;

        [SerializeField]
        [Tooltip("Текст с названием доступной награды.")]
        private Text titleLabel;

        [SerializeField]
        [Tooltip("Текст с пояснением, что игрок получит после просмотра рекламы.")]
        private Text messageLabel;

        [SerializeField]
        [Tooltip("Кнопка запуска rewarded-рекламы.")]
        private Button rewardButton;

        [SerializeField]
        [Tooltip("Текст внутри кнопки запуска rewarded-рекламы.")]
        private Text rewardButtonLabel;

        [SerializeField]
        [Tooltip("Кнопка закрытия панели без просмотра рекламы.")]
        private Button closeButton;

        [SerializeField]
        [Tooltip("Текст внутри кнопки закрытия панели.")]
        private Text closeButtonLabel;

        private AdsRewardZone activeZone;

        public static AdsRewardPanelController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            BindButtons();
            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            GameplayPauseService.Resume(this);
            UnbindButtons();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(AdsRewardZone zone)
        {
            if (zone == null)
            {
                Hide();
                return;
            }

            activeZone = zone;
            Refresh(zone);
            SetPanelVisible(true);
            GameplayPauseService.Pause(this);
            CursorStateService.Instance?.SetUiMode();
        }

        public void HideIfActive(AdsRewardZone zone)
        {
            if (activeZone == zone)
            {
                Hide();
            }
        }

        public void Refresh(AdsRewardZone zone)
        {
            if (activeZone != zone || zone == null)
            {
                return;
            }

            int rewardAmount = ProjectAdsRewardService.GetConfiguredRewardAmount(zone.RewardType);
            bool canRequest = zone.CanRequestReward(out string reason);

            if (titleLabel != null)
            {
                titleLabel.text = zone.GetRewardTitle();
            }

            if (messageLabel != null)
            {
                messageLabel.text = canRequest
                    ? ProjectLocalizationYG.FormatAdsRewardMessage(rewardAmount)
                    : ProjectLocalizationYG.FormatAdsRewardUnavailable(reason);
            }

            if (rewardButton != null)
            {
                rewardButton.interactable = canRequest && !zone.IsRequestPending;
            }

            if (rewardButtonLabel != null)
            {
                rewardButtonLabel.text = ProjectLocalizationYG.Get(
                    zone.IsRequestPending ? ProjectTextKey.AdsRewardLoading : ProjectTextKey.AdsRewardWatchAd);
            }

            if (closeButtonLabel != null)
            {
                closeButtonLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.AdsRewardClose);
            }
        }

        private void OnRewardButtonClicked()
        {
            if (activeZone == null)
            {
                Hide();
                return;
            }

            activeZone.RequestRewardFromUi();
            Refresh(activeZone);
        }

        private void Hide()
        {
            activeZone = null;
            SetPanelVisible(false);
            GameplayPauseService.Resume(this);
            CursorStateService.Instance?.SetGameplayMode();
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null && panelRoot.activeSelf != visible)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void BindButtons()
        {
            if (rewardButton != null)
            {
                rewardButton.onClick.RemoveListener(OnRewardButtonClicked);
                rewardButton.onClick.AddListener(OnRewardButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
        }

        private void UnbindButtons()
        {
            if (rewardButton != null)
            {
                rewardButton.onClick.RemoveListener(OnRewardButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }
    }
}
