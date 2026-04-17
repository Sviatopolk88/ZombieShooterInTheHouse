# README_HUD

## 1. Общая схема

- Минимальный HUD оружия и патронов живёт в `Main`-сцене.
- Источник данных остаётся в NeoFPS: `player`, `inventory`, `weapon` и `ammo` живут в `Level`.
- Связь между `Main` и `Level` делает штатный watcher NeoFPS: `SoloPlayerCharacterEventWatcher`.
- Полный vendor `HUD.prefab` не используется. В проект подключены только нужные HUD-элементы.

## 2. Что используется

### Main-сцена

- `Canvas (Screen Space Overlay)` содержит `NeoFPS_MinimalHudInstaller`.
- Installer в `Awake()` создаёт:
  - `HUD_Root`
  - `HUD_PC`
  - `HUD_Mobile`
- `HUD_Root` получает `SoloPlayerCharacterEventWatcher`.
- `HUD_Mobile` сейчас создаётся пустым и выключенным.

### HUD для PC

- Crosshair HUD: project-side copy `Assets/_Project/Prefabs/UI/HUD/Crosshair_PC.prefab`
- Ammo HUD: project-side copy `Assets/_Project/Prefabs/UI/HUD/AmmoCounter_PC.prefab`
- Weapon / quickslot HUD: project-side variant `Assets/_Project/Prefabs/UI/HUD/InventoryStandard_PC.prefab`
- Health bar visual base: project-side copy `Assets/_Project/Prefabs/UI/HUD/Health/Loading Bar Green Dark.prefab`
- Player health HUD presenter: `Assets/_Project/Scripts/UI/HUD/PlayerHealthHudPresenter.cs`
- Rescue HUD: project-side text presenter `RescueHudPresenter`, который показывает локализованный progress-текст через `PluginYourGames`

## 3. Почему так

- `HudCrosshair`, `HudAmmoCounter` и `HudInventoryStandardPC` уже умеют брать данные из NeoFPS без project-side дублирования логики.
- `SoloPlayerCharacterEventWatcher` подходит для нашей схемы `Main / Level`, потому что реагирует на смену локального player character.
- Полный `HUD.prefab` NeoFPS не тащится, чтобы не вносить лишние UI-элементы в прототип.
- `InventoryStandard_PC.prefab` вынесен в `_Project`, потому что для прототипа quickslot HUD должен быть постоянно видимым.
- За основу будущей полосы здоровья выбрана project-side копия `Loading Bar Green Dark` из `UI Kit Pro - Casual Glossy`. Она пока не подключена к данным игрока и подготовлена только как визуальный prefab для дальнейшей интеграции.
- Внутри health bar сохранены только базовые элементы: `HealthBackground`, `HealthFillArea` и `HealthFill`. Они уже перевязаны на project-side sprites и готовы к подключению через `Slider` / `fillRect`.
- Текущее здоровье игрока для этой шкалы берётся из project-side `Health`, который висит на player в `Level` и доступен через `NeoFPS_PlayerAdapter`.
- `PlayerHealthHudPresenter` получает текущего player через `SoloPlayerCharacterEventWatcher`, подписывается на `Health.OnHealthChanged` и обновляет `Slider.value` / `Slider.maxValue` без ручных cross-scene ссылок.
- Шкала размещена прямо в `Main` как обычный scene object `PlayerHealthBar_PC` под `Canvas (Screen Space Overlay)`, поэтому её позиция, размер и anchors настраиваются через `RectTransform` в сцене, а не кодом.
- Crosshair читается через `ICrosshairDriver` у текущего selected weapon, поэтому автоматически обновляется при смене оружия, ADS и holster.
- В текущей конфигурации NeoFPS константа `FpsCrosshair` содержит только `Default` и `None`, поэтому pistol и shotgun сейчас используют один и тот же базовый hip-fire crosshair. При ADS он скрывается штатно через `None`.

## 4. Файлы

- Installer: `Assets/_Project/Scripts/UI/HUD/NeoFPS_MinimalHudInstaller.cs`
- Health HUD presenter: `Assets/_Project/Scripts/UI/HUD/PlayerHealthHudPresenter.cs`
- Rescue HUD presenter: `Assets/_Project/Scripts/UI/HUD/RescueHudPresenter.cs`
- Project crosshair HUD: `Assets/_Project/Prefabs/UI/HUD/Crosshair_PC.prefab`
- Project quickslot HUD: `Assets/_Project/Prefabs/UI/HUD/InventoryStandard_PC.prefab`
- Project ammo HUD: `Assets/_Project/Prefabs/UI/HUD/AmmoCounter_PC.prefab`
- Project health bar prefab base: `Assets/_Project/Prefabs/UI/HUD/Health/Loading Bar Green Dark.prefab`
- Project health bar sprites: `Assets/_Project/Art/UI/HUD/Health/loading_BarDark.png`, `Assets/_Project/Art/UI/HUD/Health/loading_BarFillGreen.png`

## 5. Где настраивается crosshair оружия

- Для modular firearm crosshair задаётся на aimer-модуле оружия через `m_CrosshairDown` и `m_CrosshairUp`.
- Для текущих quickswitch-оружий:
  - `Firearm_Pistol_Quickswitch.prefab`: hip-fire = `Default`, ADS = `None`
  - `Firearm_Shotgun_Quickswitch.prefab`: hip-fire = `Default`, ADS = `None`
- Если позже понадобятся разные формы прицела для разных оружий, это делается не новым HUD-кодом, а через расширение набора `FpsCrosshair` и настройку соответствующих weapon prefab.

## 6. Архитектурный принцип

- Данные оружия и патронов берутся только из NeoFPS.
- Данные по спасению жителей берутся из project-side `LevelRescueController`, который живёт в `Level` и автоматически собирает `RescueObjective`.
- Язык HUD берётся из `PluginYourGames` через `YG2.lang`: `ru` показывает русский текст, остальные языки используют английский fallback.
- Представление разделяется по платформе:
  - `HUD_PC` для desktop / web keyboard+mouse
  - `HUD_Mobile` для будущего mobile HUD и mobile controls
- Текущая реализация покрывает только PC HUD.
- Mobile HUD будет следующим отдельным слоем и не должен менять source of truth в NeoFPS.

## 7. Что проверить руками

- При запуске игры на экране виден crosshair для pistol.
- В HUD видны патроны в магазине и в резерве.
- Quickslot HUD показывает выбранное оружие.
- При подборе ammo обновляется `HudAmmoCounter`.
- При подборе нового оружия обновляется `HudInventoryStandardPC`.
- При смене оружия crosshair обновляется штатно.
- При ADS crosshair скрывается штатно.
- Если на уровне есть жители с `RescueObjective`, HUD показывает локализованный текст `Спасено / Rescued: X / Y` и обновляется при `Rescued`.
- После смерти и рестарта уровня HUD повторно находит нового player character.
- `DeathScreen` перекрывает HUD корректно.
