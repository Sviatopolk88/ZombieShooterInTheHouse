using System.Collections.Generic;
using _Project.Scripts.Localization;
using Modules.AdsCore;
using UnityEngine;
using YG;

namespace _Project.Scripts.Ads
{
    public enum AdsRewardZoneUsageMode
    {
        ByRewardType = 0,
        LimitedUses = 1,
        ResourceBased = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AdsRewardZone : MonoBehaviour
    {
        [Header("Reward")]
        [SerializeField]
        [Tooltip("Тип rewarded-награды, которую запрашивает эта world-зона.")]
        private AdsRewardType rewardType = AdsRewardType.Heal;

        [SerializeField]
        [Tooltip("Правило расходования зоны после успешной выдачи награды.")]
        private AdsRewardZoneUsageMode usageMode = AdsRewardZoneUsageMode.ByRewardType;

        [SerializeField]
        [Tooltip("Количество успешных использований для режима LimitedUses.")]
        private int maxUses = 1;

        [Header("Activation")]

        [SerializeField]
        [Tooltip("Слои объектов, которым разрешено активировать эту зону.")]
        private LayerMask activatorLayers = ~0;

        [SerializeField]
        [Tooltip("Тег объекта игрока, который может активировать эту зону.")]
        private string requiredTag = "Player";

        [Header("Behaviour")]
        [SerializeField]
        [Tooltip("Выключать триггер после исчерпания лимита использований.")]
        private bool disableColliderWhenExhausted = true;

        private readonly HashSet<Collider> occupants = new();
        private bool requestPending;
        private int remainingUses;

        public AdsRewardType RewardType => rewardType;
        public int RemainingUses => remainingUses;
        public bool IsRequestPending => requestPending;

        private void Reset()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            maxUses = Mathf.Max(1, maxUses);

            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            remainingUses = Mathf.Max(1, maxUses);
            ApplyAvailabilityState();
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
        }

        private void OnDisable()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;
            occupants.Clear();
            requestPending = false;
            AdsRewardPanelController.Instance?.HideIfActive(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!MatchesActivator(other))
            {
                return;
            }

            occupants.Add(other);
            AdsRewardPanelController.Instance?.Show(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null)
            {
                return;
            }

            occupants.Remove(other);
            if (occupants.Count == 0)
            {
                AdsRewardPanelController.Instance?.HideIfActive(this);
            }
        }

        public bool CanRequestReward(out string reason)
        {
            if (IsLimitedZoneExhausted())
            {
                reason = ProjectLocalizationYG.Get(ProjectTextKey.AdsRewardStationUsed);
                return false;
            }

            return ProjectAdsRewardService.CanRequestReward(rewardType, out reason);
        }

        public void RequestRewardFromUi()
        {
            if (IsLimitedZoneExhausted())
            {
                AdsRewardPanelController.Instance?.Refresh(this);
                return;
            }

            if (!ProjectAdsRewardService.TryShowRewarded(rewardType, OnRewardRequestCompleted))
            {
                AdsRewardPanelController.Instance?.Refresh(this);
                return;
            }

            requestPending = true;
            AdsRewardPanelController.Instance?.Refresh(this);
        }

        public string GetRewardTitle()
        {
            return rewardType switch
            {
                AdsRewardType.Heal => ProjectLocalizationYG.Get(ProjectTextKey.AdsRewardTitleHeal),
                AdsRewardType.Ammo9mm => ProjectLocalizationYG.Get(ProjectTextKey.AdsRewardTitleAmmo9mm),
                _ => ProjectLocalizationYG.Get(ProjectTextKey.AdsRewardTitleDefault)
            };
        }

        private void OnRewardRequestCompleted(AdsShowResult result)
        {
            requestPending = false;

            if (result.IsSuccess && UsesLimitedCounter())
            {
                remainingUses = Mathf.Max(0, remainingUses - 1);
            }

            ApplyAvailabilityState();

            if (IsLimitedZoneExhausted())
            {
                AdsRewardPanelController.Instance?.HideIfActive(this);
                return;
            }

            AdsRewardPanelController.Instance?.Refresh(this);
        }

        private void ApplyAvailabilityState()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.enabled = !disableColliderWhenExhausted || !IsLimitedZoneExhausted();
            }
        }

        private bool MatchesActivator(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            GameObject candidate = other.gameObject;
            if (((1 << candidate.layer) & activatorLayers.value) == 0)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(requiredTag) || candidate.CompareTag(requiredTag);
        }

        private void OnSwitchLanguage(string language)
        {
            AdsRewardPanelController.Instance?.Refresh(this);
        }

        private bool UsesLimitedCounter()
        {
            return GetEffectiveUsageMode() == AdsRewardZoneUsageMode.LimitedUses;
        }

        private bool IsLimitedZoneExhausted()
        {
            return UsesLimitedCounter() && remainingUses <= 0;
        }

        private AdsRewardZoneUsageMode GetEffectiveUsageMode()
        {
            if (usageMode != AdsRewardZoneUsageMode.ByRewardType)
            {
                return usageMode;
            }

            return rewardType switch
            {
                AdsRewardType.Ammo9mm => AdsRewardZoneUsageMode.ResourceBased,
                _ => AdsRewardZoneUsageMode.LimitedUses
            };
        }
    }
}
