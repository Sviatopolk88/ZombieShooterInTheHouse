using System.Collections;
using _Project.Scripts.GameFlow;
using _Project.Scripts.Save;
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
            GameSaveController.Instance?.SaveGame();
            StartCoroutine(LoadLevelRoutine(GetCurrentLevelName()));
        }

        public void LoadNextLevel()
        {
            string currentLevelName = GetCurrentLevelName();
            string nextLevelName = GetNextLevelName(currentLevelName);
            StartCoroutine(LoadLevelRoutine(nextLevelName, saveAfterLoad: true, suppressAutoLoad: true));
        }

        private IEnumerator LoadLevelRoutine(string levelSceneName, bool saveAfterLoad = false, bool suppressAutoLoad = false)
        {
            const float reloadDelay = 1.5f;

            // Ждём в реальном времени, чтобы переход работал даже при Time.timeScale = 0.
            yield return new WaitForSecondsRealtime(reloadDelay);

            if (TryGetLoadedLevelScene(out Scene loadedLevelScene))
            {
                string loadedLevelName = loadedLevelScene.name;
                SceneLoader.UnloadScene(loadedLevelName);

                while (SceneLoader.IsSceneLoaded(loadedLevelName))
                {
                    yield return null;
                }
            }

            if (suppressAutoLoad)
            {
                GameSaveController.Instance?.SuppressAutoLoadForNextGameplayScene();
            }

            SceneLoader.LoadAdditive(levelSceneName);

            while (!SceneLoader.IsSceneLoaded(levelSceneName))
            {
                yield return null;
            }

            Time.timeScale = 1f;

            Scene mainScene = SceneManager.GetSceneByName(ProjectSceneNames.Main);
            if (mainScene.IsValid() && mainScene.isLoaded)
            {
                SceneLoader.SetActiveScene(ProjectSceneNames.Main);
            }

            if (saveAfterLoad)
            {
                GameSaveController.Instance?.SaveGame();
            }

            CursorStateService.Instance?.SetGameplayMode();
            AdsService.Instance.TryShowInterstitial(AdsInterstitialPlacement.SceneTransition);
        }

        private static string GetCurrentLevelName()
        {
            return TryGetLoadedLevelScene(out Scene levelScene) ? levelScene.name : ProjectSceneNames.FirstLevel;
        }

        private static string GetNextLevelName(string currentLevelName)
        {
            string[] levels = ProjectSceneNames.Levels;
            if (levels == null || levels.Length == 0)
            {
                return ProjectSceneNames.FirstLevel;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == currentLevelName)
                {
                    int nextIndex = (i + 1) % levels.Length;
                    return levels[nextIndex];
                }
            }

            return levels[0];
        }

        private static bool TryGetLoadedLevelScene(out Scene scene)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (!candidate.IsValid() || !candidate.isLoaded)
                {
                    continue;
                }

                if (IsKnownLevelScene(candidate.name))
                {
                    scene = candidate;
                    return true;
                }
            }

            scene = default;
            return false;
        }

        private static bool IsKnownLevelScene(string sceneName)
        {
            string[] levels = ProjectSceneNames.Levels;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == sceneName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
