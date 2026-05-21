# Save System

## Архитектура

Reusable save-core:

- `Assets/Modules/SaveSystem/Core/ISaveProvider.cs`
- `Assets/Modules/SaveSystem/Core/SaveService.cs`
- `Assets/Modules/SaveSystem/Core/SaveEnums.cs`
- `Assets/Modules/SaveSystem/Core/SaveProjectSettings.cs`
- `Assets/Modules/SaveSystem/Providers/Yandex/YandexSaveProvider.cs`
- `Assets/Modules/SaveSystem/Providers/Yandex/YandexSaveStorageData.cs`

Project-side integration:

- `Assets/_Project/Scripts/Save/GameSaveData.cs`
- `Assets/_Project/Scripts/Save/GameSaveWeaponCatalog.cs`
- `Assets/_Project/Scripts/Save/SaveDataCollector.cs`
- `Assets/_Project/Scripts/Save/SaveDataApplier.cs`
- `Assets/_Project/Scripts/Save/GameSaveController.cs`

Направление зависимостей:

- `_Project` зависит от `Modules.SaveSystem`
- `Modules.SaveSystem` не зависит от `_Project`
- `YandexSaveProvider` зависит от PluginYG2 / `YG2`

## Что Сохраняется

MVP-сохранение фиксирует:

- текущий уровень
- список оружия игрока
- состояние магазинов оружия
- количество патронов `9mm`
- количество патронов `12Gauge`
- количество патронов `556mm`

Данные хранятся в project DTO `GameSaveData` и сериализуются в JSON через `SaveService`.

`GameSaveData.version = 2` добавляет поле `ammo556mm`. Старые сохранения без этого поля остаются валидными: значение по умолчанию равно `0`.

## Provider Yandex / PluginYG2

Текущий provider использует storage-механику PluginYG2:

- данные пишутся в `YG2.saves`
- сохранение уходит через `YG2.SaveProgress()`
- загрузка становится доступной после `YG2.onGetSDKData`

В reusable слое JSON хранится как generic key/value storage внутри `YG2.saves`, без знания о gameplay-полях проекта.

## Flow

Сохранение:

1. `GameSaveController.SaveGame()`
2. `SaveDataCollector` собирает DTO из текущего состояния игры
3. `SaveService` сериализует DTO в JSON
4. `YandexSaveProvider` сохраняет JSON по ключу `game_progress_v1`

Загрузка:

1. `GameSaveController` ждёт загрузку gameplay-level и готовность SDK/save-data
2. `SaveService` читает JSON
3. `SaveDataApplier` при необходимости загружает нужный уровень
4. `SaveDataApplier` восстанавливает loadout, магазины оружия и ammo items

## Оружие и Ammo в Save

Project-side каталог `GameSaveWeaponCatalog` знает только stable save-id и project-owned prefab:

- `firearm_pistol` берётся из стартового loadout текущего `NeoFPS_PlayerLoadoutAdapter`
- `weapon_shotgun` берётся из `Resources/Purchases/Firearm_Shotgun_Quickswitch_Purchase`
- `firearm_assault_rifle` берётся из `Resources/Weapons/Firearm_AssaultRifle_Quickswitch_Project`
- `Ammo556mm` восстанавливается через `Resources/Weapons/Inventory_Ammo556mm_60`

Hands/melee добавлен в стартовый loadout `Level_1`, `Level_2` и `Level_3` как normal QuickSwitch slot `0`, поэтому доступен игроку сразу и отображается в weapon UI. Он не сохраняется как полученное оружие: `melee_hands` считается baseline item, а старые save с этим id молча пропускаются при восстановлении. Автомат не добавлен в стартовый loadout: он должен выдаваться через pickup/reward/content flow, после чего save/load восстановит его по `firearm_assault_rifle`.

## Автоматические Точки Вызова

Сейчас проект использует такие integration points:

- ранний `SaveService.Instance.Warmup()` в `BootstrapSceneStartup`
- автозагрузка прогресса в `GameSaveController` после загрузки уровня и `YG2.onGetSDKData`
- автосохранение при `OnApplicationPause(true)`
- автосохранение при `OnApplicationQuit()`
- сохранение перед `LevelReloadService.ReloadLevel()`

## Как Добавить Новые Поля

1. Расширить `GameSaveData`
2. Дополнить `SaveDataCollector`
3. Дополнить `SaveDataApplier`

Reusable `SaveService` и `YandexSaveProvider` при этом менять не нужно, пока формат остаётся JSON по ключу.

## Как Сменить Provider

1. Создать новую реализацию `ISaveProvider`
2. Подключить её в `SaveService.CreateProvider(...)`
3. Оставить project-side DTO/collector/applier без изменений

## Что Проверить Руками

1. При старте после загрузки `Level_1` сохранение автоматически подхватывается из `YG2.saves`.
2. После рестарта уровня оружие, магазины и `Ammo9mm` / `Ammo12Gauge` / `Ammo556mm` восстанавливаются из последнего save.
3. При отсутствии save проект не падает и стартует с дефолтным состоянием.
4. Повреждённый JSON не ломает игру и даёт warning в лог.
5. После подбора `Pickup_Weapon_AssaultRifle` и `Pickup_Ammo_556mm_60` сохранение содержит `firearm_assault_rifle` и ненулевой `ammo556mm`.
6. После загрузки того же save автомат остаётся в QuickSwitch inventory, а запас `556mm` восстанавливается.
