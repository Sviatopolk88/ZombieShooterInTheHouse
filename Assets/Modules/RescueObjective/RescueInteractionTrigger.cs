using UnityEngine;

namespace Modules.RescueObjective
{
    /// <summary>
    /// MVP-спасение через вход подходящего объекта в trigger-зону.
    /// Сам objective остаётся независимым и может быть спасён внешним кодом напрямую.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RescueInteractionTrigger : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Цель спасения, которая будет переведена в состояние Rescued.")]
        [SerializeField] private RescueObjective objective;

        [Header("Activation")]
        [Tooltip("Если включено, цель спасается сразу при входе подходящего объекта в trigger.")]
        [SerializeField] private bool rescueOnEnter = true;
        [Tooltip("Слои объектов, которым разрешено активировать спасение.")]
        [SerializeField] private LayerMask activatorLayers = ~0;
        [Tooltip("Необязательный тег активатора. Если заполнен, спасение сработает только для объектов с этим тегом.")]
        [SerializeField] private string requiredTag = "Player";

        private void Reset()
        {
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null)
            {
                ownCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!MatchesFilter(other))
            {
                return;
            }

            if (rescueOnEnter)
            {
                TryRescue();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!MatchesFilter(other))
            {
                return;
            }

            if (!rescueOnEnter)
            {
                TryRescue();
            }
        }

        private bool MatchesFilter(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            GameObject candidate = other.gameObject;
            if ((activatorLayers.value & (1 << candidate.layer)) == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !candidate.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        private void TryRescue()
        {
            if (objective == null || !objective.IsWaitingForRescue)
            {
                return;
            }

            objective.Rescue();
        }
    }
}
