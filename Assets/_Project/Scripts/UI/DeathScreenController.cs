using _Project.Scripts.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace _Project.Scripts.UI
{
    public sealed class DeathScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text restartLabel;

        private void Awake()
        {
            AutoAssignLabels();
            RefreshLocalizedText();

            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                Debug.LogWarning("DeathScreenController: root is not assigned.", this);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }
            else
            {
                Debug.LogWarning("DeathScreenController: restartButton is not assigned.", this);
            }
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
            RefreshLocalizedText();
        }

        private void OnDestroy()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }
        }

        public void Show()
        {
            if (root == null)
            {
                Debug.LogWarning("DeathScreenController: cannot show death screen because root is null.", this);
                return;
            }

            root.SetActive(true);
            RefreshLocalizedText();
        }

        public void Restart()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            if (GameFlowService.Instance == null)
            {
                Debug.LogWarning("DeathScreenController: GameFlowService instance not found.", this);
                return;
            }

            GameFlowService.Instance.RestartLevel();
        }

        private void AutoAssignLabels()
        {
            if (root != null && titleLabel == null)
            {
                TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    TMP_Text candidate = labels[i];
                    if (candidate != null && candidate.transform.parent == root.transform)
                    {
                        titleLabel = candidate;
                        break;
                    }
                }
            }

            if (restartButton != null && restartLabel == null)
            {
                restartLabel = restartButton.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void RefreshLocalizedText()
        {
            if (titleLabel != null)
            {
                titleLabel.text = _Project.Scripts.Localization.ProjectLocalizationYG.Get(
                    _Project.Scripts.Localization.ProjectTextKey.DeathScreenTitle);
            }

            if (restartLabel != null)
            {
                restartLabel.text = _Project.Scripts.Localization.ProjectLocalizationYG.Get(
                    _Project.Scripts.Localization.ProjectTextKey.DeathScreenRestart);
            }
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshLocalizedText();
        }
    }
}

namespace _Project.Scripts.Localization
{
    public enum ProjectTextKey
    {
        RescueHudFormat = 0,
        DeathScreenTitle = 1,
        DeathScreenRestart = 2,
        ExitConfirmTitle = 3,
        ExitConfirmLeave = 4,
        ExitConfirmStay = 5,
        VictoryTitle = 6,
        VictorySubtitle = 7,
        VictoryRescuedFormat = 8,
        VictoryFailedFormat = 9,
        VictoryRemainingFormat = 10,
        VictorySummaryPerfect = 11,
        VictorySummaryIncomplete = 12,
        VictoryNext = 13,
        BootstrapLoading = 14,
        AdsRewardTitleHeal = 15,
        AdsRewardTitleAmmo9mm = 16,
        AdsRewardTitleDefault = 17,
        AdsRewardStationUsed = 18,
        AdsRewardMessageFormat = 19,
        AdsRewardUnavailableFormat = 20,
        AdsRewardLoading = 21,
        AdsRewardWatchAd = 22,
        AdsRewardClose = 23,
        PurchaseRequest = 24,
        PurchaseBuyFormat = 25,
        PurchaseBuyWithPriceFormat = 26,
        PurchaseUnavailableFormat = 27,
        PurchaseFallbackTitle = 28
    }

    public static class ProjectLocalizationYG
    {
        public static string CurrentLanguage
        {
            get
            {
                string language = NormalizeLanguage(YG2.lang);
                if (!string.IsNullOrEmpty(language))
                {
                    return language;
                }

                return NormalizeLanguage(YG2.envir.language);
            }
        }

        public static bool IsRussianLanguage(string language = null)
        {
            return NormalizeLanguage(language ?? CurrentLanguage) == "ru";
        }

        public static string Get(ProjectTextKey key, string language = null)
        {
            bool isRussian = IsRussianLanguage(language);

            switch (key)
            {
                case ProjectTextKey.RescueHudFormat:
                    return isRussian ? "Спасено: {0} / {1}" : "Rescued: {0} / {1}";

                case ProjectTextKey.DeathScreenTitle:
                    return isRussian ? "ВЫ ПОГИБЛИ" : "YOU DIED";

                case ProjectTextKey.DeathScreenRestart:
                    return isRussian ? "ПОВТОРИТЬ" : "RETRY";

                case ProjectTextKey.ExitConfirmTitle:
                    return isRussian ? "НЕ ВСЕ ЖИТЕЛИ СПАСЕНЫ" : "CIVILIANS REMAIN";

                case ProjectTextKey.ExitConfirmLeave:
                    return isRussian ? "УЙТИ" : "LEAVE";

                case ProjectTextKey.ExitConfirmStay:
                    return isRussian ? "ВЕРНУТЬСЯ" : "STAY";

                case ProjectTextKey.VictoryTitle:
                    return isRussian ? "УРОВЕНЬ ПРОЙДЕН" : "LEVEL COMPLETE";

                case ProjectTextKey.VictorySubtitle:
                    return isRussian ? "Босс уничтожен" : "Boss eliminated";

                case ProjectTextKey.VictoryRescuedFormat:
                    return isRussian ? "Спасено: {0} / {1}" : "Rescued: {0} / {1}";

                case ProjectTextKey.VictoryFailedFormat:
                    return isRussian ? "Погибло: {0}" : "Lost: {0}";

                case ProjectTextKey.VictoryRemainingFormat:
                    return isRussian ? "Осталось: {0}" : "Remaining: {0}";

                case ProjectTextKey.VictorySummaryPerfect:
                    return isRussian ? "Все выжившие спасены" : "All survivors were rescued";

                case ProjectTextKey.VictorySummaryIncomplete:
                    return isRussian ? "Не все жители были спасены" : "Not all civilians were rescued";

                case ProjectTextKey.VictoryNext:
                    return isRussian ? "ДАЛЕЕ" : "NEXT";

                case ProjectTextKey.BootstrapLoading:
                    return isRussian ? "Загрузка..." : "Loading...";

                case ProjectTextKey.AdsRewardTitleHeal:
                    return isRussian ? "Лечение" : "Healing";

                case ProjectTextKey.AdsRewardTitleAmmo9mm:
                    return isRussian ? "Патроны 9mm" : "9mm ammo";

                case ProjectTextKey.AdsRewardTitleDefault:
                    return isRussian ? "Награда" : "Reward";

                case ProjectTextKey.AdsRewardStationUsed:
                    return isRussian ? "Станция уже использована." : "Station already used.";

                case ProjectTextKey.AdsRewardMessageFormat:
                    return isRussian ? "Получить +{0} за просмотр рекламы." : "Watch an ad to get +{0}.";

                case ProjectTextKey.AdsRewardUnavailableFormat:
                    return isRussian ? "Награда недоступна: {0}" : "Reward unavailable: {0}";

                case ProjectTextKey.AdsRewardLoading:
                    return isRussian ? "Загрузка..." : "Loading...";

                case ProjectTextKey.AdsRewardWatchAd:
                    return isRussian ? "Смотреть рекламу" : "Watch ad";

                case ProjectTextKey.AdsRewardClose:
                    return isRussian ? "Закрыть" : "Close";

                case ProjectTextKey.PurchaseRequest:
                    return isRussian ? "Запрос покупки..." : "Purchase request...";

                case ProjectTextKey.PurchaseBuyFormat:
                    return isRussian ? "[{0}] Купить: {1}" : "[{0}] Buy: {1}";

                case ProjectTextKey.PurchaseBuyWithPriceFormat:
                    return isRussian ? "[{0}] Купить: {1} | {2}" : "[{0}] Buy: {1} | {2}";

                case ProjectTextKey.PurchaseUnavailableFormat:
                    return isRussian ? "{0} недоступно: {1}" : "{0} unavailable: {1}";

                case ProjectTextKey.PurchaseFallbackTitle:
                    return isRussian ? "Покупка" : "Purchase";

                default:
                    return string.Empty;
            }
        }

        public static string FormatRescueProgress(int rescuedCount, int totalCount, string language = null)
        {
            return string.Format(Get(ProjectTextKey.RescueHudFormat, language), rescuedCount, totalCount);
        }

        public static string FormatExitConfirmationMessage(int remainingCount, string language = null)
        {
            if (IsRussianLanguage(language))
            {
                return $"На уровне остались не спасённые жители: {remainingCount}. Действительно покинуть уровень?";
            }

            return $"There are still unrescued civilians on the level: {remainingCount}. Leave the level anyway?";
        }

        public static string FormatVictoryRescued(int rescuedCount, int totalCount, string language = null)
        {
            return string.Format(Get(ProjectTextKey.VictoryRescuedFormat, language), rescuedCount, totalCount);
        }

        public static string FormatVictoryFailed(int failedCount, string language = null)
        {
            return string.Format(Get(ProjectTextKey.VictoryFailedFormat, language), failedCount);
        }

        public static string FormatVictoryRemaining(int remainingCount, string language = null)
        {
            return string.Format(Get(ProjectTextKey.VictoryRemainingFormat, language), remainingCount);
        }

        public static string FormatAdsRewardMessage(int rewardAmount, string language = null)
        {
            return string.Format(Get(ProjectTextKey.AdsRewardMessageFormat, language), rewardAmount);
        }

        public static string FormatAdsRewardUnavailable(string reason, string language = null)
        {
            return string.Format(Get(ProjectTextKey.AdsRewardUnavailableFormat, language), reason);
        }

        public static string FormatPurchaseBuy(KeyCode interactionKey, string productTitle, string language = null)
        {
            return string.Format(Get(ProjectTextKey.PurchaseBuyFormat, language), interactionKey, productTitle);
        }

        public static string FormatPurchaseBuyWithPrice(KeyCode interactionKey, string productTitle, string productPrice, string language = null)
        {
            return string.Format(Get(ProjectTextKey.PurchaseBuyWithPriceFormat, language), interactionKey, productTitle, productPrice);
        }

        public static string FormatPurchaseUnavailable(string productTitle, string reason, string language = null)
        {
            return string.Format(Get(ProjectTextKey.PurchaseUnavailableFormat, language), productTitle, reason);
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return "en";
            }

            language = language.Trim().ToLowerInvariant();

            if (language == "us" || language == "as" || language == "ai")
            {
                return "en";
            }

            return language;
        }
    }
}
