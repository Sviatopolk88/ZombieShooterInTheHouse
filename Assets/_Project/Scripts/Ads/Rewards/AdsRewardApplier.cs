using Modules.AdsCore;
using Modules.HealthSystem;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using NeoFPS.Constants;
using UnityEngine;

namespace _Project.Scripts.Ads
{
    /// <summary>
    /// Изолированная project-side логика проверки и выдачи rewarded-наград.
    /// </summary>
    public static class AdsRewardApplier
    {
        public static bool CanApplyReward(AdsRewardType rewardType, out string reason)
        {
            switch (rewardType)
            {
                case AdsRewardType.Heal:
                    return CanHeal(out reason);

                case AdsRewardType.Ammo9mm:
                    return CanGiveAmmo(out reason);

                default:
                    reason = "Неизвестный тип награды.";
                    return false;
            }
        }

        public static bool TryApplyReward(AdsRewardType rewardType, int rewardAmount, out string message)
        {
            switch (rewardType)
            {
                case AdsRewardType.Heal:
                    return TryHeal(rewardAmount, out message);

                case AdsRewardType.Ammo9mm:
                    return TryGiveAmmo(rewardAmount, out message);

                default:
                    message = "Неизвестный тип награды.";
                    return false;
            }
        }

        private static bool CanHeal(out string reason)
        {
            Health health = GetPlayerHealth();
            if (health == null)
            {
                reason = "Игрок не найден.";
                return false;
            }

            if (health.IsDead)
            {
                reason = "Лечение недоступно после смерти.";
                return false;
            }

            if (health.CurrentHealth >= health.MaxHealth)
            {
                reason = "У игрока полное здоровье.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool TryHeal(int rewardAmount, out string message)
        {
            if (!CanHeal(out message))
            {
                return false;
            }

            Health health = GetPlayerHealth();
            if (health == null)
            {
                message = "Игрок не найден.";
                return false;
            }

            int healthBeforeHeal = health.CurrentHealth;
            health.Heal(rewardAmount);

            if (health.CurrentHealth <= healthBeforeHeal)
            {
                message = "Здоровье не изменилось.";
                return false;
            }

            message = $"Выдано лечение: +{health.CurrentHealth - healthBeforeHeal} HP.";
            return true;
        }

        private static bool CanGiveAmmo(out string reason)
        {
            IInventoryItem ammoItem = GetAmmoItem();
            if (ammoItem == null)
            {
                if (CanRestoreAmmoItem(out reason))
                {
                    return true;
                }

                reason = "Пистолетные патроны недоступны в текущем инвентаре.";
                return false;
            }

            if (ammoItem.quantity >= ammoItem.maxQuantity)
            {
                reason = "Пистолетные патроны уже заполнены.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool TryGiveAmmo(int rewardAmount, out string message)
        {
            if (!CanGiveAmmo(out message))
            {
                return false;
            }

            IInventoryItem ammoItem = GetAmmoItemOrRestore();
            if (ammoItem == null)
            {
                message = "Пистолетные патроны недоступны в текущем инвентаре.";
                return false;
            }

            int quantityBefore = ammoItem.quantity;
            ammoItem.quantity = Mathf.Clamp(
                ammoItem.quantity + rewardAmount,
                0,
                ammoItem.maxQuantity);

            if (ammoItem.quantity <= quantityBefore)
            {
                message = "Количество патронов не изменилось.";
                return false;
            }

            message = $"Выданы патроны 9mm: +{ammoItem.quantity - quantityBefore}.";
            return true;
        }

        private static Health GetPlayerHealth()
        {
            NeoFPS_PlayerAdapter playerAdapter = Object.FindFirstObjectByType<NeoFPS_PlayerAdapter>(FindObjectsInactive.Exclude);
            return playerAdapter != null ? playerAdapter.GetHealth() : null;
        }

        private static IInventoryItem GetAmmoItem()
        {
            if (!TryGetPlayerInventoryContext(out _, out IInventory inventory, out _))
            {
                return null;
            }

            return inventory.GetItem(FpsInventoryKey.Ammo9mm);
        }

        private static IInventoryItem GetAmmoItemOrRestore()
        {
            if (!TryGetPlayerInventoryContext(out _, out IInventory inventory, out NeoFPS_PlayerLoadoutAdapter loadoutAdapter))
            {
                return null;
            }

            IInventoryItem ammoItem = inventory.GetItem(FpsInventoryKey.Ammo9mm);
            if (ammoItem != null)
            {
                return ammoItem;
            }

            if (loadoutAdapter == null)
            {
                return null;
            }

            FpsInventoryItemBase ammoPrefab = loadoutAdapter.GetStartupItemPrefab(FpsInventoryKey.Ammo9mm);
            if (ammoPrefab == null)
            {
                return null;
            }

            InventoryAddResult addResult = inventory.AddItemFromPrefab(ammoPrefab.gameObject);
            if (addResult == InventoryAddResult.Rejected)
            {
                return null;
            }

            return inventory.GetItem(FpsInventoryKey.Ammo9mm);
        }

        private static bool CanRestoreAmmoItem(out string reason)
        {
            if (!TryGetPlayerInventoryContext(out _, out _, out NeoFPS_PlayerLoadoutAdapter loadoutAdapter))
            {
                reason = "Игрок или инвентарь недоступны.";
                return false;
            }

            if (loadoutAdapter == null)
            {
                reason = "NeoFPS_PlayerLoadoutAdapter не найден на игроке.";
                return false;
            }

            FpsInventoryItemBase ammoPrefab = loadoutAdapter.GetStartupItemPrefab(FpsInventoryKey.Ammo9mm);
            if (ammoPrefab == null)
            {
                reason = "Prefab патронов 9mm не найден в стартовом loadout игрока.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool TryGetPlayerInventoryContext(
            out NeoFPS_PlayerAdapter playerAdapter,
            out IInventory inventory,
            out NeoFPS_PlayerLoadoutAdapter loadoutAdapter)
        {
            playerAdapter = Object.FindFirstObjectByType<NeoFPS_PlayerAdapter>(FindObjectsInactive.Exclude);
            inventory = null;
            loadoutAdapter = null;

            if (playerAdapter == null)
            {
                return false;
            }

            inventory = playerAdapter.GetComponentInParent<IInventory>();
            if (inventory == null)
            {
                return false;
            }

            loadoutAdapter = playerAdapter.GetComponentInParent<NeoFPS_PlayerLoadoutAdapter>();
            return true;
        }
    }
}
