using System.Collections.Generic;
using Modules.AdsCore;
using TMPro;
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

    /// <summary>
    /// Reusable project-side зона поддержки, которая вызывает rewarded ads через project-side reward bridge.
    /// Использует MVP-flow: игрок входит в trigger и осознанно жмёт кнопку взаимодействия.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AdsRewardZone : MonoBehaviour
    {
        [Header("Reward")]
        [SerializeField] private AdsRewardType rewardType = AdsRewardType.Heal;
        [SerializeField] private AdsRewardZoneUsageMode usageMode = AdsRewardZoneUsageMode.ByRewardType;
        [SerializeField] private int maxUses = 1;

        [Header("Activation")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private LayerMask activatorLayers = ~0;
        [SerializeField] private string requiredTag = "Player";

        [Header("Prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptLabel;

        [Header("Behaviour")]
        [SerializeField] private bool disableColliderWhenExhausted = true;

        private readonly HashSet<Collider> occupants = new();
        private bool requestPending;
        private int remainingUses;

        public AdsRewardType RewardType => rewardType;
        public int RemainingUses => remainingUses;

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
            RefreshPrompt();
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
            RefreshPrompt();
        }

        private void OnDisable()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;
            occupants.Clear();
            requestPending = false;
            RefreshPrompt();
        }

        private void Update()
        {
            if (occupants.Count == 0)
            {
                RefreshPrompt();
                return;
            }

            RefreshPrompt();

            if (requestPending || !UnityEngine.Input.GetKeyDown(interactionKey))
            {
                return;
            }

            TryRequestReward();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!MatchesActivator(other))
            {
                return;
            }

            occupants.Add(other);
            RefreshPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null)
            {
                return;
            }

            occupants.Remove(other);
            RefreshPrompt();
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

            if (!string.IsNullOrWhiteSpace(requiredTag) && !candidate.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        private void TryRequestReward()
        {
            if (IsLimitedZoneExhausted())
            {
                RefreshPrompt();
                return;
            }

            if (!ProjectAdsRewardService.TryShowRewarded(rewardType, OnRewardRequestCompleted))
            {
                RefreshPrompt();
                return;
            }

            requestPending = true;
            RefreshPrompt();
        }

        private void OnRewardRequestCompleted(AdsShowResult result)
        {
            requestPending = false;

            if (result.IsSuccess && UsesLimitedCounter())
            {
                remainingUses = Mathf.Max(0, remainingUses - 1);
            }

            ApplyAvailabilityState();
            RefreshPrompt();
        }

        private void ApplyAvailabilityState()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.enabled = !disableColliderWhenExhausted || !IsLimitedZoneExhausted();
            }
        }

        private void RefreshPrompt()
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(occupants.Count > 0);
            }

            if (promptLabel == null)
            {
                return;
            }

            if (occupants.Count == 0)
            {
                promptLabel.text = string.Empty;
                return;
            }

            promptLabel.text = BuildPromptText();
        }

        private string BuildPromptText()
        {
            bool isRussian = IsRussianLanguage();

            if (requestPending)
            {
                return isRussian ? "Запрос рекламы..." : "Ad request...";
            }

            if (IsLimitedZoneExhausted())
            {
                return isRussian ? "Станция исчерпана" : "Station depleted";
            }

            int rewardAmount = ProjectAdsRewardService.GetConfiguredRewardAmount(rewardType);
            if (ProjectAdsRewardService.CanRequestReward(rewardType, out string reason))
            {
                if (UsesLimitedCounter())
                {
                    return isRussian
                    ? $"[{interactionKey}] {GetRewardTitle(true)} +{rewardAmount} | Осталось: {remainingUses}"
                    : $"[{interactionKey}] {GetRewardTitle(false)} +{rewardAmount} | Left: {remainingUses}";
                }

                return isRussian
                    ? $"[{interactionKey}] {GetRewardTitle(true)} +{rewardAmount}"
                    : $"[{interactionKey}] {GetRewardTitle(false)} +{rewardAmount}";
            }

            return isRussian
                ? $"{GetRewardTitle(true)} недоступно: {reason}"
                : $"{GetRewardTitle(false)} unavailable: {reason}";
        }

        private string GetRewardTitle(bool isRussian)
        {
            return rewardType switch
            {
                AdsRewardType.Heal => isRussian ? "Лечение" : "Healing",
                AdsRewardType.Ammo9mm => isRussian ? "Патроны 9mm" : "9mm ammo",
                _ => isRussian ? "Награда" : "Reward"
            };
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshPrompt();
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

        private static bool IsRussianLanguage()
        {
            string language = YG2.lang;
            if (string.IsNullOrWhiteSpace(language))
            {
                language = YG2.envir.language;
            }

            return !string.IsNullOrWhiteSpace(language) && language.ToLowerInvariant().StartsWith("ru");
        }
    }
}
