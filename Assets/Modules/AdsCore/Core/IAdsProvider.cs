using System;

namespace Modules.AdsCore
{
    public interface IAdsProvider
    {
        string ProviderId { get; }
        bool IsInitialized { get; }
        bool IsShowingAd { get; }

        void Initialize();
        bool TryShowInterstitial(AdsInterstitialPlacement placement, Action<AdsShowResult> callback);
        bool TryShowRewarded(AdsRewardType rewardType, Action<AdsShowResult> callback);
    }
}
