using NeoFPS;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    // Применяет проектный стартовый loadout через штатный API NeoFPS
    // до инициализации дефолтного инвентаря.
    [DisallowMultipleComponent]
    public sealed class NeoFPS_PlayerLoadoutAdapter : MonoBehaviour
    {
        // Конфигурация стартового набора хранится сериализованно на объекте / prefab игрока,
        // чтобы можно было менять loadout без правок кода adapter.
        [SerializeField] private FpsInventoryItemBase[] startupItemPrefabs = new FpsInventoryItemBase[0];
        [SerializeField] private bool replaceExistingStartingItems = true;

        // Защищает от повторного наложения loadout в рамках одного жизненного цикла персонажа.
        private bool applied;

        private void Awake()
        {
            if (applied)
            {
                return;
            }

            IInventory inventory = GetComponent<IInventory>();

            if (inventory == null)
            {
                Debug.LogWarning("NeoFPS_PlayerLoadoutAdapter: IInventory component not found on player.", this);
                return;
            }

            if (startupItemPrefabs == null || startupItemPrefabs.Length == 0)
            {
                Debug.LogWarning("NeoFPS_PlayerLoadoutAdapter: startup loadout is empty.", this);
                return;
            }

            // Вызываем ApplyLoadout именно здесь, потому что FpsInventoryBase заполняет
            // стартовый инвентарь позже, в своём Start(). Так мы успеваем заменить
            // vendor-starting-items до их фактического инстанцирования.
            inventory.ApplyLoadout(startupItemPrefabs, prefabs: true, replace: replaceExistingStartingItems);
            applied = true;
        }

        public FpsInventoryItemBase GetStartupItemPrefab(int itemIdentifier)
        {
            if (startupItemPrefabs == null || startupItemPrefabs.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < startupItemPrefabs.Length; i++)
            {
                FpsInventoryItemBase itemPrefab = startupItemPrefabs[i];
                if (itemPrefab != null && itemPrefab.itemIdentifier == itemIdentifier)
                {
                    return itemPrefab;
                }
            }

            return null;
        }
    }
}
