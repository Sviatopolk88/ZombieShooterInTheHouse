using System;
using UnityEngine;

namespace Modules.AdsCore
{
    /// <summary>
    /// Reusable фасад рекламы.
    /// Отвечает только за показ рекламы и возврат результата показа.
    /// </summary>
    public sealed class AdsService
    {
        private static AdsService instance;

        private readonly AdsProjectSettings settings = new();
        private readonly IAdsProvider provider;
        private bool adRequestInProgress;

        private AdsService()
        {
            provider = CreateProvider(settings.ProviderType);
            provider.Initialize();
        }

        public static AdsService Instance => instance ??= new AdsService();

        public string ActiveProviderId => provider.ProviderId;
        public bool IsInitialized => provider.IsInitialized;
        public bool IsAdShowing => provider.IsShowingAd || adRequestInProgress;

        public void Warmup()
        {
            // Явная точка ранней инициализации из bootstrap.
        }

        public bool TryShowInterstitial(AdsInterstitialPlacement placement)
        {
            if (adRequestInProgress)
            {
                return false;
            }

            if (!provider.TryShowInterstitial(placement, OnInterstitialCompleted))
            {
                return false;
            }

            adRequestInProgress = true;
            return true;
        }

        public bool CanRequestReward(AdsRewardType rewardType, out string reason)
        {
            if (adRequestInProgress || provider.IsShowingAd)
            {
                reason = "Реклама уже показывается.";
                return false;
            }

            if (!provider.IsInitialized)
            {
                reason = "SDK рекламы ещё не готов.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryShowRewarded(AdsRewardType rewardType)
        {
            return TryShowRewarded(rewardType, null);
        }

        public bool TryShowRewarded(AdsRewardType rewardType, Action<AdsShowResult> callback)
        {
            if (!CanRequestReward(rewardType, out string reason))
            {
                Debug.Log($"AdsService: rewarded '{rewardType}' отклонён. Причина: {reason}");
                callback?.Invoke(AdsShowResult.Rejected(reason));
                return false;
            }

            if (!provider.TryShowRewarded(rewardType, result => OnRewardedCompleted(rewardType, result, callback)))
            {
                return false;
            }

            adRequestInProgress = true;
            return true;
        }

        public int GetConfiguredRewardAmount(AdsRewardType rewardType)
        {
            return rewardType switch
            {
                AdsRewardType.Heal => settings.HealRewardAmount,
                AdsRewardType.Ammo9mm => settings.Ammo9mmRewardAmount,
                _ => 0
            };
        }

        private void OnInterstitialCompleted(AdsShowResult result)
        {
            adRequestInProgress = false;

            if (!result.IsSuccess && !string.IsNullOrEmpty(result.Message))
            {
                Debug.Log($"AdsService: interstitial завершён со статусом {result.Status}. {result.Message}");
            }
        }

        private void OnRewardedCompleted(AdsRewardType rewardType, AdsShowResult result, Action<AdsShowResult> callback)
        {
            adRequestInProgress = false;

            if (!result.IsSuccess)
            {
                Debug.Log($"AdsService: rewarded '{rewardType}' завершён со статусом {result.Status}. {result.Message}");
            }

            callback?.Invoke(result);
        }

        private static IAdsProvider CreateProvider(AdsProviderType providerType)
        {
            return providerType switch
            {
                AdsProviderType.YandexGames => new YandexAdsProvider(),
                _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Неизвестный ads provider.")
            };
        }
    }
}
