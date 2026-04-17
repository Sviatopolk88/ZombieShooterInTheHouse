using System;
using System.Collections;
using UnityEngine;
using YG;

namespace Modules.AdsCore
{
    /// <summary>
    /// Reusable provider поверх PluginYG2 / YG2.
    /// </summary>
    public sealed class YandexAdsProvider : IAdsProvider
    {
        private const float InterstitialStartGuardSeconds = 0.75f;

        private Action<AdsShowResult> pendingInterstitialCallback;
        private Action<AdsShowResult> pendingRewardedCallback;
        private AdsRewardType pendingRewardType;
        private bool pendingRewardConfirmed;
        private bool initialized;
        private uint interstitialRequestVersion;

        public string ProviderId => "yandex-games";
        public bool IsInitialized => YG2.isSDKEnabled;
        public bool IsShowingAd => YG2.nowAdsShow;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

#if InterstitialAdv_yg
            YG2.onCloseInterAdvWasShow += OnInterstitialClosed;
            YG2.onErrorInterAdv += OnInterstitialError;
#endif

#if RewardedAdv_yg
            YG2.onRewardAdv += OnRewarded;
            YG2.onCloseRewardedAdv += OnRewardedClosed;
            YG2.onErrorRewardedAdv += OnRewardedError;
#endif

            initialized = true;
        }

        public bool TryShowInterstitial(AdsInterstitialPlacement placement, Action<AdsShowResult> callback)
        {
#if !InterstitialAdv_yg
            callback?.Invoke(AdsShowResult.Failed("Модуль interstitial рекламы не включён в PluginYG2."));
            return false;
#else
            if (!IsInitialized)
            {
                callback?.Invoke(AdsShowResult.Rejected("Yandex SDK ещё не инициализирован."));
                return false;
            }

            if (IsShowingAd || pendingInterstitialCallback != null)
            {
                callback?.Invoke(AdsShowResult.Rejected("Реклама уже показывается."));
                return false;
            }

            if (!YG2.isTimerAdvCompleted)
            {
                callback?.Invoke(AdsShowResult.Rejected("Interstitial ещё не доступен по встроенному таймеру PluginYG2."));
                return false;
            }

            bool wasShowingAdBeforeRequest = YG2.nowAdsShow;
            pendingInterstitialCallback = callback;
            uint requestVersion = ++interstitialRequestVersion;
            YG2.InterstitialAdvShow();

            if (!wasShowingAdBeforeRequest && YG2.nowAdsShow)
            {
                return true;
            }

            StartInterstitialStartGuard(requestVersion);
            return true;
#endif
        }

        public bool TryShowRewarded(AdsRewardType rewardType, Action<AdsShowResult> callback)
        {
#if !RewardedAdv_yg
            callback?.Invoke(AdsShowResult.Failed("Модуль rewarded рекламы не включён в PluginYG2."));
            return false;
#else
            if (!IsInitialized)
            {
                callback?.Invoke(AdsShowResult.Rejected("Yandex SDK ещё не инициализирован."));
                return false;
            }

            if (IsShowingAd || pendingRewardedCallback != null)
            {
                callback?.Invoke(AdsShowResult.Rejected("Реклама уже показывается."));
                return false;
            }

            pendingRewardType = rewardType;
            pendingRewardConfirmed = false;
            pendingRewardedCallback = callback;
            YG2.RewardedAdvShow(GetRewardId(rewardType));
            return true;
#endif
        }

#if InterstitialAdv_yg
        private void OnInterstitialClosed(bool wasShown)
        {
            CompleteInterstitial(wasShown
                ? AdsShowResult.Completed("Interstitial завершён.")
                : AdsShowResult.Cancelled("Interstitial был закрыт без показа."));
        }

        private void OnInterstitialError()
        {
            CompleteInterstitial(AdsShowResult.Failed("Yandex interstitial вернул ошибку."));
        }

        private void CompleteInterstitial(AdsShowResult result)
        {
            Action<AdsShowResult> callback = pendingInterstitialCallback;
            pendingInterstitialCallback = null;
            callback?.Invoke(result);
        }

        private void StartInterstitialStartGuard(uint requestVersion)
        {
            if (YG2.sendMessage == null)
            {
                CompleteInterstitial(AdsShowResult.Failed("PluginYG2 не подготовил YGSendMessage для отслеживания interstitial."));
                return;
            }

            YG2.sendMessage.StartCoroutine(WaitForInterstitialStart(requestVersion));
        }

        private IEnumerator WaitForInterstitialStart(uint requestVersion)
        {
            yield return null;

            if (!IsPendingInterstitialRequest(requestVersion) || YG2.nowAdsShow)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(InterstitialStartGuardSeconds);

            if (!IsPendingInterstitialRequest(requestVersion) || YG2.nowAdsShow)
            {
                yield break;
            }

            CompleteInterstitial(AdsShowResult.Rejected("PluginYG2 не запустил interstitial. Запрос сброшен без зависания AdsService."));
        }

        private bool IsPendingInterstitialRequest(uint requestVersion)
        {
            return pendingInterstitialCallback != null && requestVersion == interstitialRequestVersion;
        }
#endif

#if RewardedAdv_yg
        private void OnRewarded(string rewardId)
        {
            if (pendingRewardedCallback == null)
            {
                return;
            }

            if (!string.Equals(rewardId, GetRewardId(pendingRewardType), StringComparison.Ordinal))
            {
                return;
            }

            pendingRewardConfirmed = true;
        }

        private void OnRewardedClosed()
        {
            CompleteRewarded(
                pendingRewardConfirmed
                    ? AdsShowResult.Completed("Rewarded просмотрен до конца.")
                    : AdsShowResult.Cancelled("Rewarded закрыт без подтверждённой награды."));
        }

        private void OnRewardedError()
        {
            CompleteRewarded(AdsShowResult.Failed("Yandex rewarded вернул ошибку."));
        }

        private void CompleteRewarded(AdsShowResult result)
        {
            Action<AdsShowResult> callback = pendingRewardedCallback;
            pendingRewardedCallback = null;
            pendingRewardConfirmed = false;
            callback?.Invoke(result);
        }
#endif

        private static string GetRewardId(AdsRewardType rewardType)
        {
            return rewardType switch
            {
                AdsRewardType.Heal => "reward_heal",
                AdsRewardType.Ammo9mm => "reward_ammo_9mm",
                _ => "reward_unknown"
            };
        }
    }
}
