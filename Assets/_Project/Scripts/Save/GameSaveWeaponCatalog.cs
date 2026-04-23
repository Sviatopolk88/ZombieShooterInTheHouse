using System.Collections.Generic;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using NeoFPS.Constants;
using NeoFPS.ModularFirearms;
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
        public const int PistolRuntimeItemId = -1790531922;
        public const int ShotgunRuntimeItemId = 1014509881;

        private const string ShotgunPrefabResourcePath = "Purchases/Firearm_Shotgun_Quickswitch_Purchase";
        private const string Ammo12GaugePrefabResourcePath = "Purchases/Inventory_Ammo12gauge_1";

        private static FpsInventoryItemBase cachedShotgunPrefab;
        private static FpsInventoryItemBase cachedAmmo12GaugePrefab;
        private static bool shotgunPrefabLoaded;
        private static bool ammo12GaugePrefabLoaded;

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

            if (inventory.GetItem(FpsInventoryKey.FirearmPistol) != null
                || inventory.GetItem(PistolRuntimeItemId) != null)
            {
                result.Add(PistolWeaponId);
            }

            if (inventory.GetItem(FpsInventoryKey.FirearmShotgun) != null
                || inventory.GetItem(ShotgunRuntimeItemId) != null)
            {
                result.Add(ShotgunWeaponId);
            }

            return result.ToArray();
        }

        public static WeaponMagazineSaveData[] CollectWeaponMagazines(IInventory inventory)
        {
            if (inventory == null)
            {
                return System.Array.Empty<WeaponMagazineSaveData>();
            }

            List<WeaponMagazineSaveData> result = new(2);
            TryCollectWeaponMagazine(inventory, PistolWeaponId, FpsInventoryKey.FirearmPistol, PistolRuntimeItemId, result);
            TryCollectWeaponMagazine(inventory, ShotgunWeaponId, FpsInventoryKey.FirearmShotgun, ShotgunRuntimeItemId, result);
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

        public static bool TryResolveAmmo12GaugePrefab(out FpsInventoryItemBase itemPrefab)
        {
            itemPrefab = GetAmmo12GaugePrefab();
            return itemPrefab != null;
        }

        public static bool TryGetWeaponInventoryItem(IInventory inventory, string weaponId, out IInventoryItem item)
        {
            item = null;

            if (inventory == null || string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            if (string.Equals(weaponId, PistolWeaponId, System.StringComparison.Ordinal))
            {
                return TryGetInventoryItem(inventory, FpsInventoryKey.FirearmPistol, PistolRuntimeItemId, out item);
            }

            if (string.Equals(weaponId, ShotgunWeaponId, System.StringComparison.Ordinal))
            {
                return TryGetInventoryItem(inventory, FpsInventoryKey.FirearmShotgun, ShotgunRuntimeItemId, out item);
            }

            return false;
        }

        private static void TryCollectWeaponMagazine(
            IInventory inventory,
            string weaponId,
            int configuredItemId,
            int runtimeItemId,
            List<WeaponMagazineSaveData> output)
        {
            if (!TryGetInventoryItem(inventory, configuredItemId, runtimeItemId, out IInventoryItem item))
            {
                return;
            }

            if (!TryGetMagazine(item, out int magazine))
            {
                return;
            }

            output.Add(new WeaponMagazineSaveData()
            {
                weaponId = weaponId,
                magazine = magazine
            });
        }

        private static bool TryGetInventoryItem(
            IInventory inventory,
            int configuredItemId,
            int runtimeItemId,
            out IInventoryItem item)
        {
            item = inventory.GetItem(configuredItemId);
            if (item != null)
            {
                return true;
            }

            item = inventory.GetItem(runtimeItemId);
            return item != null;
        }

        private static bool TryGetMagazine(IInventoryItem item, out int magazine)
        {
            magazine = 0;

            IModularFirearm firearm = item?.gameObject.GetComponentInChildren<IModularFirearm>(true);
            IReloader reloader = firearm?.reloader;
            if (reloader == null)
            {
                return false;
            }

            magazine = Mathf.Clamp(reloader.currentMagazine, 0, reloader.magazineSize);
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

        private static FpsInventoryItemBase GetAmmo12GaugePrefab()
        {
            if (ammo12GaugePrefabLoaded)
            {
                return cachedAmmo12GaugePrefab;
            }

            ammo12GaugePrefabLoaded = true;

            GameObject ammoPrefabObject = Resources.Load<GameObject>(Ammo12GaugePrefabResourcePath);
            if (ammoPrefabObject == null)
            {
                return null;
            }

            cachedAmmo12GaugePrefab = ammoPrefabObject.GetComponent<FpsInventoryItemBase>();
            return cachedAmmo12GaugePrefab;
        }
    }
}
