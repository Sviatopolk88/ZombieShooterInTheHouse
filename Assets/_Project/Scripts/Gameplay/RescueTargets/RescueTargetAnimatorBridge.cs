using Modules.RescueObjective;
using UnityEngine;

namespace _Project.Scripts.Gameplay.RescueTargets
{
    /// <summary>
    /// Связывает нейтральное состояние RescueObjective с конкретным Animator prefab жителя.
    /// Нужен только для визуальной реакции и не засоряет reusable-модуль RescueObjective.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RescueObjective))]
    public sealed class RescueTargetAnimatorBridge : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Animator жителя. Если не задан, будет найден на этом объекте или у детей.")]
        [SerializeField] private Animator animator;

        [Header("Состояния")]
        [Tooltip("Имя базового scared/waiting state в Animator Controller.")]
        [SerializeField] private string waitingStateName = "Scared";
        [Tooltip("Имя trigger-параметра, который запускает позитивную реакцию после спасения.")]
        [SerializeField] private string rescuedTriggerName = "Rescued";
        [Tooltip("Короткая длительность возврата в waiting state при ResetState.")]
        [SerializeField, Min(0f)] private float resetTransitionDuration = 0.1f;

        private RescueObjective objective;
        private RescueObjectiveState lastKnownState;
        private bool stateInitialized;

        private void Awake()
        {
            objective = GetComponent<RescueObjective>();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void OnEnable()
        {
            SyncState(immediate: true);
        }

        private void Update()
        {
            if (objective == null)
            {
                return;
            }

            if (!stateInitialized || objective.State != lastKnownState)
            {
                SyncState(immediate: false);
            }
        }

        private void SyncState(bool immediate)
        {
            if (objective == null)
            {
                return;
            }

            lastKnownState = objective.State;
            stateInitialized = true;

            switch (lastKnownState)
            {
                case RescueObjectiveState.WaitingForRescue:
                    PlayWaiting(immediate);
                    break;
                case RescueObjectiveState.Rescued:
                    PlayRescuedReaction();
                    break;
            }
        }

        private void PlayWaiting(bool immediate)
        {
            if (animator == null || string.IsNullOrWhiteSpace(waitingStateName))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(rescuedTriggerName))
            {
                animator.ResetTrigger(rescuedTriggerName);
            }

            if (immediate || resetTransitionDuration <= 0f)
            {
                animator.Play(waitingStateName, 0, 0f);
                return;
            }

            animator.CrossFadeInFixedTime(waitingStateName, resetTransitionDuration, 0, 0f);
        }

        private void PlayRescuedReaction()
        {
            if (animator == null || string.IsNullOrWhiteSpace(rescuedTriggerName))
            {
                return;
            }

            animator.ResetTrigger(rescuedTriggerName);
            animator.SetTrigger(rescuedTriggerName);
        }
    }
}
