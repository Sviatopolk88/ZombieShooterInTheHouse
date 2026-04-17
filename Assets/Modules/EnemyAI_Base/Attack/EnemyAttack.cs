using System.Collections;
using Modules.EnemyAI_Base.Animation;
using Modules.HealthSystem;
using UnityEngine;

namespace Modules.EnemyAI_Base.Attack
{
    public sealed class EnemyAttack : MonoBehaviour
    {
        [Header("Урон")]
        [Tooltip("Количество урона за один удар.")]
        [SerializeField] private int damage = 10;

        [Header("Дистанция")]
        [Tooltip("Максимальная дистанция, на которой удар может нанести урон.")]
        [SerializeField] private float attackDistance = 1.5f;

        [Header("Тайминг")]
        [Tooltip("Задержка перед нанесением урона после старта атаки.")]
        [SerializeField] private float attackDelay = 0.4f;

        private bool isAttacking;
        private bool damageApplied;
        private Transform target;
        private Animator animator;
        private EnemyAnimationController animationController;
        private Coroutine attackRoutine;

        public bool IsAttacking => isAttacking;
        public int Damage => damage;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            animationController = GetComponent<EnemyAnimationController>();
        }

        public void StartAttack(Transform newTarget)
        {
            if (isAttacking)
            {
                return;
            }

            target = newTarget;
            isAttacking = true;
            damageApplied = false;

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            if (attackDelay > 0f)
            {
                yield return new WaitForSeconds(attackDelay);
            }

            // Если к моменту удара анимация уже закончилась, урон не наносим.
            if (animator != null && !IsInAttackState())
            {
                FinishAttack();
                yield break;
            }

            TryApplyDamage();
            animationController?.StopAttack();

            while (IsInAttackState())
            {
                yield return null;
            }

            FinishAttack();
        }

        private void TryApplyDamage()
        {
            if (damageApplied || target == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance > attackDistance)
            {
                return;
            }

            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable == null)
            {
                damageable = target.GetComponentInParent<IDamageable>();
            }

            if (damageable == null || !damageable.CanTakeDamage)
            {
                return;
            }

            DamageContext context = new DamageContext(damage, source: gameObject);

            if (!damageable.CanApplyDamage(context))
            {
                return;
            }

            damageable.TakeDamage(context);
            damageApplied = true;
        }

        private bool IsInAttackState()
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Attack") || state.IsName("Base Layer.Attack"))
            {
                return true;
            }

            if (!animator.IsInTransition(0))
            {
                return false;
            }

            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            return nextState.IsName("Attack") || nextState.IsName("Base Layer.Attack");
        }

        private void FinishAttack()
        {
            isAttacking = false;
            damageApplied = false;
            target = null;
            attackRoutine = null;
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            FinishAttack();
        }
    }
}
