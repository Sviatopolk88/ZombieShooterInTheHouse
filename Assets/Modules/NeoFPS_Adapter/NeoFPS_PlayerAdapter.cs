using Modules.HealthSystem;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    public sealed class NeoFPS_PlayerAdapter : MonoBehaviour, IDamageable
    {
        private Health health;
        private bool warnedAboutMissingHealth;

        public bool CanTakeDamage => health != null && health.CanTakeDamage;

        private void Awake()
        {
            // Ищем Health в родителях, потому что NeoFPS может иметь вложенную структуру объекта игрока.
            health = GetComponentInParent<Health>();

            if (health != null)
            {
                health.OnDeath += OnDeath;
                return;
            }

            WarnAboutMissingHealth();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= OnDeath;
            }
        }

        public Health GetHealth()
        {
            if (health == null)
            {
                WarnAboutMissingHealth();
            }

            return health;
        }

        public bool CanApplyDamage(in DamageContext context)
        {
            if (health == null)
            {
                WarnAboutMissingHealth();
                return false;
            }

            return health != null && health.CanApplyDamage(context);
        }

        public void TakeDamage(int amount)
        {
            if (health == null)
            {
                WarnAboutMissingHealth();
                return;
            }

            health.TakeDamage(amount);
        }

        public bool TakeDamage(DamageContext context)
        {
            if (health == null)
            {
                WarnAboutMissingHealth();
                return false;
            }

            return health != null && health.TakeDamage(context);
        }

        private void OnDeath()
        {
            // Пока просто фиксируем смерть игрока в лог, чтобы подготовить точку расширения под Game Over.
            Debug.Log("Player died");
        }

        private void WarnAboutMissingHealth()
        {
            if (warnedAboutMissingHealth)
            {
                return;
            }

            warnedAboutMissingHealth = true;
            Debug.LogWarning("NeoFPS_PlayerAdapter: Health component not found in parent hierarchy.", this);
        }
    }
}
