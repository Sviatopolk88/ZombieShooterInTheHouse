using System;
using System.Collections;
using Modules.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace _Project.Scripts.Save
{
    /// <summary>
    /// Project-side orchestration слой сохранения и загрузки.
    /// Связывает reusable SaveService с DTO/collector/applier конкретной игры.
    /// </summary>
    public sealed class GameSaveController : MonoBehaviour
    {
        private const string RuntimeObjectName = "GameSaveControllerRuntime";
        private const string SaveKey = "game_progress_v1";

        private static GameSaveController instance;

        private bool autoLoadCompleted;
        private bool loadInProgress;

        public static GameSaveController Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            instance = runtimeObject.AddComponent<GameSaveController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            YG2.onGetSDKData -= OnGetSdkData;
            YG2.onGetSDKData += OnGetSdkData;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            YG2.onGetSDKData -= OnGetSdkData;
        }

        public bool SaveGame()
        {
            if (!SaveDataCollector.TryCollect(out GameSaveData data))
            {
                return false;
            }

            bool saved = SaveService.Instance.Save(SaveKey, data);

            if (saved)
            {
                Debug.Log($"GameSaveController: прогресс сохранён. Уровень {data.currentLevel}, оружие: {data.weapons.Length}, ammo9mm: {data.ammo9mm}.");
            }

            return saved;
        }

        public bool LoadGame()
        {
            if (loadInProgress)
            {
                return false;
            }

            if (!SaveService.Instance.IsReady)
            {
                return false;
            }

            if (!SaveService.Instance.TryLoad(SaveKey, out GameSaveData data))
            {
                return false;
            }

            StartCoroutine(LoadRoutine(data, markAutoLoaded: true));
            return true;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || !IsGameplayLevelScene(scene.name))
            {
                return;
            }

            autoLoadCompleted = false;
            TryAutoLoad();
        }

        private void OnGetSdkData()
        {
            TryAutoLoad();
        }

        private void TryAutoLoad()
        {
            if (autoLoadCompleted || loadInProgress)
            {
                return;
            }

            if (!IsAnyGameplayLevelLoaded())
            {
                return;
            }

            if (!SaveService.Instance.IsReady)
            {
                return;
            }

            if (!SaveService.Instance.HasKey(SaveKey))
            {
                autoLoadCompleted = true;
                return;
            }

            if (!SaveService.Instance.TryLoad(SaveKey, out GameSaveData data))
            {
                autoLoadCompleted = true;
                Debug.LogWarning("GameSaveController: сохранение найдено, но не удалось его загрузить.");
                return;
            }

            StartCoroutine(LoadRoutine(data, markAutoLoaded: true));
        }

        private IEnumerator LoadRoutine(GameSaveData data, bool markAutoLoaded)
        {
            loadInProgress = true;

            yield return SaveDataApplier.Apply(data);

            loadInProgress = false;
            autoLoadCompleted = markAutoLoaded || autoLoadCompleted;

            Debug.Log($"GameSaveController: прогресс загружен. Уровень {Mathf.Max(1, data.currentLevel)}, оружие: {(data.weapons != null ? data.weapons.Length : 0)}, ammo9mm: {Mathf.Max(0, data.ammo9mm)}.");
        }

        private static bool IsAnyGameplayLevelLoaded()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && IsGameplayLevelScene(scene.name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGameplayLevelScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && sceneName.StartsWith("Level_", StringComparison.Ordinal);
        }
    }
}
