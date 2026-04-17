using Modules.HealthSystem;
using Modules.NeoFPS_Adapter;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using _Project.Scripts.Gameplay.Enemy;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.SceneFlow;

namespace _Project.Scripts.DebugTools
{
    public sealed class DebugUIController : MonoBehaviour
    {
        private const float RefreshInterval = 0.2f;
        private const float PlayerSearchRetryInterval = 1f;
        private const float MissingPlayerWarningDelay = 3f;
        private const int EnemyKillDamage = 9999;
        private const int PlayerDebugDamage = 25;
        private const int PlayerDebugHeal = 25;

        [Header("UI")]
        [SerializeField] private TMP_Text enemiesCountText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text timeScaleText;

        [Header("Debug Keys")]
        [SerializeField] private KeyCode killAllEnemiesKey = KeyCode.F1;
        [SerializeField] private KeyCode reloadLevelKey = KeyCode.F2;
        [SerializeField] private KeyCode damagePlayerKey = KeyCode.F3;
        [SerializeField] private KeyCode healPlayerKey = KeyCode.F4;
        [SerializeField] private KeyCode pauseToggleKey = KeyCode.F5;

        private float refreshTimer;
        private float nextPlayerSearchTime;
        private Health playerHealth;
        private PlayerDeathHandler playerDeathHandler;
        private NeoFPS_PlayerAdapter playerAdapter;
        private bool warnedAboutMissingPlayer;

        private void Awake()
        {
            CachePlayerHealth();
        }

        private void OnEnable()
        {
            refreshTimer = RefreshInterval;
            RefreshUI();
        }

        private void Update()
        {
            HandleDebugInput();

            // UI обновляется по таймеру в реальном времени, чтобы работать даже на паузе.
            refreshTimer += Time.unscaledDeltaTime;

            if (refreshTimer < RefreshInterval)
            {
                return;
            }

            refreshTimer = 0f;
            RefreshUI();
        }

        private void HandleDebugInput()
        {
            if (UnityEngine.Input.GetKeyDown(killAllEnemiesKey))
            {
                KillAllEnemies();
            }

            if (UnityEngine.Input.GetKeyDown(reloadLevelKey))
            {
                ReloadLevel();
            }

            if (UnityEngine.Input.GetKeyDown(damagePlayerKey))
            {
                DamagePlayer();
            }

            if (UnityEngine.Input.GetKeyDown(healPlayerKey))
            {
                HealPlayer();
            }

            if (UnityEngine.Input.GetKeyDown(pauseToggleKey))
            {
                PauseToggle();
            }
        }

        public void KillAllEnemies()
        {
            EnemyDeathNotifier[] enemyNotifiers = FindObjectsByType<EnemyDeathNotifier>(FindObjectsSortMode.None);

            foreach (EnemyDeathNotifier enemyNotifier in enemyNotifiers)
            {
                if (enemyNotifier == null)
                {
                    continue;
                }

                Health enemyHealth = enemyNotifier.GetComponent<Health>();

                if (enemyHealth == null)
                {
                    enemyHealth = enemyNotifier.GetComponentInParent<Health>();
                }

                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    continue;
                }

                enemyHealth.TakeDamage(EnemyKillDamage);
            }

            RefreshUI();
        }

        public void ReloadLevel()
        {
            LevelReloadService.Instance?.ReloadLevel();
        }

        public void DamagePlayer()
        {
            Health cachedPlayerHealth = GetPlayerHealth();

            if (cachedPlayerHealth == null)
            {
                return;
            }

            cachedPlayerHealth.TakeDamage(PlayerDebugDamage);
            RefreshUI();
        }

        public void HealPlayer()
        {
            Health cachedPlayerHealth = GetPlayerHealth();

            if (cachedPlayerHealth == null)
            {
                return;
            }

            cachedPlayerHealth.Heal(Mathf.Max(PlayerDebugHeal, cachedPlayerHealth.MaxHealth));
            RefreshUI();
        }

        public void PauseToggle()
        {
            Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
            RefreshUI();
        }

        private void RefreshUI()
        {
            UpdateEnemiesCountText();
            UpdatePlayerHpText();
            UpdateTimeScaleText();
        }

        private void UpdateEnemiesCountText()
        {
            if (enemiesCountText == null)
            {
                return;
            }

            EnemyTracker enemyTracker = EnemyTracker.Instance;
            enemiesCountText.text = enemyTracker != null
                ? enemyTracker.AliveEnemies.ToString()
                : "N/A";
        }

        private void UpdatePlayerHpText()
        {
            if (playerHpText == null)
            {
                return;
            }

            Health cachedPlayerHealth = GetPlayerHealth();

            playerHpText.text = cachedPlayerHealth != null
                ? $"{cachedPlayerHealth.CurrentHealth}/{cachedPlayerHealth.MaxHealth}"
                : "N/A";
        }

        private void UpdateTimeScaleText()
        {
            if (timeScaleText == null)
            {
                return;
            }

            timeScaleText.text = Time.timeScale.ToString("0.##");
        }

        private Health GetPlayerHealth()
        {
            if (playerHealth != null)
            {
                return playerHealth;
            }

            CachePlayerHealth();

            if (playerHealth == null)
            {
                WarnAboutMissingPlayer();
            }

            return playerHealth;
        }

        private void CachePlayerHealth()
        {
            if (playerHealth != null)
            {
                return;
            }

            // Если игрок ещё не появился, повторяем поиск редко, а не на каждом обновлении UI.
            if (Time.unscaledTime < nextPlayerSearchTime)
            {
                return;
            }

            nextPlayerSearchTime = Time.unscaledTime + PlayerSearchRetryInterval;

            if (playerAdapter == null)
            {
                playerAdapter = FindFirstObjectByType<NeoFPS_PlayerAdapter>();
            }

            if (playerAdapter != null)
            {
                playerHealth = playerAdapter.GetHealth();

                if (playerHealth != null)
                {
                    playerDeathHandler = playerAdapter.GetComponentInChildren<PlayerDeathHandler>();
                    warnedAboutMissingPlayer = false;
                    return;
                }
            }

            if (playerDeathHandler == null)
            {
                playerDeathHandler = FindFirstObjectByType<PlayerDeathHandler>();
            }

            if (playerDeathHandler != null)
            {
                playerHealth = playerDeathHandler.GetComponent<Health>();

                if (playerHealth == null)
                {
                    playerHealth = playerDeathHandler.GetComponentInParent<Health>();
                }
            }

            if (playerHealth != null)
            {
                warnedAboutMissingPlayer = false;
                return;
            }

            Health[] healthComponents = FindObjectsByType<Health>(FindObjectsSortMode.None);

            foreach (Health health in healthComponents)
            {
                if (health == null || health.GetComponent<EnemyDeathNotifier>() != null)
                {
                    continue;
                }

                if (health.GetComponentInChildren<PlayerDeathHandler>() != null ||
                    health.GetComponentInParent<PlayerDeathHandler>() != null)
                {
                    playerHealth = health;
                    playerDeathHandler = health.GetComponentInChildren<PlayerDeathHandler>();
                    warnedAboutMissingPlayer = false;
                    break;
                }
            }
        }

        private void WarnAboutMissingPlayer()
        {
            if (warnedAboutMissingPlayer)
            {
                return;
            }

            if (!IsGameplaySceneLoaded())
            {
                return;
            }

            if (Time.unscaledTime < MissingPlayerWarningDelay)
            {
                return;
            }

            warnedAboutMissingPlayer = true;
            Debug.LogWarning("DebugUIController: Health игрока не найден.", this);
        }

        private static bool IsGameplaySceneLoaded()
        {
            Scene levelScene = SceneManager.GetSceneByName(ProjectSceneNames.FirstLevel);
            return levelScene.IsValid() && levelScene.isLoaded;
        }
    }
}
