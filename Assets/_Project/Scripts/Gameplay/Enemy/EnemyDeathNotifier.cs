using Modules.HealthSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemy
{
    public sealed class EnemyDeathNotifier : MonoBehaviour
    {
        private Health health;
        private Collider[] cachedColliders;
        private bool[] initialColliderStates;
        private bool warnedAboutMissingHealth;

        private void Awake()
        {
            // Ищем Health на объекте или выше по иерархии, чтобы компонент работал с вложенной структурой врага.
            health = GetComponent<Health>();

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (health == null)
            {
                WarnAboutMissingHealth();
            }

            CacheColliders();
        }

        private void OnEnable()
        {
            RestoreColliders();

            if (health == null)
            {
                return;
            }

            health.OnDeath += OnDeath;
            EnemyTracker.Instance?.RegisterEnemy();
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.OnDeath -= OnDeath;
        }

        private void OnDeath()
        {
            DisableCollisionColliders();
            EnemyTracker.Instance?.UnregisterEnemy();
        }

        private void CacheColliders()
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
            initialColliderStates = new bool[cachedColliders.Length];

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                initialColliderStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
            }
        }

        private void RestoreColliders()
        {
            if (cachedColliders == null || initialColliderStates == null)
            {
                return;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider cachedCollider = cachedColliders[i];

                if (cachedCollider == null)
                {
                    continue;
                }

                cachedCollider.enabled = initialColliderStates[i];
            }
        }

        private void DisableCollisionColliders()
        {
            if (cachedColliders == null)
            {
                return;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider cachedCollider = cachedColliders[i];

                if (cachedCollider == null || cachedCollider.isTrigger)
                {
                    continue;
                }

                // После смерти убираем только физические столкновения, чтобы игрок не спотыкался о труп.
                cachedCollider.enabled = false;
            }
        }

        private void WarnAboutMissingHealth()
        {
            if (warnedAboutMissingHealth)
            {
                return;
            }

            warnedAboutMissingHealth = true;
            Debug.LogWarning("EnemyDeathNotifier: Health component not found.", this);
        }
    }
}
