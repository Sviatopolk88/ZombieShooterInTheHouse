using Modules.HealthSystem;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using UnityEngine;

namespace _Project.Scripts.Pickups
{
    /// <summary>
    /// Простая аптечка с автоподбором без отдельного инвентаря.
    /// </summary>
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Сколько здоровья восстанавливает аптечка при успешном подборе.")]
        private int healAmount = 35;

        [SerializeField]
        [Tooltip("Источник настроек пространственного звука аптечки. Используется для проверки и ручной настройки в Inspector.")]
        private AudioSource pickupAudioSource;

        [SerializeField]
        [Tooltip("Клип, который проигрывается при успешном использовании аптечки. Если не задан, берётся clip из AudioSource.")]
        private AudioClip useSoundClip;

        private bool consumed;

        private void Awake()
        {
            if (pickupAudioSource == null)
                pickupAudioSource = GetComponent<AudioSource>();
        }

        private void Reset()
        {
            pickupAudioSource = GetComponent<AudioSource>();
        }

        private void OnValidate()
        {
            if (pickupAudioSource == null)
                pickupAudioSource = GetComponent<AudioSource>();
        }

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
            PlayPickupSound();
            Destroy(gameObject);
        }

        private void PlayPickupSound()
        {
            AudioClip clip = ResolvePickupSound();
            if (clip == null)
                return;

            NeoFpsAudioManager.PlayEffectAudioAtPosition(clip, transform.position);
        }

        private AudioClip ResolvePickupSound()
        {
            if (useSoundClip != null)
                return useSoundClip;

            if (pickupAudioSource != null)
                return pickupAudioSource.clip;

            return null;
        }
    }
}
