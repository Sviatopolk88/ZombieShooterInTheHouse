using System;
using UnityEngine;

namespace Modules.AdsCore
{
    [Serializable]
    public sealed class AdsProjectSettings
    {
        [SerializeField] private AdsProviderType providerType = AdsProviderType.YandexGames;
        [SerializeField] private int healRewardAmount = 35;
        [SerializeField] private int ammo9mmRewardAmount = 30;

        public AdsProviderType ProviderType => providerType;
        public int HealRewardAmount => Mathf.Max(1, healRewardAmount);
        public int Ammo9mmRewardAmount => Mathf.Max(1, ammo9mmRewardAmount);
    }
}
