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
        [Tooltip("Минимальный интервал между запусками атакующего цикла.")]
        [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
        [Tooltip("Задержка перед нанесением урона после старта атаки.")]
        [SerializeField] private float attackDelay = 0.4f;
        [Tooltip("Скорость разворота врага к цели во время подготовки и выполнения атаки.")]
        [SerializeField, Min(0f)] private float attackTurnSpeed = 12f;

        private bool isAttacking;
        private bool damageApplied;
        private Transform target;
        private EnemyAnimationController animationController;
        private Coroutine attackRoutine;
        private float nextAttackTime;

        public bool IsAttacking => isAttacking;
        public int Damage => damage;
        public float AttackDistance => attackDistance;
        public float AttackTurnSpeed => attackTurnSpeed;
        public bool CanStartAttack => !isAttacking && Time.time >= nextAttackTime;

        private void Awake()
        {
            animationController = GetComponent<EnemyAnimationController>();
        }

        public void StartAttack(Transform newTarget)
        {
            TryStartAttack(newTarget);
        }

        public bool TryStartAttack(Transform newTarget)
        {
            if (isAttacking)
            {
                return false;
            }

            if (Time.time < nextAttackTime)
            {
                return false;
            }

            target = newTarget;
            isAttacking = true;
            damageApplied = false;
            nextAttackTime = Time.time + attackCooldown;

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine());
            return true;
        }

        private IEnumerator AttackRoutine()
        {
            if (attackDelay > 0f)
            {
                yield return new WaitForSeconds(attackDelay);
            }

            // Если к моменту удара анимация уже закончилась, урон не наносим.
            if (animationController != null && animationController.HasAnimator && !animationController.IsAttackAnimationActive())
            {
                FinishAttack();
                yield break;
            }

            TryApplyDamage();
            animationController?.StopAttack();

            while (animationController != null && animationController.IsAttackAnimationActive())
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

            if (!EnemyDamageResolver.TryGetDamageable(target, out IDamageable damageable) || !damageable.CanTakeDamage)
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

    internal static class EnemyDamageResolver
    {
        public static bool TryGetDamageable(Component target, out IDamageable damageable)
        {
            damageable = null;

            if (target == null)
            {
                return false;
            }

            damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                return true;
            }

            damageable = target.GetComponentInParent<IDamageable>();
            return damageable != null;
        }
    }
}
