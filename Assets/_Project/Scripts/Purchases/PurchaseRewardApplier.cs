using System.Collections.Generic;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using UnityEngine;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Изолированная project-side логика проверки и выдачи наград за успешные покупки.
    /// Runtime-выдача отделена от ownership и не пишет entitlement сама.
    /// </summary>
    public static class PurchaseRewardApplier
    {
        public const string ShotgunProductId = "weapon_shotgun";
        public const string ShotgunEditorSimulationProductId = "gun";

        private const string ShotgunPrefabResourcePath = "Purchases/Firearm_Shotgun_Quickswitch_Purchase";

        private static FpsInventoryItemBase cachedShotgunPrefab;
        private static bool shotgunPrefabLoaded;

        public static bool TryNormalizeOwnedProductId(string productId, out string normalizedProductId)
        {
            string candidateProductId = string.IsNullOrWhiteSpace(productId)
                ? string.Empty
                : productId.Trim();

            if (IsShotgunProduct(candidateProductId))
            {
                normalizedProductId = ShotgunProductId;
                return true;
            }

            normalizedProductId = string.Empty;
            return false;
        }

        public static bool CanApplyRuntime(string productId, out string reason)
        {
            if (!TryNormalizeOwnedProductId(productId, out string normalizedProductId))
            {
                reason = $"Неизвестный товар '{productId}'.";
                return false;
            }

            if (normalizedProductId == ShotgunProductId)
            {
                return CanApplyShotgunRuntime(out reason);
            }

            reason = $"Неизвестный товар '{productId}'.";
            return false;
        }

        public static bool CanApplyAnyOwnedRuntime(IReadOnlyList<string> ownedProductIds, out string reason)
        {
            reason = "Gameplay ещё не готов к восстановлению покупок.";

            if (ownedProductIds == null || ownedProductIds.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < ownedProductIds.Count; i++)
            {
                if (CanApplyRuntime(ownedProductIds[i], out reason))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryApplyOwnedProduct(string productId, out string message)
        {
            if (TryNormalizeOwnedProductId(productId, out string normalizedProductId) && normalizedProductId == ShotgunProductId)
            {
                return TryGrantShotgun(out message);
            }

            message = $"Неизвестный товар '{productId}'.";
            return false;
        }

        public static void RestoreOwnedPurchases(IReadOnlyList<string> ownedProductIds)
        {
            if (ownedProductIds == null || ownedProductIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < ownedProductIds.Count; i++)
            {
                TryApplyOwnedProduct(ownedProductIds[i], out _);
            }
        }

        private static bool CanApplyShotgunRuntime(out string reason)
        {
            if (!TryGetPlayerInventory(out _))
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

            reason = string.Empty;
            return true;
        }

        private static bool TryGrantShotgun(out string message)
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
            bool itemAdded = false;

            if (existingItem == null)
            {
                InventoryAddResult addResult = inventory.AddItemFromPrefab(shotgunPrefab.gameObject);
                if (addResult == InventoryAddResult.Rejected)
                {
                    message = "NeoFPS отклонил добавление дробовика в инвентарь.";
                    return false;
                }

                existingItem = inventory.GetItem(shotgunPrefab.itemIdentifier);
                itemAdded = existingItem != null;
            }

            if (existingItem == null)
            {
                message = "Дробовик не появился в инвентаре после добавления.";
                return false;
            }

            message = itemAdded
                ? "Дробовик выдан игроку."
                : "Дробовик уже доступен игроку.";
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
            return string.Equals(productId, ShotgunProductId, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(productId, ShotgunEditorSimulationProductId, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
