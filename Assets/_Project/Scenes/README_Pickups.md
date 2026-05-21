# Подбор предметов NeoFPS

## Что использовать в проекте

Текущий игрок использует `FpsInventoryQuickSwitch`, поэтому для Level нужно брать только
pickup-prefab из ветки `QuickSwitchInventory` и стандартные ammo-pickup NeoFPS.

Подходящие prefab:

- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/QuickSwitchInventory/WeaponPickup_Pistol_Quickswitch.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/QuickSwitchInventory/WeaponPickup_Shotgun_Quickswitch.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/QuickSwitchInventory/WeaponPickup_AssaultRifle_Quickswitch.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/QuickSwitchInventory/WeaponPickup_Revolver_Quickswitch.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/QuickSwitchInventory/WeaponPickup_SniperRifle_Quickswitch.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/QuickSwitchInventory/WeaponPickup_GrenadeLauncher_Quickswitch.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/Ammo/Pickup_Ammo9mm_30.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/Ammo/Pickup_Ammo12gauge_16.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/Ammo/Pickup_Ammo357magnum_12.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/Ammo/Pickup_Ammo556mm_60.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/Ammo/Pickup_Ammo762mm_14.prefab`
- `Assets/NeoFPS/Samples/Shared/Prefabs/Weapons/Ammo/Pickup_Ammo40mm_2.prefab`

## Как это работает

- Подбор патронов делает штатный `InventoryItemPickup`.
- Контактный подбор патронов делает штатный `PickupTriggerZone`.
- Подбор оружия делает штатный `InteractivePickup`.
- Подбор аптечки делает project-side `HealthPickup` через trigger-автоподбор.
- Для weapon pickup по `E` критичны два параметра:
- дистанция проверки в `CharacterInteractionHandler`;
- размер корневого trigger `BoxCollider` на объекте `InteractivePickup`.
- У modular firearm pickup есть встроенный `ModularFirearmAmmoPickup`, который хранит патроны магазина, лежащие в самом оружии.
- Дубликаты оружия обрабатываются штатным поведением `FpsInventoryQuickSwitch`: для текущего инвентаря duplicate behaviour равен `Reject`, поэтому уже имеющееся оружие второй раз в inventory не добавляется.
- `PickupTriggerZone` не отвечает за weapon pickup по `E`. Он нужен для trigger-based pickup, а не для raycast interaction.

## Правила

- Для `QuickSwitch` использовать только `WeaponPickup_*_Quickswitch`.
- Ammo pickup делать через штатный `InventoryItemPickup`.
- Health pickup в проекте делать через `Assets/_Project/Scripts/Pickups/HealthPickup.cs`.
- Объём ammo менять через project-side variant prefab в `Assets/_Project/Prefabs/Pickups/Ammo/`.
- Лечение аптечки задавать через `healAmount` на project-side prefab.
- Текущая стандартная аптечка проекта: `Pickup_Health_35` и она лечит `+35 HP`.
- Если у игрока полное здоровье, аптечка не тратится и остаётся на месте.
- Weapon pickup в проекте должен работать по `tap E`, без удержания.
- Если делаете project-side variant оружия, принудительно ставьте `m_HoldDuration = 0`.
- Для удобства weapon pickup настраивайте корневой `BoxCollider` у `InteractivePickup`, а не `PickupTriggerZone`.
- Стартовая безопасная настройка для веб-прототипа: немного увеличить `m_Size.x / y` и умеренно увеличить `m_Size.z`, не трогая дистанцию взаимодействия игрока.
- Vendor prefab NeoFPS не редактировать.
- Дубликаты оружия в текущем inventory-mode отклоняются штатно.

## Пайплайн для расстановки на уровне

1. Перетащите на сцену нужный `WeaponPickup_*_Quickswitch.prefab`, если хотите дать новое оружие.
2. Если pickup нужен повторно в проекте, создайте project-side variant в `Assets/_Project/Prefabs/Pickups/Weapons/`.
3. Для project-side weapon pickup зафиксируйте UX: обычное нажатие `E`, без удержания.
4. Для удобства подбора настраивайте у variant корневой `BoxCollider`:
- `m_Size` определяет рабочую зону наведения для raycast interaction.
- `m_Center` помогает поднять / сместить hit area без изменения визуальной модели.
5. Не меняйте `PickupTriggerZone` ради weapon pickup: он не влияет на интерактивный подбор по `E`.
6. Перетащите на сцену нужный `Pickup_Ammo*.prefab`, если хотите дать отдельный запас патронов.
7. Перетащите на сцену `Assets/_Project/Prefabs/Pickups/Health/Pickup_Health_35.prefab`, если хотите дать аптечку.
8. Для аптечки не нужен `E`: она подбирается автоматически через trigger.
9. У аптечки регулируйте только:
- `SphereCollider` trigger для удобства подбора;
- `healAmount` для размера лечения.
10. Для оружия не меняйте inventory-тип prefab на `Stacked` или `Swappable`, пока игрок использует `QuickSwitch`.
11. Для ammo не делайте кастомный счётчик патронов: количество задаётся самим NeoFPS ammo item prefab.
12. Если нужен другой объём патронов, создайте project-side variant/копию ammo pickup prefab и поменяйте в нём `m_ItemPrefab` на нужный ammo item prefab NeoFPS.

## HUD и интерфейс

- Для быстрого HUD оружия и патронов NeoFPS уже даёт готовые компоненты:
- `HudAmmoCounter` для патронов в магазине и в резерве.
- `HudInventoryStandardPC` для quickswitch-слотов и иконок оружия.
- `HudFirearmMode` для режима стрельбы.
- `HudInteractionTooltip` для подсказки `Press / Hold` и текста взаимодействия.
- Стандартный prefab `Assets/NeoFPS/Samples/Shared/Prefabs/HUD/HUD.prefab` уже собран вокруг `SoloPlayerCharacterEventWatcher`, поэтому HUD может жить отдельно от player и подписываться на локального персонажа через событие NeoFPS.
- Для нашей 3-сценной архитектуры это означает, что штатный HUD NeoFPS можно попробовать размещать в `Main`, если в этой сцене есть watcher и canvas живёт достаточно долго.
- Минимальный следующий шаг для прототипа: не писать свой HUD с нуля, а взять из NeoFPS только `HudAmmoCounter` и `HudInventoryStandardPC`, посадив их под один root с `SoloPlayerCharacterEventWatcher`.

## Что уже есть в Level_1

В `Level_1` добавлены демонстрационные pickup рядом со стартом игрока и они ссылаются на project-side prefab в `_Project/Prefabs/Pickups`:

- `Pickup_Ammo_9mm_30`
- `Pickup_Weapon_Shotgun`
- `Pickup_Ammo_12Gauge_16`
- `Pickup_Health_35` можно использовать как готовый project-side prefab для аптечки

Для автомата подготовлены project-owned prefab без правки `Assets/NeoFPS`:

- `Assets/_Project/Resources/Weapons/Firearm_AssaultRifle_Quickswitch_Project.prefab`
- `Assets/_Project/Resources/Weapons/BackupWeapon_Hands_Project.prefab`
- `Assets/_Project/Resources/Weapons/Inventory_Ammo556mm_60.prefab`
- `Assets/_Project/Prefabs/Pickups/Weapons/Pickup_Weapon_AssaultRifle.prefab`
- `Assets/_Project/Prefabs/Pickups/Ammo/Pickup_Ammo_556mm_60.prefab`

`Pickup_Weapon_AssaultRifle` собран по pattern `Pickup_Weapon_Shotgun`: тот же набор project-side компонентов, root trigger / `InteractivePickup`, `ModularFirearmAmmoPickup` и `tap E` (`m_HoldDuration = 0`). Отличаются только визуальная модель автомата, tooltip, ссылка на `Firearm_AssaultRifle_Quickswitch_Project` и встроенный ammo type `556mm`.

`Pickup_Ammo_556mm_60` собран по pattern `Pickup_Ammo_12Gauge_16`: тот же trigger pickup flow, но `m_ItemPrefab` указывает на project-owned `Inventory_Ammo556mm_60`.

Hands/melee подключён как стартовый `BackupWeapon_Hands_Project` в `NeoFPS_PlayerLoadoutAdapter` сцен `Level_1`, `Level_2` и `Level_3`. В project copy он переведён из backup slot `-1` в normal QuickSwitch slot `0`, чтобы выбираться как отдельное оружие, отображаться в weapon UI и не висеть поверх firearm. Автомат и `556mm` ammo не расставлены в scene YAML автоматически: для проверки перетащите `Pickup_Weapon_AssaultRifle` и `Pickup_Ammo_556mm_60` в нужную точку уровня, чтобы не рисковать ручной правкой prefab instance в сцене.

## Текущая проектная настройка

- `Pickup_Weapon_Shotgun` использует `tap E`.
- У `Pickup_Weapon_Shotgun` расширен корневой интерактивный `BoxCollider` для более лёгкого наведения.
- Это project-side override поверх vendor prefab, без правки исходников NeoFPS.
- `Pickup_Health_35` лечит `+35 HP`, подбирается автоматически и не тратится при полном здоровье.
- `Pickup_Weapon_AssaultRifle` использует стандартный NeoFPS `InteractivePickup` и выдаёт QuickSwitch assault rifle.
- `Pickup_Ammo_556mm_60` добавляет запас `556mm`, который расходует assault rifle.
- Save/load сохраняет `firearm_assault_rifle`, состояние магазина автомата и `ammo556mm`.
- `melee_hands` не сохраняется: это baseline weapon из стартового loadout.

## Что проверять руками

1. Игрок стартует с pistol, стартовым `9mm` ammo и `BackupWeapon_Hands_Project`.
2. `Pickup_Weapon_Shotgun` подбирается обычным нажатием `E`, без удержания.
3. У дробовика больше не требуется точное наведение в край модели: interaction hit area ощущается шире.
4. `Pickup_Ammo_9mm_30` добавляет запас патронов для pistol и исчезает / уходит в respawn по правилам prefab.
5. `Pickup_Weapon_Shotgun` добавляет shotgun в QuickSwitch inventory.
6. После подбора shotgun его можно выбрать через штатное переключение оружия NeoFPS.
7. `Pickup_Ammo_12Gauge_16` добавляет патроны для shotgun.
8. После смерти и рестарта уровня pickup-объекты возвращаются, потому что сцена Level пересоздаётся целиком.
9. `Pickup_Health_35` лечит только при неполном здоровье, после успешного лечения исчезает.
10. `BackupWeapon_Hands_Project` доступен в quickswitch/loadout и наносит melee-урон штатным input NeoFPS.
11. `Pickup_Weapon_AssaultRifle` добавляет автомат, автомат стреляет и расходует `556mm`.
12. `Pickup_Ammo_556mm_60` пополняет запас `556mm`.
13. После save/load автомат и `Ammo556mm` восстанавливаются, hands снова приходят из стартового loadout, а pistol/shotgun/9mm/12Gauge продолжают работать как раньше.
