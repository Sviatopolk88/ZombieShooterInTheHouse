using Modules.HealthSystem;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI.HUD
{
    /// <summary>
    /// Проектный HUD-представитель здоровья игрока.
    /// Живёт в Main и получает текущее здоровье игрока из Level через watcher NeoFPS.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class PlayerHealthHudPresenter : PlayerCharacterHudBase
    {
        [SerializeField] private Slider slider;

        private Health playerHealth;

        protected override void Awake()
        {
            base.Awake();

            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (slider != null)
            {
                slider.interactable = false;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.SetValueWithoutNotify(1f);
            }
        }

        protected override void OnDestroy()
        {
            DetachFromHealth();
            base.OnDestroy();
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            DetachFromHealth();

            Component characterComponent = character as Component;
            if (characterComponent == null)
            {
                ApplyHealth(0, 1);
                return;
            }

            NeoFPS_PlayerAdapter playerAdapter = characterComponent.GetComponentInChildren<NeoFPS_PlayerAdapter>(true);
            if (playerAdapter != null)
            {
                playerHealth = playerAdapter.GetHealth();
            }

            if (playerHealth == null)
            {
                playerHealth = characterComponent.GetComponentInChildren<Health>(true);
            }

            if (playerHealth == null)
            {
                ApplyHealth(0, 1);
                return;
            }

            playerHealth.OnHealthChanged += OnHealthChanged;
            ApplyHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            ApplyHealth(currentHealth, maxHealth);
        }

        private void ApplyHealth(int currentHealth, int maxHealth)
        {
            if (slider == null)
            {
                return;
            }

            int safeMaxHealth = Mathf.Max(1, maxHealth);
            slider.minValue = 0f;
            slider.maxValue = safeMaxHealth;
            slider.SetValueWithoutNotify(Mathf.Clamp(currentHealth, 0, safeMaxHealth));
        }

        private void DetachFromHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.OnHealthChanged -= OnHealthChanged;
            playerHealth = null;
        }
    }
}
