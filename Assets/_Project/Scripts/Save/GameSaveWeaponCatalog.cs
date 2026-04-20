using System.Collections.Generic;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using NeoFPS.Constants;
using UnityEngine;

namespace _Project.Scripts.Save
{
    /// <summary>
    /// Project-side каталог оружия для save-системы.
    /// Хранит только mapping save-id -> prefab / inventory item.
    /// </summary>
    public static class GameSaveWeaponCatalog
    {
        public const string PistolWeaponId = "firearm_pistol";
        public const string ShotgunWeaponId = "weapon_shotgun";

        private const string ShotgunPrefabResourcePath = "Purchases/Firearm_Shotgun_Quickswitch_Purchase";

        private static FpsInventoryItemBase cachedShotgunPrefab;
        private static bool shotgunPrefabLoaded;

        public static FpsInventoryItemBase[] GetStartupLoadoutPrefabs()
        {
            NeoFPS_PlayerLoadoutAdapter loadoutAdapter = Object.FindFirstObjectByType<NeoFPS_PlayerLoadoutAdapter>(FindObjectsInactive.Exclude);
            if (loadoutAdapter == null)
            {
                return System.Array.Empty<FpsInventoryItemBase>();
            }

            return loadoutAdapter.GetStartupItemPrefabs();
        }

        public static string[] CollectOwnedWeapons(IInventory inventory)
        {
            if (inventory == null)
            {
                return System.Array.Empty<string>();
            }

            List<string> result = new(2);

            if (inventory.GetItem(FpsInventoryKey.FirearmPistol) != null)
            {
                result.Add(PistolWeaponId);
            }

            if (inventory.GetItem(FpsInventoryKey.FirearmShotgun) != null)
            {
                result.Add(ShotgunWeaponId);
            }

            return result.ToArray();
        }

        public static bool TryResolveWeaponPrefab(string weaponId, out FpsInventoryItemBase itemPrefab)
        {
            itemPrefab = null;

            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            if (string.Equals(weaponId, PistolWeaponId, System.StringComparison.Ordinal))
            {
                NeoFPS_PlayerLoadoutAdapter loadoutAdapter = Object.FindFirstObjectByType<NeoFPS_PlayerLoadoutAdapter>(FindObjectsInactive.Exclude);
                if (loadoutAdapter == null)
                {
                    return false;
                }

                itemPrefab = loadoutAdapter.GetStartupItemPrefab(FpsInventoryKey.FirearmPistol);
                return itemPrefab != null;
            }

            if (string.Equals(weaponId, ShotgunWeaponId, System.StringComparison.Ordinal))
            {
                itemPrefab = GetShotgunPrefab();
                return itemPrefab != null;
            }

            return false;
        }

        public static bool TryResolveAmmo9mmPrefab(out FpsInventoryItemBase itemPrefab)
        {
            itemPrefab = null;

            NeoFPS_PlayerLoadoutAdapter loadoutAdapter = Object.FindFirstObjectByType<NeoFPS_PlayerLoadoutAdapter>(FindObjectsInactive.Exclude);
            if (loadoutAdapter == null)
            {
                return false;
            }

            itemPrefab = loadoutAdapter.GetStartupItemPrefab(FpsInventoryKey.Ammo9mm);
            return itemPrefab != null;
        }

        public static int GetDefaultAmmo9mmQuantity()
        {
            return TryResolveAmmo9mmPrefab(out FpsInventoryItemBase itemPrefab) && itemPrefab != null
                ? Mathf.Max(0, itemPrefab.quantity)
                : 0;
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
    }
}
