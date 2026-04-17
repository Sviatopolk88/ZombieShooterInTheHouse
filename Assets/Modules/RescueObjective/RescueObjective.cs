using System;
using Modules.HealthSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Modules.RescueObjective
{
    public enum RescueObjectiveState
    {
        WaitingForRescue = 0,
        Rescued = 1,
        Failed = 2
    }

    /// <summary>
    /// Универсальная цель спасения для MVP-логики rescue objective.
    /// Не знает ничего о конкретной игре, врагах или UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RescueObjective : MonoBehaviour, ITargetableEntity
    {
        [Header("State")]
        [Tooltip("Если включено, цель автоматически перейдет в Failed при смерти связанного Health.")]
        [SerializeField] private bool failOnHealthDeath = true;
        [Tooltip("Необязательная явная ссылка на Health. Если не задана, компонент будет найден на объекте или у родителей.")]
        [SerializeField] private Health health;

        [Header("Events")]
        [Tooltip("Вызывается при успешном спасении цели.")]
        [SerializeField] private UnityEvent onRescued;
        [Tooltip("Вызывается при провале цели.")]
        [SerializeField] private UnityEvent onFailed;
        [Tooltip("Вызывается при сбросе состояния обратно в WaitingForRescue.")]
        [SerializeField] private UnityEvent onReset;

        private RescueObjectiveState state = RescueObjectiveState.WaitingForRescue;
        private bool isSubscribedToHealth;

        public event Action<RescueObjective, RescueObjectiveState, RescueObjectiveState> StateChanged;

        public RescueObjectiveState State => state;
        public bool IsWaitingForRescue => state == RescueObjectiveState.WaitingForRescue;
        public bool IsRescued => state == RescueObjectiveState.Rescued;
        public bool IsFailed => state == RescueObjectiveState.Failed;
        public bool IsValidEnemyTarget => IsWaitingForRescue;

        private void Awake()
        {
            ResolveHealth();
        }

        private void OnEnable()
        {
            SubscribeToHealth();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        public bool Rescue()
        {
            if (!IsWaitingForRescue)
            {
                return false;
            }

            SetState(RescueObjectiveState.Rescued);
            onRescued?.Invoke();
            return true;
        }

        public bool Fail()
        {
            if (!IsWaitingForRescue)
            {
                return false;
            }

            SetState(RescueObjectiveState.Failed);
            onFailed?.Invoke();
            return true;
        }

        public void ResetState()
        {
            SetState(RescueObjectiveState.WaitingForRescue);
            onReset?.Invoke();
        }

        private void ResolveHealth()
        {
            if (health != null)
            {
                return;
            }

            health = GetComponent<Health>();
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
        }

        private void SubscribeToHealth()
        {
            if (!failOnHealthDeath)
            {
                return;
            }

            ResolveHealth();
            if (health == null || isSubscribedToHealth)
            {
                return;
            }

            health.OnDeath += OnHealthDeath;
            isSubscribedToHealth = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (health == null || !isSubscribedToHealth)
            {
                return;
            }

            health.OnDeath -= OnHealthDeath;
            isSubscribedToHealth = false;
        }

        private void OnHealthDeath()
        {
            Fail();
        }

        private void SetState(RescueObjectiveState newState)
        {
            if (state == newState)
            {
                return;
            }

            var previousState = state;
            state = newState;
            StateChanged?.Invoke(this, previousState, newState);
        }
    }
}
