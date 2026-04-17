using Modules.HealthSystem;
using Modules.NeoFPS_Adapter;
using UnityEngine;

namespace _Project.Scripts.Pickups
{
    /// <summary>
    /// Простая аптечка с автоподбором без отдельного инвентаря.
    /// </summary>
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField] private int healAmount = 35;

        private bool consumed;

        private void OnTriggerEnter(Collider other)
        {
            TryConsume(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryConsume(other);
        }

        private void TryConsume(Collider other)
        {
            if (consumed || healAmount <= 0 || other == null)
                return;

            NeoFPS_PlayerAdapter playerAdapter = other.GetComponentInParent<NeoFPS_PlayerAdapter>();
            if (playerAdapter == null)
                return;

            Health health = playerAdapter.GetHealth();
            if (health == null || health.IsDead || health.CurrentHealth >= health.MaxHealth)
                return;

            int healthBeforeHeal = health.CurrentHealth;
            health.Heal(healAmount);

            if (health.CurrentHealth <= healthBeforeHeal)
                return;

            consumed = true;
            Destroy(gameObject);
        }
    }
}
