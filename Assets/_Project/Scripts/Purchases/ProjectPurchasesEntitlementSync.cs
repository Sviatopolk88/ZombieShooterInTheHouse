using UnityEngine;
using UnityEngine.SceneManagement;
using YG;
using _Project.Scripts.Systems.SceneFlow;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Project-side восстановление уже купленных товаров после загрузки уровня и данных SDK.
    /// </summary>
    public static class ProjectPurchasesEntitlementSync
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            YG2.onGetSDKData -= OnGetSdkData;
            YG2.onGetSDKData += OnGetSdkData;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ProjectSceneNames.FirstLevel)
            {
                return;
            }

            ProjectPurchaseService.RestoreOwnedPurchases();
        }

        private static void OnGetSdkData()
        {
            Scene levelScene = SceneManager.GetSceneByName(ProjectSceneNames.FirstLevel);
            if (levelScene.IsValid() && levelScene.isLoaded)
            {
                ProjectPurchaseService.RestoreOwnedPurchases();
            }
        }
    }
}
