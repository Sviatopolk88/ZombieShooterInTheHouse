# Управление

## 1. Общая информация

- В проекте используется штатная input-система NeoFPS.
- Кастомные project-side бинды управления сейчас не внедрены, кроме отдельного переключения курсора на `Tab`.
- Фактические gameplay-бинды берутся из дефолтной конфигурации [FpsManager_Input.asset](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/NeoFPS/Resources/FpsManager_Input.asset).
- Файл [FpsSettings_KeyBindings.asset](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/NeoFPS/Resources/FpsSettings_KeyBindings.asset) некорректен: массив биндов обрезан. NeoFPS при такой конфигурации сбрасывает bindings к дефолту, если длина массива не равна `FpsInputButton.count`.

## 2. Активные действия

| Действие | Кнопка | Тип | Примечание |
| -------- | ------ | --- | ---------- |
| Move | `WASD` | Gameplay | Движение персонажа |
| Look | `Mouse` | Gameplay | Осмотр камерой |
| Jump | `Space` | Gameplay | Прыжок |
| Sprint | `Left Shift` | Gameplay | Toggle |
| Crouch | `Left Ctrl` | Gameplay | Toggle |
| Dodge Left | `Double Tap A` | Gameplay | Уклонение влево |
| Dodge Right | `Double Tap D` | Gameplay | Уклонение вправо |
| Lean Left | `Z` | Gameplay | Наклон влево |
| Lean Right | `C` | Gameplay | Наклон вправо |
| Use / Interact | `E` | Gameplay | Взаимодействие с интерактивными объектами и weapon pickup |
| Fire | `LMB` | Gameplay | Основной огонь |
| Aim | `RMB` | Gameplay | Toggle |
| Reload | `R` | Gameplay | Перезарядка |
| Weapon Mode | `MMB` | Gameplay | Переключение режима оружия |
| Inspect | `L` | Gameplay | Осмотр оружия |
| Flashlight | `F` | Gameplay | Если модуль фонаря есть у оружия |
| Scope Brightness | `Keypad + / -` | Gameplay | Если оптика поддерживает яркость |
| Select Slot | `1..0` | Gameplay | Слоты `1-10` |
| Next Weapon | `]` | Gameplay | Следующее оружие |
| Previous Weapon | `[` | Gameplay | Предыдущее оружие |
| Quick Switch | `Q` | Gameplay | Быстрое переключение на предыдущее выбранное |
| Drop Weapon | `G` | Gameplay | Выбросить текущее оружие |
| Holster | `H` | Gameplay | Убрать оружие |
| Scroll Weapon | `Mouse Wheel` | Gameplay | Прокрутка оружия |
| Ability | `Left Alt / Mouse Side Button` | Gameplay | Зарезервировано под ability; эффект зависит от подключённой механики |

## 3. Управление проекта

| Действие | Кнопка | Где реализовано | Примечание |
| -------- | ------ | --------------- | ---------- |
| Toggle Cursor | `Tab` | `CursorToggleInput` | Переключает режим UI / Game |

## 4. Подбор предметов

- `Weapon Pickup` -> `Tap E`, через штатный NeoFPS `InteractivePickup`.
- `Ammo Pickup` -> автоматически, через штатный NeoFPS `InventoryItemPickup`.
- Это поведение относится к NeoFPS и не реализовано project-layer логикой.
- Удобство подбора weapon pickup регулируется размером корневого trigger `BoxCollider` на prefab pickup, а не `PickupTriggerZone`.

## 5. Неиспользуемые / неактивные бинды NeoFPS

- `X` -> `PickUp`, существует в NeoFPS, но в текущем прототипе не используется.
- `/` -> `QuickMenu`, существует в NeoFPS, но не используется в текущем прототипе.
- `F5` -> `QuickSave`, существует в NeoFPS, но не используется в текущем прототипе.
- `F9` -> `QuickLoad`, существует в NeoFPS, но не используется в текущем прототипе.
- `I` -> `Inventory`, существует в NeoFPS, но не используется в текущем прототипе.
- `J` -> `Journal`, существует в NeoFPS, но не используется в текущем прототипе.
- `M` -> `Map`, существует в NeoFPS, но не используется в текущем прототипе.
- `Tab` -> `Stats`, существует в NeoFPS по дефолту, но в проекте фактически перекрыт `CursorToggleInput`.
- `Character` / `Crafting` -> не назначены в дефолтной конфигурации NeoFPS.
- `Menu` / `Back` / `Cancel` -> зарезервированы NeoFPS, но на клавиатуре не назначены.

## 6. Важные замечания

- Текущая система управления в проекте равна дефолтной конфигурации NeoFPS.
- Кастомные input bindings пока не внедрены.
- При добавлении mobile input потребуется отдельная система управления поверх текущей desktop-конфигурации.
- `Tab` для переключения курсора это project-side override, а не штатный NeoFPS binding.
- Любое изменение input через NeoFPS `KeyBindings` требует корректного массива длиной `FpsInputButton.count`.
- Для прототипа weapon pickup зафиксирован как обычный `tap E`, без удержания.
