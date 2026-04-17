using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace _Project.Scripts.Systems.SceneFlow
{
    /// <summary>
    /// Project-side интеграция с lifecycle веб-платформы.
    /// Сообщает Yandex Games, что игра готова к взаимодействию, после загрузки игрового уровня.
    /// </summary>
    public static class ProjectPlatformLifecycle
    {
        private static bool gameReadyReported;
        private static bool waitingForSdkData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            gameReadyReported = false;
            waitingForSdkData = false;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            YG2.onGetSDKData -= OnGetSdkData;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ProjectSceneNames.FirstLevel || gameReadyReported)
            {
                return;
            }

            TryReportGameReady();
        }

        private static void TryReportGameReady()
        {
            if (gameReadyReported)
            {
                return;
            }

            if (YG2.isSDKEnabled)
            {
                gameReadyReported = true;
                waitingForSdkData = false;
                YG2.onGetSDKData -= OnGetSdkData;
                YG2.GameReadyAPI();
                return;
            }

            if (waitingForSdkData)
            {
                return;
            }

            waitingForSdkData = true;
            YG2.onGetSDKData -= OnGetSdkData;
            YG2.onGetSDKData += OnGetSdkData;
        }

        private static void OnGetSdkData()
        {
            waitingForSdkData = false;
            TryReportGameReady();
        }
    }
}
