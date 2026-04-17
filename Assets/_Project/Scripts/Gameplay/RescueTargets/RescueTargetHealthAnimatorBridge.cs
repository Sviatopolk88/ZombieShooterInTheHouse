using Modules.HealthSystem;
using Modules.RescueObjective;
using UnityEngine;

namespace _Project.Scripts.Gameplay.RescueTargets
{
    /// <summary>
    /// Project-side bridge между Health жителя и его Animator.
    /// Отвечает только за hit/death анимации и не засоряет RescueObjective.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class RescueTargetHealthAnimatorBridge : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Animator жителя. Если не задан, будет найден на объекте или у детей.")]
        [SerializeField] private Animator animator;
        [Tooltip("Необязательная ссылка на RescueObjective, чтобы не сбивать rescued-анимацию у уже спасённого жителя.")]
        [SerializeField] private RescueObjective objective;

        [Header("Параметры Animator")]
        [Tooltip("Trigger-параметр для анимации получения урона.")]
        [SerializeField] private string hitTriggerName = "Hit";
        [Tooltip("Bool-параметр для анимации смерти.")]
        [SerializeField] private string deadBoolName = "IsDead";

        private Health health;
        private bool isSubscribed;

        private void Awake()
        {
            health = GetComponent<Health>();

            if (objective == null)
            {
                objective = GetComponent<RescueObjective>();
            }

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
            ResetAnimatorState();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (health == null || isSubscribed)
            {
                return;
            }

            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (health == null || !isSubscribed)
            {
                return;
            }

            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
            isSubscribed = false;
        }

        private void ResetAnimatorState()
        {
            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(hitTriggerName))
            {
                animator.ResetTrigger(hitTriggerName);
            }

            if (!string.IsNullOrWhiteSpace(deadBoolName))
            {
                animator.SetBool(deadBoolName, false);
            }
        }

        private void OnDamaged(int appliedDamage)
        {
            if (appliedDamage <= 0 || animator == null || health == null || health.IsDead)
            {
                return;
            }

            if (objective != null && objective.IsRescued)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(hitTriggerName))
            {
                return;
            }

            animator.ResetTrigger(hitTriggerName);
            animator.SetTrigger(hitTriggerName);
        }

        private void OnDeath()
        {
            if (animator == null || string.IsNullOrWhiteSpace(deadBoolName))
            {
                return;
            }

            animator.SetBool(deadBoolName, true);
        }
    }
}
