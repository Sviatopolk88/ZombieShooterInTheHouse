using Modules.NeoFPS_Adapter;
using NeoFPS;
using UnityEngine;
using YG;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Изолированная project-side логика проверки и выдачи наград за успешные покупки.
    /// </summary>
    public static class PurchaseRewardApplier
    {
        public const string ShotgunProductId = "weapon_shotgun";
        public const string ShotgunEditorSimulationProductId = "gun";

        private const string ShotgunPrefabResourcePath = "Purchases/Firearm_Shotgun_Quickswitch_Purchase";

        private static FpsInventoryItemBase cachedShotgunPrefab;
        private static bool shotgunPrefabLoaded;

        public static bool CanApplyPurchase(string productId, out string reason)
        {
            if (IsShotgunProduct(productId))
            {
                return CanGrantShotgun(out reason);
            }

            reason = $"Неизвестный товар '{productId}'.";
            return false;
        }

        public static bool TryApplyPurchase(string productId, out string message)
        {
            if (IsShotgunProduct(productId))
            {
                return TryGrantShotgun(markAsOwned: true, out message);
            }

            message = $"Неизвестный товар '{productId}'.";
            return false;
        }

        public static void RestoreOwnedPurchases()
        {
            if (!IsShotgunOwned())
            {
                return;
            }

            TryGrantShotgun(markAsOwned: false, out _);
        }

        private static bool CanGrantShotgun(out string reason)
        {
            if (IsShotgunOwned())
            {
                reason = "Дробовик уже куплен и разблокирован.";
                return false;
            }

            if (!TryGetPlayerInventory(out IInventory inventory))
            {
                reason = "Игрок или инвентарь недоступны.";
                return false;
            }

            FpsInventoryItemBase shotgunPrefab = GetShotgunPrefab();
            if (shotgunPrefab == null)
            {
                reason = "Prefab дробовика для покупки не найден в Resources.";
                return false;
            }

            if (inventory.GetItem(shotgunPrefab.itemIdentifier) != null)
            {
                reason = "Дробовик уже есть в текущем инвентаре.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool TryGrantShotgun(bool markAsOwned, out string message)
        {
            if (!TryGetPlayerInventory(out IInventory inventory))
            {
                message = "Игрок или инвентарь недоступны.";
                return false;
            }

            FpsInventoryItemBase shotgunPrefab = GetShotgunPrefab();
            if (shotgunPrefab == null)
            {
                message = "Prefab дробовика для покупки не найден в Resources.";
                return false;
            }

            IInventoryItem existingItem = inventory.GetItem(shotgunPrefab.itemIdentifier);
            if (existingItem == null)
            {
                InventoryAddResult addResult = inventory.AddItemFromPrefab(shotgunPrefab.gameObject);
                if (addResult == InventoryAddResult.Rejected)
                {
                    message = "NeoFPS отклонил добавление дробовика в инвентарь.";
                    return false;
                }

                existingItem = inventory.GetItem(shotgunPrefab.itemIdentifier);
            }

            if (existingItem == null)
            {
                message = "Дробовик не появился в инвентаре после добавления.";
                return false;
            }

            if (markAsOwned)
            {
                MarkShotgunAsOwned();
            }

            message = "Дробовик выдан игроку.";
            return true;
        }

        private static FpsInventoryItemBase GetShotgunPrefab()
        {
            if (shotgunPrefabLoaded)
            {
                return cachedShotgunPrefab;
            }

            shotgunPrefabLoaded = true;

            GameObject shotgunPrefabObject = Resources.Load<GameObject>(ShotgunPrefabResourcePath);
            if (shotgunPrefabObject == null)
            {
                return null;
            }

            cachedShotgunPrefab = shotgunPrefabObject.GetComponent<FpsInventoryItemBase>();
            return cachedShotgunPrefab;
        }

        private static bool TryGetPlayerInventory(out IInventory inventory)
        {
            inventory = null;

            NeoFPS_PlayerAdapter playerAdapter = Object.FindFirstObjectByType<NeoFPS_PlayerAdapter>(FindObjectsInactive.Exclude);
            if (playerAdapter == null)
            {
                return false;
            }

            inventory = playerAdapter.GetComponentInParent<IInventory>();
            return inventory != null;
        }

        private static bool IsShotgunProduct(string productId)
        {
            return string.Equals(productId, ShotgunProductId, System.StringComparison.Ordinal)
                || string.Equals(productId, ShotgunEditorSimulationProductId, System.StringComparison.Ordinal);
        }

        private static bool IsShotgunOwned()
        {
#if Storage_yg
            return YG2.saves != null && YG2.saves.purchasedWeaponShotgun;
#else
            return false;
#endif
        }

        private static void MarkShotgunAsOwned()
        {
#if Storage_yg
            if (YG2.saves == null)
            {
                return;
            }

            if (YG2.saves.purchasedWeaponShotgun)
            {
                return;
            }

            YG2.saves.purchasedWeaponShotgun = true;

            if (YG2.isSDKEnabled)
            {
                YG2.SaveProgress();
            }
#endif
        }
    }
}
