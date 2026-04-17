using Modules.HealthSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemy
{
    public sealed class EnemyDamageMessageHandler : MonoBehaviour
    {
        private Health health;
        private bool warnedAboutMissingHealth;

        private void Awake()
        {
            // Ищем Health рядом с обработчиком, чтобы получать сообщения об уроне по врагу.
            health = GetComponent<Health>();

            if (health != null)
            {
                health.OnDamaged += OnDamaged;
                return;
            }

            WarnAboutMissingHealth();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
            }
        }

        private void OnDamaged(int damageAmount)
        {
            // Пока используем простое project-level сообщение в консоль.
            Debug.Log($"{gameObject.name} took {damageAmount} damage.");
        }

        private void WarnAboutMissingHealth()
        {
            if (warnedAboutMissingHealth)
            {
                return;
            }

            warnedAboutMissingHealth = true;
            Debug.LogWarning("EnemyDamageMessageHandler: Health component not found.", this);
        }
    }
}
