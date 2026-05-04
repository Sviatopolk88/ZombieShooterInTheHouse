using _Project.Scripts.Gameplay.LevelExit;
using Modules.EnemyAI_Base;
using Modules.HealthSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI.HUD
{
    /// <summary>
    /// Project-side presenter шкалы здоровья босса в сцене Main.
    /// Ищет босса в активном уровне через LevelExitController и показывает шкалу только во время преследования игрока.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class EnemyBossHealthBarPresenter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Слайдер, который отображает текущее и максимальное здоровье босса.")]
        private Slider slider;

        private CanvasGroup canvasGroup;
        private Health trackedBossHealth;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (slider != null)
            {
                slider.interactable = false;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.SetValueWithoutNotify(0f);
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            DetachFromBoss();
            SetVisible(false);
        }

        private void Update()
        {
            Health candidate = ResolveBossHealth();
            if (candidate != trackedBossHealth)
            {
                AttachToBoss(candidate);
            }

            if (trackedBossHealth == null)
            {
                SetVisible(false);
                return;
            }

            EnemyAI_Base enemyAi = trackedBossHealth.GetComponent<EnemyAI_Base>();
            bool shouldShow = enemyAi != null &&
                enemyAi.IsPursuingPlayer &&
                trackedBossHealth.gameObject.activeInHierarchy &&
                !trackedBossHealth.IsDead;

            if (shouldShow)
            {
                ApplyHealth(trackedBossHealth.CurrentHealth, trackedBossHealth.MaxHealth);
            }

            SetVisible(shouldShow);
        }

        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            ApplyHealth(currentHealth, maxHealth);
        }

        private void OnBossDeath()
        {
            ApplyHealth(0, trackedBossHealth != null ? trackedBossHealth.MaxHealth : 1);
            SetVisible(false);
        }

        private Health ResolveBossHealth()
        {
            LevelExitController levelExitController = LevelExitController.Active;
            if (levelExitController == null)
            {
                return null;
            }

            Health fallbackBoss = null;
            IReadOnlyList<Health> bossHealths = levelExitController.BossHealths;
            if (bossHealths == null)
            {
                return null;
            }

            for (int i = 0; i < bossHealths.Count; i++)
            {
                Health health = bossHealths[i];
                if (health == null || !health.gameObject.activeInHierarchy || health.IsDead)
                {
                    continue;
                }

                if (fallbackBoss == null)
                {
                    fallbackBoss = health;
                }

                EnemyAI_Base enemyAi = health.GetComponent<EnemyAI_Base>();
                if (enemyAi != null && enemyAi.IsPursuingPlayer)
                {
                    return health;
                }
            }

            return fallbackBoss;
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

        private void AttachToBoss(Health bossHealth)
        {
            DetachFromBoss();
            trackedBossHealth = bossHealth;

            if (trackedBossHealth == null)
            {
                ApplyHealth(0, 1);
                return;
            }

            trackedBossHealth.OnHealthChanged += OnHealthChanged;
            trackedBossHealth.OnDeath += OnBossDeath;
            ApplyHealth(trackedBossHealth.CurrentHealth, trackedBossHealth.MaxHealth);
        }

        private void DetachFromBoss()
        {
            if (trackedBossHealth == null)
            {
                return;
            }

            trackedBossHealth.OnHealthChanged -= OnHealthChanged;
            trackedBossHealth.OnDeath -= OnBossDeath;
            trackedBossHealth = null;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
