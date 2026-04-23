using System.Collections.Generic;
using Modules.HealthSystem;
using UnityEngine;

namespace Modules.EnemyAI_Base.Attack
{
    public sealed class AttackHitbox : MonoBehaviour
    {
        [Tooltip("Включает legacy-режим контактного урона через trigger hitbox. Обычно должен быть выключен, чтобы не дублировать EnemyAttack.")]
        [SerializeField] private bool enableContactDamage;

        private EnemyAttack enemyAttack;
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        private bool wasAttacking;

        private void Awake()
        {
            enemyAttack = GetComponentInParent<EnemyAttack>();
        }

        private void Update()
        {
            if (enemyAttack == null)
            {
                return;
            }

            if (enemyAttack.IsAttacking && !wasAttacking)
            {
                hitTargets.Clear();
            }

            wasAttacking = enemyAttack.IsAttacking;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Legacy-режим контактного урона отключён по умолчанию,
            // чтобы не дублировать урон новой системы через EnemyAttack.
            if (!enableContactDamage)
            {
                return;
            }

            if (enemyAttack == null || !enemyAttack.IsAttacking || other == null)
            {
                return;
            }

            if (other.transform.IsChildOf(enemyAttack.transform))
            {
                return;
            }

            if (!EnemyDamageResolver.TryGetDamageable(other, out IDamageable damageable) || !damageable.CanTakeDamage)
            {
                return;
            }

            if (!hitTargets.Add(damageable))
            {
                return;
            }

            DamageContext context = new DamageContext(enemyAttack.Damage, source: enemyAttack.gameObject);

            if (!damageable.CanApplyDamage(context))
            {
                return;
            }

            damageable.TakeDamage(context);
        }
    }
}
