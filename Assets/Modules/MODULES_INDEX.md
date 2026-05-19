# MODULES_INDEX

Аудит выполнен по фактическому коду в `Assets/Modules`, `Assets/_Project` и по обнаруженным vendor-интеграциям. Ниже перечислены явные модульные папки и сильные кандидаты на вынос из project layer.

| Module | Path | Status | Dependencies | Used for Bake or Die? | Migration risk | Notes |
|---|---|---|---|---|---|---|
| `HealthSystem` | `Assets/Modules/HealthSystem` | Reusable | Unity runtime | Yes | Low | База HP, death, heal, `DamageContext`. |
| `DamageSystem` | `Assets/Modules/DamageSystem` | Reusable | `Modules.HealthSystem` | Yes | Low | Единая точка нанесения урона через `IDamageable`. |
| `SceneLoader` | `Assets/Modules/SceneLoader` | Reusable | Unity `SceneManagement` | Yes | Low | Синхронный scene helper, без async orchestration. |
| `EnemySpawner` | `Assets/Modules/EnemySpawner` | Reusable with cleanup | Unity runtime | Yes, ограниченно | Medium | Это не wave-system, а простой burst spawn. |
| `EnemyAI_Base` | `Assets/Modules/EnemyAI_Base` | Reusable with cleanup | `Modules.DamageSystem`, `Modules.HealthSystem`, `NavMeshAgent`, `Animator` | Yes | Medium | Переносим, но требует cleanup animator/tag assumptions. |
| `RescueObjective` | `Assets/Modules/RescueObjective` | Reusable | `Modules.HealthSystem` | Only if rescue loop is needed | Low | Нейтральное objective-ядро без UI и level stats. |
| `DoorBreachEncounter` | `Assets/Modules/DoorBreachEncounter` | Reusable with cleanup | Unity runtime, scene references | Maybe | Medium | Подходит для scripted encounter, не для общей door-system. |
| `AdsCore` | `Assets/Modules/AdsCore` | Reusable with cleanup | `PluginYG2` / `YG2` | Only if Yandex/Web ads are required | Medium | Ядро переносимо, provider и reward enum жёстко Yandex-ориентированы. |
| `SaveSystem` | `Assets/Modules/SaveSystem` | Reusable with cleanup | `PluginYG2` / `YG2`, `JsonUtility` | Maybe | Medium | Хорошее ядро save layer, provider можно заменить. |
| `PurchasesCore` | `Assets/Modules/PurchasesCore` | Reusable with cleanup | `PluginYG2` / `YG2` purchases | No for current MVP scope | Medium | Нужен project-side entitlement/reward слой. |
| `NeoFPS_Adapter` | `Assets/Modules/NeoFPS_Adapter` | Project-specific | `NeoFPS`, `NeoFPS.Managers`, `NeoSaveGames`, `Modules.HealthSystem`, `Modules.DamageSystem` | No | High | Это bridge под конкретный vendor stack и текущие prefab. |
| `Project.SceneFlow` | `Assets/_Project/Scripts/Systems/SceneFlow` | Project-specific | `Modules.SceneLoader`, `AdsCore`, `SaveSystem`, `PurchasesCore`, `PluginYG2` | No as-is | High | Жёстко знает `_Bootstrap`, `_Main`, `Level_1..3` и текущий level flow. |
| `Project.SaveFlow` | `Assets/_Project/Scripts/Save` | Project-specific | `SaveSystem`, `NeoFPS_Adapter`, `NeoFPS`, `ProjectSceneNames` | No as-is | High | Сохраняет NeoFPS loadout, ammo и текущую схему scene naming. |
| `Project.AdsRewardFlow` | `Assets/_Project/Scripts/Ads` | Reusable with cleanup | `AdsCore`, `ProjectLocalizationYG`, `NeoFPS` inventory/health | Maybe later | Medium | Можно вынести паттерн reward bridge, но текущие награды `Heal`/`Ammo9mm` project-specific. |
| `Project.PurchaseFlow` | `Assets/_Project/Scripts/Purchases` | Project-specific | `PurchasesCore`, `PluginYG2` storage, `NeoFPS`, `Resources/Purchases` | No | High | Завязан на shotgun entitlement и YG storage migration. |
| `Project.Pickups` | `Assets/_Project/Scripts/Pickups` | Reusable with cleanup | `NeoFPS_PlayerAdapter`, `NeoFPS`, `HealthSystem` | Maybe partial | Medium | `HealthPickup` почти reusable, `WeaponPickupAutoTrigger` жёстко NeoFPS-specific. |
| `Project.LevelRescueFlow` | `Assets/_Project/Scripts/Gameplay/Rescue*` | Project-specific | `RescueObjective`, scene scanning, UI/HUD hooks | Only if same rescue loop is reused | Medium | Это orchestration и visual bridges вокруг reusable `RescueObjective`. |
| `Project.UI_HUD_Localization` | `Assets/_Project/Scripts/UI` | Project-specific | `NeoFPS`, `HealthSystem`, `YG2`, `LevelRescueController`, `LevelExitController` | No | High | HUD, result screens и локализация сильно завязаны на текущий scene composition. |
| `PluginYG2` | `Assets/PluginYourGames` | Vendor / external | Vendor asset | No direct migration from this audit | High | Vendor-код не менять; использовать только как внешнюю зависимость. |
| `NeoFPS` | `Assets/NeoFPS` | Vendor / external | Vendor asset | No direct migration from this audit | High | Vendor framework, не часть переносимых модулей проекта. |

## Дополнительные выводы

- `GameAnalytics` в проекте не найден ни в `Assets`, ни в `Packages`, ни в `ProjectSettings`.
- Отдельного `day-night cycle` модуля по коду не найдено.
- Полноценного `wave spawning` модуля не найдено: есть только `EnemySpawner` без волн, таймеров и lifetime management.
- Build-helper слой ограничен editor/debug утилитами в `_Project/Editor/Development`; reusable build pipeline модуля не обнаружено.
