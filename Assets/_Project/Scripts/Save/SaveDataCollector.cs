using Modules.NeoFPS_Adapter;
using NeoFPS;
using NeoFPS.Constants;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Save
{
    /// <summary>
    /// Извлекает сериализуемые данные прогресса из текущего состояния игры.
    /// </summary>
    public static class SaveDataCollector
    {
        public static bool TryCollect(out GameSaveData data)
        {
            return TryCollect(out data, logWarnings: true);
        }

        public static bool TryCollect(out GameSaveData data, bool logWarnings)
        {
            data = new GameSaveData()
            {
                currentLevel = GetCurrentLevelIndex(),
                weapons = System.Array.Empty<string>(),
                weaponMagazines = System.Array.Empty<WeaponMagazineSaveData>(),
                ammo9mm = 0,
                ammo12Gauge = 0
            };

            if (!TryGetPlayerInventory(out IInventory inventory))
            {
                if (logWarnings)
                {
                    Debug.LogWarning("SaveDataCollector: игрок или инвентарь недоступны. Сохранение пропущено, чтобы не перезаписать прогресс пустыми данными.");
                }

                return false;
            }

            data.weapons = GameSaveWeaponCatalog.CollectOwnedWeapons(inventory);
            data.weaponMagazines = GameSaveWeaponCatalog.CollectWeaponMagazines(inventory);

            IInventoryItem ammoItem = inventory.GetItem(FpsInventoryKey.Ammo9mm);
            data.ammo9mm = ammoItem != null ? Mathf.Max(0, ammoItem.quantity) : 0;

            IInventoryItem ammo12GaugeItem = inventory.GetItem(FpsInventoryKey.Ammo12gauge);
            data.ammo12Gauge = ammo12GaugeItem != null ? Mathf.Max(0, ammo12GaugeItem.quantity) : 0;

            return true;
        }

        private static int GetCurrentLevelIndex()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (TryParseLevelIndex(scene.name, out int levelIndex))
                {
                    return levelIndex;
                }
            }

            return 1;
        }

        private static bool TryParseLevelIndex(string sceneName, out int levelIndex)
        {
            levelIndex = 0;

            if (string.IsNullOrWhiteSpace(sceneName) || !sceneName.StartsWith("Level_", System.StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(sceneName.Substring("Level_".Length), out levelIndex) && levelIndex > 0;
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
    }
}
