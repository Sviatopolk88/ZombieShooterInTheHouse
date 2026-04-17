using System.Collections;
using Modules.AdsCore;
using Modules.SceneLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Systems.SceneFlow
{
    public sealed class LevelReloadService : MonoBehaviour
    {
        public static LevelReloadService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("LevelReloadService: duplicate instance detected, destroying component.", this);
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

        public void ReloadLevel()
        {
            StartCoroutine(ReloadLevelRoutine());
        }

        private IEnumerator ReloadLevelRoutine()
        {
            const float reloadDelay = 1.5f;
            string levelSceneName = ProjectSceneNames.FirstLevel;

            // Используем ожидание в реальном времени, чтобы перезагрузка работала даже при Time.timeScale = 0.
            yield return new WaitForSecondsRealtime(reloadDelay);

            if (SceneLoader.IsSceneLoaded(levelSceneName))
            {
                SceneLoader.UnloadScene(levelSceneName);

                while (SceneLoader.IsSceneLoaded(levelSceneName))
                {
                    yield return null;
                }
            }

            SceneLoader.LoadAdditive(levelSceneName);

            while (!SceneLoader.IsSceneLoaded(levelSceneName))
            {
                yield return null;
            }

            // После перезагрузки возвращаем нормальный ход времени для нового старта уровня.
            Time.timeScale = 1f;

            // Main остаётся постоянной сценой с системами, поэтому активную сцену сохраняем за ней.
            Scene mainScene = SceneManager.GetSceneByName(ProjectSceneNames.Main);

            if (mainScene.IsValid() && mainScene.isLoaded)
            {
                SceneLoader.SetActiveScene(ProjectSceneNames.Main);
            }

            AdsService.Instance.TryShowInterstitial(AdsInterstitialPlacement.SceneTransition);
        }
    }
}
