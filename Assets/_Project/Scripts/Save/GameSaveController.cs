using System;
using System.Collections;
using _Project.Scripts.Purchases;
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
        private bool checkpointCaptureInProgress;
        private bool checkpointRestorePending;
        private bool loadInProgress;
        private bool suppressNextAutoLoad;
        private GameSaveData levelStartCheckpoint;

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

            return Save(data);
        }

        public bool SaveGameForLevel(string levelSceneName)
        {
            if (!SaveDataCollector.TryCollect(out GameSaveData data))
            {
                return false;
            }

            if (!TryParseLevelIndex(levelSceneName, out int levelIndex))
            {
                Debug.LogWarning($"GameSaveController: не удалось определить индекс уровня '{levelSceneName}'. Сохранение пропущено.");
                return false;
            }

            data.currentLevel = levelIndex;
            bool saved = Save(data);
            if (saved)
            {
                levelStartCheckpoint = data.Clone();
            }

            return saved;
        }

        private bool Save(GameSaveData data)
        {
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

        public bool ResetProgressForDevelopment()
        {
            if (!IsDevelopmentResetAllowed())
            {
                Debug.LogWarning("GameSaveController: сброс сохранения доступен только в editor/debug build.");
                return false;
            }

            if (!SaveService.Instance.IsReady)
            {
                Debug.LogWarning("GameSaveController: save provider ещё не готов, сброс сохранения отложен.");
                return false;
            }

            bool saveReset = SaveService.Instance.Delete(SaveKey);
            bool ownershipReset = ProjectPurchasesOwnershipStore.TryResetForDevelopment(out string ownershipMessage);

            checkpointCaptureInProgress = false;
            checkpointRestorePending = false;
            loadInProgress = false;
            levelStartCheckpoint = null;
            autoLoadCompleted = true;
            suppressNextAutoLoad = true;

            Debug.Log(
                $"GameSaveController: development reset выполнен. SaveKey='{SaveKey}' удалён: {saveReset}. " +
                $"Ownership reset: {ownershipReset}. {ownershipMessage}");

            return saveReset && ownershipReset;
        }

        public bool QueueLevelCheckpointRestore()
        {
            if (levelStartCheckpoint == null)
            {
                Debug.LogWarning("GameSaveController: checkpoint текущего уровня не найден.");
                return false;
            }

            checkpointRestorePending = true;
            return true;
        }

        public void SuppressAutoLoadForNextGameplayScene()
        {
            suppressNextAutoLoad = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || !IsGameplayLevelScene(scene.name))
            {
                return;
            }

            if (suppressNextAutoLoad)
            {
                suppressNextAutoLoad = false;
                autoLoadCompleted = true;

                if (checkpointRestorePending)
                {
                    StartCoroutine(RestoreCheckpointRoutine());
                }
                else
                {
                    EnsureLevelStartCheckpointCaptured();
                }

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
                EnsureLevelStartCheckpointCaptured();
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

            // Save restore заменяет runtime-loadout целиком, поэтому ownership-покупки
            // нужно переапплаить после него, чтобы купленное оружие не терялось между сессиями.
            ProjectPurchaseService.RestoreOwnedPurchases();

            levelStartCheckpoint = data.Clone();
            checkpointRestorePending = false;
            checkpointCaptureInProgress = false;
            loadInProgress = false;
            autoLoadCompleted = markAutoLoaded || autoLoadCompleted;

            Debug.Log($"GameSaveController: прогресс загружен. Уровень {Mathf.Max(1, data.currentLevel)}, оружие: {(data.weapons != null ? data.weapons.Length : 0)}, ammo9mm: {Mathf.Max(0, data.ammo9mm)}.");
        }

        private IEnumerator RestoreCheckpointRoutine()
        {
            checkpointRestorePending = false;

            if (levelStartCheckpoint == null)
            {
                yield break;
            }

            loadInProgress = true;
            yield return SaveDataApplier.Apply(levelStartCheckpoint.Clone());
            ProjectPurchaseService.RestoreOwnedPurchases();
            loadInProgress = false;

            Debug.Log($"GameSaveController: checkpoint уровня восстановлен. Уровень {Mathf.Max(1, levelStartCheckpoint.currentLevel)}, оружие: {(levelStartCheckpoint.weapons != null ? levelStartCheckpoint.weapons.Length : 0)}, ammo9mm: {Mathf.Max(0, levelStartCheckpoint.ammo9mm)}.");
        }

        private void EnsureLevelStartCheckpointCaptured()
        {
            if (checkpointCaptureInProgress || loadInProgress)
            {
                return;
            }

            if (TryGetCurrentLoadedLevelIndex(out int currentLevelIndex)
                && levelStartCheckpoint != null
                && levelStartCheckpoint.currentLevel == currentLevelIndex)
            {
                return;
            }

            StartCoroutine(CaptureLevelStartCheckpointRoutine());
        }

        private IEnumerator CaptureLevelStartCheckpointRoutine()
        {
            checkpointCaptureInProgress = true;

            const float timeoutSeconds = 5f;
            float endTime = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < endTime)
            {
                if (SaveDataCollector.TryCollect(out GameSaveData data, logWarnings: false))
                {
                    levelStartCheckpoint = data.Clone();
                    checkpointCaptureInProgress = false;
                    Debug.Log($"GameSaveController: зафиксирован checkpoint старта уровня {Mathf.Max(1, data.currentLevel)}.");
                    yield break;
                }

                yield return null;
            }

            checkpointCaptureInProgress = false;
            Debug.LogWarning("GameSaveController: не удалось зафиксировать checkpoint старта уровня.");
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

        private static bool TryParseLevelIndex(string sceneName, out int levelIndex)
        {
            levelIndex = 0;

            if (!IsGameplayLevelScene(sceneName))
            {
                return false;
            }

            return int.TryParse(sceneName.Substring("Level_".Length), out levelIndex) && levelIndex > 0;
        }

        private static bool TryGetCurrentLoadedLevelIndex(out int levelIndex)
        {
            levelIndex = 0;

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (TryParseLevelIndex(scene.name, out levelIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDevelopmentResetAllowed()
        {
#if UNITY_EDITOR
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }
}
