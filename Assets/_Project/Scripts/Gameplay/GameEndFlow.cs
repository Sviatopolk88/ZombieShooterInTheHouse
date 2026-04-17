using UnityEngine;
using _Project.Scripts.Systems.SceneFlow;

namespace _Project.Scripts.Gameplay
{
    public sealed class GameEndFlow : MonoBehaviour
    {
        public static GameEndFlow Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GameEndFlow: duplicate instance detected, destroying component.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Win()
        {
            Debug.Log("ПОБЕДА");
            Time.timeScale = 0f;
            LevelReloadService.Instance?.ReloadLevel();
        }

        public void Lose()
        {
            Debug.Log("ПОРАЖЕНИЕ");
            Time.timeScale = 0f;
            LevelReloadService.Instance?.ReloadLevel();
        }
    }
}
