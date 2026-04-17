using Modules.RescueObjective;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Gameplay.Rescue
{
    /// <summary>
    /// Автоматически создаёт LevelRescueController в сцене уровня, если в ней есть RescueObjective.
    /// </summary>
    public static class LevelRescueControllerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureControllerForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureControllerForScene(scene);
        }

        private static void EnsureControllerForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var existingControllers = Object.FindObjectsByType<LevelRescueController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var controller in existingControllers)
            {
                if (controller != null && controller.gameObject.scene == scene)
                {
                    controller.RefreshObjectives();
                    return;
                }
            }

            var objectives = Object.FindObjectsByType<RescueObjective>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var hasObjectivesInScene = false;
            foreach (var objective in objectives)
            {
                if (objective != null && objective.gameObject.scene == scene)
                {
                    hasObjectivesInScene = true;
                    break;
                }
            }

            if (!hasObjectivesInScene)
            {
                return;
            }

            var gameObject = new GameObject("LevelRescueController");
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            gameObject.AddComponent<LevelRescueController>();
        }
    }
}
