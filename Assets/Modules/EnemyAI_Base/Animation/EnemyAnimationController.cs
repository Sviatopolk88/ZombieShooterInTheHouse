using UnityEngine;
using UnityEngine.AI;

namespace Modules.EnemyAI_Base.Animation
{
    public sealed class EnemyAnimationController : MonoBehaviour
    {
        private const string SpeedParameter = "Speed";
        private const string IsAttackingParameter = "IsAttacking";
        private const string IsDeadParameter = "IsDead";
        private const string HitParameter = "Hit";
        private const string AttackStateShortName = "Attack";
        private const string AttackStateName = "Base Layer.Attack";
        private const string HitStateShortName = "Hit";
        private const string HitStateName = "Base Layer.Hit";

        private Animator animator;
        private NavMeshAgent agent;
        private bool hasLoggedMissingAnimatorWarning;

        public bool HasAnimator => animator != null;

        private void Awake()
        {
            // Animator ожидается на дочернем объекте с визуальной моделью.
            animator = GetComponentInChildren<Animator>(true);

            // Агент может отсутствовать, это не должно ломать компонент.
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return;
            }

            if (agent == null || !agent.enabled)
            {
                animator.SetFloat(SpeedParameter, 0f);
                return;
            }

            float speed = IsHitAnimationActive() ? 0f : agent.velocity.magnitude;
            animator.SetFloat(SpeedParameter, speed);
        }

        public void PlayAttack()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return;
            }

            animator.SetBool(IsAttackingParameter, true);
        }

        public void StopAttack()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return;
            }

            animator.SetBool(IsAttackingParameter, false);
        }

        public void PlayHit()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return;
            }

            animator.SetTrigger(HitParameter);
        }

        public bool IsHitAnimationActive()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return false;
            }

            if (IsCurrentOrNextState(HitStateShortName, HitStateName))
            {
                return true;
            }

            return false;
        }

        public void PlayDeath()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return;
            }

            animator.SetBool(IsAttackingParameter, false);
            animator.SetBool(IsDeadParameter, true);
        }

        public bool IsAttackAnimationActive()
        {
            if (animator == null)
            {
                LogMissingAnimatorWarning();
                return false;
            }

            if (IsCurrentOrNextState(AttackStateShortName, AttackStateName))
            {
                return true;
            }

            return false;
        }

        private bool IsCurrentOrNextState(string shortStateName, string fullStateName)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName(shortStateName) || currentState.IsName(fullStateName))
            {
                return true;
            }

            if (!animator.IsInTransition(0))
            {
                return false;
            }

            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            return nextState.IsName(shortStateName) || nextState.IsName(fullStateName);
        }

        private void LogMissingAnimatorWarning()
        {
            if (hasLoggedMissingAnimatorWarning)
            {
                return;
            }

            hasLoggedMissingAnimatorWarning = true;
            Debug.LogWarning("EnemyAnimationController: Animator не найден в дочерних объектах.", this);
        }
    }
}
