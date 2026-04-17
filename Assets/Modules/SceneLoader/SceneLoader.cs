using UnityEngine;
using UnityEngine.SceneManagement;

namespace Modules.SceneLoader
{
    public static class SceneLoader
    {
        public static bool LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            // Single используем, когда нужно полностью заменить текущий набор сцен.
            // Это подходит для перехода Bootstrap -> Main или для полной смены состояния игры.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }

        public static bool LoadAdditive(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || IsSceneLoaded(sceneName))
            {
                return false;
            }

            // Additive используем, когда основная сцена должна остаться загруженной.
            // В схеме Bootstrap -> Main -> Level это нужно для подгрузки Level поверх Main.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            return true;
        }

        public static bool UnloadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !IsSceneLoaded(sceneName))
            {
                return false;
            }

            SceneManager.UnloadSceneAsync(sceneName);
            return true;
        }

        public static bool ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.name))
            {
                return false;
            }

            // Перезагрузка активной сцены полезна для рестарта текущего уровня или тестового сценария.
            SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
            return true;
        }

        public static bool SetActiveScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            return SceneManager.SetActiveScene(scene);
        }

        public static bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
