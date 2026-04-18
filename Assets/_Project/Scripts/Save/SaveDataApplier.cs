using System.Collections;
using System.Collections.Generic;
using Modules.SceneLoader;
using NeoFPS;
using NeoFPS.Constants;
using UnityEngine;
using UnityEngine.SceneManagement;
using _Project.Scripts.Systems.SceneFlow;

namespace _Project.Scripts.Save
{
    /// <summary>
    /// Применяет сериализованные данные прогресса к текущей игровой сессии.
    /// </summary>
    public static class SaveDataApplier
    {
        public static IEnumerator Apply(GameSaveData data)
        {
            if (data == null)
            {
                yield break;
            }

            int targetLevelIndex = Mathf.Max(1, data.currentLevel);
            string targetLevelSceneName = GetSceneNameForLevel(targetLevelIndex);

            yield return EnsureTargetLevelLoaded(targetLevelSceneName);
            yield return WaitForPlayerInventory();

            if (!TryGetPlayerInventory(out IInventory inventory))
            {
                Debug.LogWarning("SaveDataApplier: игрок или инвентарь недоступны. Данные инвентаря не применены.");
                yield break;
            }

            List<IInventoryItem> loadout = BuildLoadout(data);
            inventory.ApplyLoadout(loadout.ToArray(), prefabs: true, replace: true);

            IInventoryItem ammoItem = inventory.GetItem(FpsInventoryKey.Ammo9mm);
            if (ammoItem != null)
            {
                ammoItem.quantity = Mathf.Clamp(data.ammo9mm, 0, ammoItem.maxQuantity);
            }
        }

        private static IEnumerator EnsureTargetLevelLoaded(string targetLevelSceneName)
        {
            if (TryGetLoadedLevelScene(out Scene loadedLevelScene) && loadedLevelScene.name == targetLevelSceneName)
            {
                yield break;
            }

            if (loadedLevelScene.IsValid() && loadedLevelScene.isLoaded)
            {
                SceneLoader.UnloadScene(loadedLevelScene.name);

                while (loadedLevelScene.IsValid() && loadedLevelScene.isLoaded)
                {
                    yield return null;
                    loadedLevelScene = SceneManager.GetSceneByName(loadedLevelScene.name);
                }
            }

            if (!SceneLoader.LoadAdditive(targetLevelSceneName))
            {
                Debug.LogWarning($"SaveDataApplier: не удалось загрузить уровень '{targetLevelSceneName}'.");
                yield break;
            }

            while (!SceneLoader.IsSceneLoaded(targetLevelSceneName))
            {
                yield return null;
            }

            SceneLoader.SetActiveScene(ProjectSceneNames.Main);
        }

        private static IEnumerator WaitForPlayerInventory()
        {
            const float timeoutSeconds = 5f;
            float endTime = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < endTime)
            {
                if (TryGetPlayerInventory(out _))
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static List<IInventoryItem> BuildLoadout(GameSaveData data)
        {
            List<IInventoryItem> result = new();

            string[] weapons = data.weapons ?? System.Array.Empty<string>();
            for (int i = 0; i < weapons.Length; i++)
            {
                if (!GameSaveWeaponCatalog.TryResolveWeaponPrefab(weapons[i], out FpsInventoryItemBase weaponPrefab) || weaponPrefab == null)
                {
                    Debug.LogWarning($"SaveDataApplier: prefab оружия '{weapons[i]}' не найден и будет пропущен.");
                    continue;
                }

                if (ContainsItem(result, weaponPrefab.itemIdentifier))
                {
                    continue;
                }

                result.Add(weaponPrefab);
            }

            if (data.ammo9mm > 0 && GameSaveWeaponCatalog.TryResolveAmmo9mmPrefab(out FpsInventoryItemBase ammoPrefab) && ammoPrefab != null)
            {
                if (!ContainsItem(result, ammoPrefab.itemIdentifier))
                {
                    result.Add(ammoPrefab);
                }
            }

            return result;
        }

        private static bool ContainsItem(List<IInventoryItem> items, int itemIdentifier)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemIdentifier == itemIdentifier)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetSceneNameForLevel(int levelIndex)
        {
            return levelIndex > 0 ? $"Level_{levelIndex}" : ProjectSceneNames.FirstLevel;
        }

        private static bool TryGetLoadedLevelScene(out Scene scene)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (!candidate.IsValid() || !candidate.isLoaded)
                {
                    continue;
                }

                if (candidate.name.StartsWith("Level_", System.StringComparison.Ordinal))
                {
                    scene = candidate;
                    return true;
                }
            }

            scene = default;
            return false;
        }

        private static bool TryGetPlayerInventory(out IInventory inventory)
        {
            inventory = null;

            Modules.NeoFPS_Adapter.NeoFPS_PlayerAdapter playerAdapter =
                Object.FindFirstObjectByType<Modules.NeoFPS_Adapter.NeoFPS_PlayerAdapter>(FindObjectsInactive.Exclude);
            if (playerAdapter == null)
            {
                return false;
            }

            inventory = playerAdapter.GetComponentInParent<IInventory>();
            return inventory != null;
        }
    }
}
