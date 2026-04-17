using System;
using Modules.AdsCore;
using UnityEngine;

namespace _Project.Scripts.Ads
{
    /// <summary>
    /// Project-side bridge между reusable ads-core и gameplay-наградами.
    /// </summary>
    public static class ProjectAdsRewardService
    {
        public static bool CanRequestReward(AdsRewardType rewardType, out string reason)
        {
            if (!AdsService.Instance.CanRequestReward(rewardType, out reason))
            {
                return false;
            }

            return AdsRewardApplier.CanApplyReward(rewardType, out reason);
        }

        public static bool TryShowRewarded(AdsRewardType rewardType)
        {
            return TryShowRewarded(rewardType, null);
        }

        public static bool TryShowRewarded(AdsRewardType rewardType, Action<AdsShowResult> callback)
        {
            if (!CanRequestReward(rewardType, out string reason))
            {
                Debug.Log($"ProjectAdsRewardService: rewarded '{rewardType}' отклонён. Причина: {reason}");
                callback?.Invoke(AdsShowResult.Rejected(reason));
                return false;
            }

            return AdsService.Instance.TryShowRewarded(
                rewardType,
                result => OnRewardedCompleted(rewardType, result, callback));
        }

        public static int GetConfiguredRewardAmount(AdsRewardType rewardType)
        {
            return AdsService.Instance.GetConfiguredRewardAmount(rewardType);
        }

        private static void OnRewardedCompleted(AdsRewardType rewardType, AdsShowResult result, Action<AdsShowResult> callback)
        {
            if (!result.IsSuccess)
            {
                callback?.Invoke(result);
                return;
            }

            int rewardAmount = GetConfiguredRewardAmount(rewardType);
            if (!AdsRewardApplier.TryApplyReward(rewardType, rewardAmount, out string rewardMessage))
            {
                Debug.LogWarning($"ProjectAdsRewardService: rewarded '{rewardType}' подтверждён, но награда не выдана. {rewardMessage}");
                callback?.Invoke(AdsShowResult.Failed(rewardMessage));
                return;
            }

            Debug.Log($"ProjectAdsRewardService: rewarded '{rewardType}' выдан успешно. {rewardMessage}");
            callback?.Invoke(AdsShowResult.Completed(rewardMessage));
        }
    }
}
