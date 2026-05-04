using UnityEngine;
using _Project.Scripts.Systems.SceneFlow;
using _Project.Scripts.UI;

namespace _Project.Scripts.GameFlow
{
    public sealed class GameFlowService : MonoBehaviour
    {
        public static GameFlowService Instance { get; private set; }

        public enum GameState
        {
            Playing,
            GameOver
        }

        [SerializeField] private DeathScreenController deathScreen;
        [SerializeField] private LevelReloadService levelReloadService;

        private GameState currentState;

        public bool IsGameOver => currentState == GameState.GameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GameFlowService: duplicate instance detected, destroying component.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            currentState = GameState.Playing;
        }

        private void Start()
        {
            CursorStateService.Instance?.SetGameplayMode();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void OnPlayerDied()
        {
            if (currentState == GameState.GameOver)
            {
                return;
            }

            currentState = GameState.GameOver;
            Time.timeScale = 0f;
            CursorStateService.Instance?.EnterGameOverMode();

            if (deathScreen != null)
            {
                deathScreen.Show();
            }
            else
            {
                Debug.LogWarning("GameFlowService: deathScreen is not assigned.", this);
            }
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            currentState = GameState.Playing;
            CursorStateService.Instance?.ExitGameOverMode();

            if (levelReloadService != null)
            {
                levelReloadService.ReloadLevelFromCheckpoint();
            }
            else
            {
                Debug.LogWarning("GameFlowService: levelReloadService is not assigned.", this);
            }
        }
    }
}
