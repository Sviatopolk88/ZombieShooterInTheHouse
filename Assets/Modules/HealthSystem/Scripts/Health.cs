using System;
using UnityEngine;

namespace Modules.HealthSystem
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IHealth
    {
        [Header("Settings")]
        [Tooltip("Максимальное количество здоровья объекта.")]
        [SerializeField] private int maxHealth = 100;
        [Tooltip("Если включено, объект игнорирует входящий урон.")]
        [SerializeField] private bool invulnerable;
        [Tooltip("Удалять ли объект автоматически при смерти.")]
        [SerializeField] private bool destroyOnDeath;
        [Tooltip("Восстанавливать ли здоровье до максимума при повторном включении объекта.")]
        [SerializeField] private bool restoreHealthOnEnable = true;

        private int currentHealth;
        private bool isDead;
        private DamageContext lastDamageContext;

        public event Action<int, int> OnHealthChanged;
        public event Action<DamageContext, int> OnDamageApplied;
        public event Action<int> OnDamaged;
        public event Action<int> OnHealed;
        public event Action OnDeath;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool IsAlive => !IsDead;
        public float NormalizedHealth => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
        public bool CanTakeDamage => !isDead && !invulnerable && currentHealth > 0;
        public DamageContext LastDamageContext => lastDamageContext;

        private void Awake()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
        }

        private void OnEnable()
        {
            if (restoreHealthOnEnable)
            {
                ResetHealth();
            }
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(new DamageContext(amount));
        }

        public bool TakeDamage(DamageContext context)
        {
            if (!TryBuildDamageContext(context, out DamageContext finalContext))
            {
                return false;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - finalContext.Amount);
            int appliedDamage = previousHealth - currentHealth;

            if (appliedDamage <= 0)
            {
                return false;
            }

            lastDamageContext = finalContext;

            // Порядок событий урона всегда фиксирован:
            // контекст урона -> числовой урон -> изменение HP -> смерть.
            Debug.Log($"{gameObject.name} lost {appliedDamage} health.");
            OnDamageApplied?.Invoke(finalContext, appliedDamage);
            OnDamaged?.Invoke(appliedDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }

            return true;
        }

        public void Heal(int amount)
        {
            if (isDead || amount <= 0)
            {
                return;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            if (currentHealth == previousHealth)
            {
                return;
            }

            int restoredHealth = currentHealth - previousHealth;
            OnHealed?.Invoke(restoredHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Kill()
        {
            if (isDead)
            {
                return;
            }

            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Die();
        }

        public void ResetHealth()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
            isDead = false;
            lastDamageContext = default;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(int value, bool fillHealth = true)
        {
            maxHealth = Mathf.Max(1, value);

            if (fillHealth)
            {
                currentHealth = maxHealth;
                isDead = false;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                isDead = currentHealth <= 0;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public bool CanApplyDamage(in DamageContext context)
        {
            return TryBuildDamageContext(context, out _);
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            OnDeath?.Invoke();

            // Удаляем объект только если это явно требуется настройкой модуля.
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        private bool TryBuildDamageContext(in DamageContext context, out DamageContext finalContext)
        {
            finalContext = default;

            if (!CanTakeDamage)
            {
                return false;
            }

            int amount = Mathf.Max(0, context.Amount);

            if (amount <= 0)
            {
                return false;
            }

            finalContext = context.WithAmount(amount);
            return true;
        }
    }
}
