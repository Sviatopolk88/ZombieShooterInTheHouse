using UnityEngine;
using UnityEngine.SceneManagement;
using _Project.Scripts.GameFlow;
using _Project.Scripts.Systems.SceneFlow;

namespace _Project.Scripts.UI.LevelComplete
{
    /// <summary>
    /// Project-side контроллер экрана завершения уровня.
    /// Использует уже подготовленный VictoryScreen в Main и не требует cross-scene ссылок из Level.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelCompleteScreenController : MonoBehaviour
    {
        private const string RootObjectName = "LevelCompleteScreenController";

        private VictoryScreenController victoryScreen;

        public static LevelCompleteScreenController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static LevelCompleteScreenController EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.BindVictoryScreen();
                return Instance;
            }

            Scene mainScene = SceneManager.GetSceneByName(ProjectSceneNames.Main);
            if (!mainScene.IsValid() || !mainScene.isLoaded)
            {
                return null;
            }

            return EnsureInstanceInScene(mainScene);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BindVictoryScreen();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ShowResult(bool bossDefeated, int rescuedCount, int totalCount, int failedCount, int remainingCount)
        {
            if (!BindVictoryScreen())
            {
                Debug.LogWarning("LevelCompleteScreenController: VictoryScreenController not found in Main scene.", this);
                return;
            }

            Time.timeScale = 0f;
            CursorStateService.Instance?.SetUiMode();

            victoryScreen.SetNextAction(null);
            victoryScreen.SetButtonsVisible(false, true);
            victoryScreen.SetBossDefeatedVisible(bossDefeated);
            victoryScreen.Show(rescuedCount, totalCount, failedCount, remainingCount);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ProjectSceneNames.Main)
            {
                return;
            }

            EnsureInstanceInScene(scene);
        }

        private static LevelCompleteScreenController EnsureInstanceInScene(Scene scene)
        {
            if (Instance != null)
            {
                Instance.BindVictoryScreen();
                return Instance;
            }

            GameObject existingRoot = FindRootInScene(scene);
            if (existingRoot != null)
            {
                LevelCompleteScreenController existingController = existingRoot.GetComponent<LevelCompleteScreenController>();
                if (existingController != null)
                {
                    Instance = existingController;
                    existingController.BindVictoryScreen();
                    return existingController;
                }
            }

            GameObject rootObject = new(RootObjectName);
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            LevelCompleteScreenController controller = rootObject.AddComponent<LevelCompleteScreenController>();
            return controller != null && controller == Instance ? controller : null;
        }

        private static GameObject FindRootInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == RootObjectName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private bool BindVictoryScreen()
        {
            if (victoryScreen != null)
            {
                return true;
            }

            Scene mainScene = SceneManager.GetSceneByName(ProjectSceneNames.Main);
            if (!mainScene.IsValid() || !mainScene.isLoaded)
            {
                return false;
            }

            VictoryScreenController[] screens = Resources.FindObjectsOfTypeAll<VictoryScreenController>();
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] != null && screens[i].gameObject.scene == mainScene)
                {
                    victoryScreen = screens[i];
                    return true;
                }
            }

            return false;
        }
    }
}
