using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Project-side восстановление уже купленных товаров после загрузки runtime и данных SDK.
    /// Ждёт явной готовности gameplay-инвентаря вместо привязки к конкретной сцене.
    /// </summary>
    public static class ProjectPurchasesEntitlementSync
    {
        private const string RuntimeObjectName = "ProjectPurchasesEntitlementSyncRuntime";

        private static ProjectPurchasesEntitlementSyncRunner runner;
        private static bool restorePending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureRunner();

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            YG2.onGetSDKData -= OnGetSdkData;
            YG2.onGetSDKData += OnGetSdkData;

            restorePending = true;
            TryRestoreOwnedPurchases();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            restorePending = true;
            TryRestoreOwnedPurchases();
        }

        private static void OnGetSdkData()
        {
            restorePending = true;
            TryRestoreOwnedPurchases();
        }

        private static void EnsureRunner()
        {
            if (runner != null)
            {
                return;
            }

            GameObject runtimeObject = new(RuntimeObjectName);
            Object.DontDestroyOnLoad(runtimeObject);
            runner = runtimeObject.AddComponent<ProjectPurchasesEntitlementSyncRunner>();
            runner.hideFlags = HideFlags.HideAndDontSave;
        }

        private static void TryRestoreOwnedPurchases()
        {
            if (!restorePending)
            {
                return;
            }

            var ownedProductIds = ProjectPurchasesOwnershipStore.GetOwnedProductIds();
            if (ownedProductIds == null || ownedProductIds.Count == 0)
            {
                restorePending = false;
                return;
            }

            if (!PurchaseRewardApplier.CanApplyAnyOwnedRuntime(ownedProductIds, out _))
            {
                return;
            }

            ProjectPurchaseService.RestoreOwnedPurchases();
            restorePending = false;
        }

        private sealed class ProjectPurchasesEntitlementSyncRunner : MonoBehaviour
        {
            private void Update()
            {
                TryRestoreOwnedPurchases();
            }
        }
    }
}
