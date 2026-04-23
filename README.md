# ZombieShooterInTheHouse

Проект на Unity 6.3 для WebGL / Яндекс Игр. Это FPS-прототип на базе NeoFPS, в котором проектная логика отделена от reusable-модулей и внешних SDK.

Главная архитектурная идея проекта:

- `Assets/Modules` содержит переиспользуемые модули и integration-core
- `Assets/_Project` содержит интеграционный слой конкретной игры
- vendor / third-party код не смешивается с gameplay-кодом проекта
- новые игровые фичи по возможности собираются как отдельные reusable-модули + project-side glue

## Общее Устройство

Проект делится на 4 больших слоя:

1. Third-party / vendor
   - `Assets/NeoFPS`
   - `Assets/PluginYourGames`
   - `Assets/TextMesh Pro`
   - UI/арт-паки в `Assets/Modern Shooting UI Pack`, `Assets/UI Kit Pro - Casual Glossy`
2. Reusable modules
   - `Assets/Modules/*`
3. Project integration layer
   - `Assets/_Project/*`
4. Контент и настройки
   - `Assets/Art`, `Assets/Audio`, `Assets/Settings`, `Assets/WebGLTemplates`, `Assets/Resources`

Практически это означает следующее:

- базовые системы не должны знать о конкретной сцене, конкретном оружии, конкретном UI
- `_Project` знает про текущий FPS-прототип, сцены, награды, покупки, HUD и flow уровня
- внешние SDK изолируются через provider/service-слой

## Структура Репозитория

Ключевые папки:

- `Assets/Modules`
  reusable-код проекта
- `Assets/_Project`
  код и ресурсы конкретной игры
- `Assets/NeoFPS`
  vendor FPS-framework
- `Assets/PluginYourGames`
  PluginYG2 / YG2 для Яндекс Игр
- `Assets/Editor`
  editor-only утилиты проекта
- `Assets/Settings`
  URP / quality / render settings
- `ProjectSettings`
  стандартные настройки Unity-проекта

## Scene Flow

Проект строится вокруг схемы:

- `_Bootstrap`
- `_Main`
- `Level_*`

Смысл схемы:

- `_Bootstrap` делает ранний запуск систем и загружает остальные сцены
- `_Main` живёт постоянно и содержит долгоживущие системы, игрока, камеру, UI
- `Level_*` грузится additively поверх `_Main` и может независимо меняться / перезагружаться

Основные scene-flow классы:

- `Assets/_Project/Scripts/Systems/SceneFlow/BootstrapSceneStartup.cs`
  запускает initial load, прогревает purchases/save, вызывает interstitial на старте
- `Assets/_Project/Scripts/Systems/SceneFlow/LevelReloadService.cs`
  перезагружает текущий уровень и вызывает interstitial на scene transition
- `Assets/_Project/Scripts/Systems/SceneFlow/ProjectPlatformLifecycle.cs`
  сообщает Yandex Games через `YG2.GameReadyAPI()`, что уровень готов
- `Assets/_Project/Scripts/Systems/SceneFlow/ProjectSceneNames.cs`
  хранит project-side scene constants

Важно:

- код проекта ориентируется на additive-модель `_Main + Level`
- уровень не должен быть местом для глобальных singleton-сервисов
- глобальные runtime-сервисы либо живут в `_Main`, либо создаются отдельно через `RuntimeInitializeOnLoadMethod`

Текущее состояние build settings:

- в сборке находятся ключевые сцены проекта:
  - `Assets/_Project/Scenes/_Bootstrap.unity`
  - `Assets/_Project/Scenes/_Main.unity`
  - `Assets/_Project/Scenes/Level_1.unity`
- scene-flow проекта ориентирован именно на связку `_Bootstrap -> _Main -> Level_1`
- перед финальной сборкой важно держать build settings синхронизированными с этим flow

## Reusable Modules

### 1. `SceneLoader`

Путь:

- `Assets/Modules/SceneLoader`

Назначение:

- тонкая reusable-обёртка над `SceneManager`
- умеет `LoadScene`, `LoadAdditive`, `UnloadScene`, `ReloadActiveScene`, `SetActiveScene`, `IsSceneLoaded`

Что важно:

- не знает про `_Project`
- не знает про конкретные scene names
- используется как infrastructure utility

### 2. `HealthSystem`

Путь:

- `Assets/Modules/HealthSystem`

Назначение:

- автономная система HP, урона, лечения, смерти

Ключевые сущности:

- `Health`
- `IHealth`
- `IDamageable`
- `IHealable`
- `HealthDamageableAdapter`

Роль в проекте:

- общий источник истины по здоровью игрока, врагов и rescue-target логики
- поверх неё строятся player death, enemy death, heal reward и другие project-side сценарии

### 3. `DamageSystem`

Путь:

- `Assets/Modules/DamageSystem`

Назначение:

- единый способ передавать урон по контракту `IDamageable`

Роль:

- decoupling между источником урона и конкретной реализацией здоровья
- используется как мост между NeoFPS hit/explosion pipeline и `HealthSystem`

### 4. `EnemyAI_Base`

Путь:

- `Assets/Modules/EnemyAI_Base`

Назначение:

- reusable базовая логика поведения врага на `NavMeshAgent`

Состав:

- `EnemyAI_Base`
- `EnemyMovement`
- `EnemyVision`
- `EnemyAttack`
- `EnemyAnimationController`
- `EnemyTargetRegistry`
- `EnemyTargetRegistryMember`

Роль:

- модуль даёт общий AI-каркас
- `_Project` определяет конкретные archetype-prefab, параметры, анимации, приоритеты целей и surface/headshot-настройки

### 5. `EnemySpawner`

Путь:

- `Assets/Modules/EnemySpawner`

Назначение:

- простой reusable-spawner врагов

Роль:

- занимается только созданием врагов
- не управляет их жизненным циклом после спавна

### 6. `RescueObjective`

Путь:

- `Assets/Modules/RescueObjective`

Назначение:

- reusable-логика rescue-target, с которой потом работает project-side level logic

Роль:

- базовый статус rescue-сущности
- `_Project` добавляет HUD, fail/success flow и интеграцию с LevelExit

### 7. `DoorBreachEncounter`

Путь:

- `Assets/Modules/DoorBreachEncounter`

Назначение:

- scripted reusable encounter для выбивания двери

Роль:

- не универсальная система дверей, а готовый encounter-модуль
- настраивается scene references в конкретном уровне

### 8. `NeoFPS_Adapter`

Путь:

- `Assets/Modules/NeoFPS_Adapter`

Назначение:

- project-owned bridge между vendor NeoFPS и остальной архитектурой

Основные задачи:

- мост от оружейных попаданий NeoFPS к `DamageSystem`
- bridge для player health / death
- bridge для взрывов, гранат, ammo effects
- настройка стартового loadout игрока через `NeoFPS_PlayerLoadoutAdapter`
- project-specific расширения без правки vendor-кода NeoFPS

Это один из ключевых модулей проекта, потому что он отделяет ваш код от прямой правки NeoFPS.

### 9. `AdsCore`

Путь:

- `Assets/Modules/AdsCore`

Назначение:

- reusable ads-core для проектов на PluginYG2

Структура:

- `Core/AdsService.cs`
- `Core/IAdsProvider.cs`
- `Core/AdsEnums.cs`
- `Core/AdsShowResult.cs`
- `Core/AdsProjectSettings.cs`
- `Providers/Yandex/YandexAdsProvider.cs`

Роль:

- `AdsService` является фасадом показа рекламы
- `IAdsProvider` абстрагирует backend рекламы
- `YandexAdsProvider` адаптирует PluginYG2 / YG2

Что принципиально:

- ads-core не знает про здоровье, патроны, игрока, NeoFPS
- gameplay/UI не вызывает YG2 напрямую

### 10. `PurchasesCore`

Путь:

- `Assets/Modules/PurchasesCore`

Назначение:

- reusable purchase-core для проектов на PluginYG2

Структура:

- `Core/PurchaseService.cs`
- `Core/IPurchaseProvider.cs`
- `Core/PurchaseResult.cs`
- `Core/PurchaseProductInfo.cs`
- `Core/PurchaseEnums.cs`
- `Core/PurchaseProjectSettings.cs`
- `Providers/Yandex/YandexPurchaseProvider.cs`

Роль:

- фасад и orchestration покупки
- каталог товаров и purchase result
- адаптация PluginYG2 purchase API в единый контракт

Что принципиально:

- purchase-core не знает про оружие, инвентарь и gameplay-награды

### 11. `SaveSystem`

Путь:

- `Assets/Modules/SaveSystem`

Назначение:

- reusable save-core

Структура:

- `Core/ISaveProvider.cs`
- `Core/SaveService.cs`
- `Core/SaveEnums.cs`
- `Core/SaveProjectSettings.cs`
- `Providers/Yandex/YandexSaveProvider.cs`
- `Providers/Yandex/YandexSaveStorageData.cs`

Роль:

- generic сохранение/загрузка JSON по ключу
- провайдер storage для Yandex / PluginYG2

Что принципиально:

- save-core не знает про текущий уровень, оружие, NeoFPS inventory
- все gameplay DTO и логика применения save остаются в `_Project`

## Project Layer: `Assets/_Project`

`_Project` — это integration layer конкретной игры. Здесь лежат:

- gameplay flow
- scene-specific logic
- UI и HUD
- локализация project-строк
- интеграция reusable-модулей между собой
- конкретные prefab, resources и scriptable objects игры

Ключевые подпапки:

- `Scenes`
- `Scripts`
- `Prefabs`
- `Resources`
- `README`
- `ScriptableObjects`
- `UI`
- `Tests`

## Основные Project-Side Подсистемы

### 1. Game Flow

Путь:

- `Assets/_Project/Scripts/GameFlow`

Ключевые классы:

- `GameFlowService`
- `CursorStateService`

Роль:

- state machine уровня на верхнем уровне
- game over / restart
- управление режимом курсора и переходом между gameplay/UI state

### 2. Enemy Integration

Путь:

- `Assets/_Project/Scripts/Gameplay/Enemy`

Ключевые классы:

- `EnemyTracker`
- `EnemyDeathNotifier`
- `EnemyDamageMessageHandler`
- `EnemyFleshSurface`

Роль:

- project-side счёт и презентация врагов
- surface/headshot integration
- помощь level/gameplay flow вокруг reusable enemy modules

### 3. Rescue Flow

Путь:

- `Assets/_Project/Scripts/Gameplay/Rescue`
- `Assets/_Project/Scripts/Gameplay/RescueTargets`

Ключевые классы:

- `LevelRescueController`
- `LevelRescueControllerBootstrap`
- bridges для animator / fade / rescue target presentation

Роль:

- управление rescue objective на уровне
- сбор rescue targets
- статистика для HUD и выхода с уровня

### 4. Level Exit / Level Completion

Путь:

- `Assets/_Project/Scripts/Gameplay/LevelExit`
- `Assets/_Project/Scripts/UI/LevelComplete`
- `Assets/_Project/Scripts/UI`

Ключевые классы:

- `LevelExitController`
- `LevelExitTriggerZone`
- `LevelExitConfirmationController`
- `LevelCompleteScreenController`
- `VictoryScreenController`

Роль:

- блокировка выхода до выполнения условий
- подтверждение досрочного выхода
- экран завершения уровня

### 5. Player Flow

Путь:

- `Assets/_Project/Scripts/Gameplay/Player`

Ключевые классы:

- `PlayerDeathHandler`

Роль:

- project-side реакция на смерть игрока
- интеграция с `GameFlowService` и UI

### 6. HUD / UI

Путь:

- `Assets/_Project/Scripts/UI`
- `Assets/_Project/Scripts/UI/HUD`

Ключевые классы:

- `NeoFPS_MinimalHudInstaller`
- `PlayerHealthHudPresenter`
- `RescueHudPresenter`
- `DeathScreenController`

Роль:

- project-side установка HUD поверх NeoFPS
- presenter-слой между gameplay state и UI
- без прямой зависимости UI на vendor internals, где это возможно

### 7. Input

Путь:

- `Assets/_Project/Scripts/Input`

Ключевой класс:

- `CursorToggleInput`

Роль:

- project-side override для переключения режима курсора
- основной input gameplay по-прежнему идёт из NeoFPS

### 8. Ads Integration

Путь:

- `Assets/_Project/Scripts/Ads`

Ключевые классы:

- `ProjectAdsRewardService`
- `AdsRewardZone`
- `Rewards/AdsRewardApplier`

Роль:

- project-side проверка доступности награды
- выдача награды за rewarded
- вызов rewarded через support zone

Граница ответственности:

- `Modules.AdsCore` показывает рекламу
- `_Project` решает, что делать после успешного rewarded

### 9. Purchases Integration

Путь:

- `Assets/_Project/Scripts/Purchases`

Ключевые классы:

- `ProjectPurchaseService`
- `PurchaseRewardApplier`
- `PurchaseOfferZone`
- `ProjectPurchasesEntitlementSync`
- `ProjectPurchasesSaveData`

Роль:

- project-side mapping `productId -> gameplay reward`
- текущая MVP-покупка оружия
- восстановление entitlement из `YG2.saves`

Граница ответственности:

- `Modules.PurchasesCore` делает purchase orchestration
- `_Project` выдаёт дробовик и решает, как UI/зона инициирует покупку

### 10. Save Integration

Путь:

- `Assets/_Project/Scripts/Save`

Ключевые классы:

- `GameSaveController`
- `GameSaveData`
- `SaveDataCollector`
- `SaveDataApplier`
- `GameSaveWeaponCatalog`

Роль:

- сбор gameplay-state
- применение save обратно в игру
- автозагрузка / автосохранение

Граница ответственности:

- `Modules.SaveSystem` хранит JSON
- `_Project` знает, как преобразовать игру в DTO и обратно

## Внешние Плагины И SDK

### 1. NeoFPS

Путь:

- `Assets/NeoFPS`

Роль:

- vendor FPS-framework
- character controller
- inventory
- firearms
- ammo
- interaction
- HUD foundations
- input bindings

Правило проекта:

- vendor-code NeoFPS не меняется
- любые project-изменения делаются через адаптеры, presenter-слой и project-prefab configuration

### 2. PluginYG2 / PluginYourGames

Путь:

- `Assets/PluginYourGames`

Основной namespace / entry point:

- `YG`
- `YG2`

Роль:

- интеграция с Яндекс Играми
- SDK init
- interstitial / rewarded ads
- in-app purchases
- storage / save data
- language / platform environment
- lifecycle callbacks

Как он связан с проектом:

- напрямую YG2 должен знать только provider/integration-слой
- gameplay/UI код проекта не должен зависеть от YG2 напрямую
- ads/purchases/save вынесены в reusable core + project-side integration

### 3. TextMesh Pro

Путь:

- `Assets/TextMesh Pro`

Роль:

- базовый текстовый UI-рендеринг
- prompt labels, HUD texts, purchase/ads zones и прочие UI-тексты

### 4. UI / Art Packs

Путь:

- `Assets/Modern Shooting UI Pack`
- `Assets/UI Kit Pro - Casual Glossy`

Роль:

- ассетная база для UI и визуальных элементов
- итоговая project-side композиция UI лежит в `_Project`

## Как Связаны Ads / Purchases / Saves

В проекте уже выстроена единая схема для platform integrations:

1. Reusable core
   - живёт в `Assets/Modules/*Core`
   - знает только про orchestration и provider API
2. Provider
   - адаптирует внешний SDK
   - в текущем проекте это Yandex / PluginYG2 providers
3. Project-side integration
   - живёт в `Assets/_Project/Scripts/*`
   - знает про оружие, здоровье, UI, инвентарь, текущий UX

Это даёт важный результат:

- gameplay-код не зависит от low-level SDK
- другой проект можно быстрее поднять на тех же `AdsCore`, `PurchasesCore`, `SaveSystem`
- project-specific reward / entitlement / DTO остаются локальными для конкретной игры

## Реклама

Reusable слой:

- `Modules.AdsCore.AdsService`
- `Modules.AdsCore.IAdsProvider`
- `Modules.AdsCore.YandexAdsProvider`

Project-side слой:

- `ProjectAdsRewardService`
- `AdsRewardApplier`
- `AdsRewardZone`

Текущее поведение:

- interstitial вызывается на старте и при scene transition
- rewarded вызывается через zone-based interaction
- награды:
  - `Heal`
  - `Ammo9mm`

Архитектурный принцип:

- rewarded flow замыкается через `AdsService`
- выдача награды живёт только в `_Project`

## Внутриигровые Покупки

Reusable слой:

- `Modules.PurchasesCore.PurchaseService`
- `Modules.PurchasesCore.IPurchaseProvider`
- `Modules.PurchasesCore.YandexPurchaseProvider`

Project-side слой:

- `ProjectPurchaseService`
- `PurchaseRewardApplier`
- `PurchaseOfferZone`

Текущее MVP:

- покупка дробовика
- entitlement хранится project-side в расширении `YG2.saves`

Архитектурный принцип:

- purchase-core не знает, что такое shotgun
- `_Project` решает, что даёт `productId`

## Сохранения

Reusable слой:

- `Modules.SaveSystem.SaveService`
- `Modules.SaveSystem.ISaveProvider`
- `Modules.SaveSystem.YandexSaveProvider`

Project-side слой:

- `GameSaveController`
- `SaveDataCollector`
- `SaveDataApplier`
- `GameSaveData`

Сейчас сохраняется:

- текущий уровень
- список оружия
- `ammo9mm`

Архитектурный принцип:

- reusable save-core хранит JSON по ключу
- `_Project` решает, какие именно данные превращать в save

## Локализация

Текущий источник истины по языку:

- `YG2.lang`

Project-side локализация:

- минимальный bridge над PluginYG2
- локализуются project UI-строки, HUD и часть экранов

Подход:

- RU/EN определяется через `YG2.lang`
- на смену языка UI реагирует через `YG2.onSwitchLang`

## Input

Текущий проект в основном использует input NeoFPS.

Это значит:

- базовые movement/fire/aim/reload/weapon actions идут из NeoFPS bindings
- project-side поверх этого добавлены отдельные локальные точки вроде `CursorToggleInput`
- любые новые input-overrides желательно оформлять project-side, не ломая vendor input pipeline

## Важные Проектные Принципы

1. Не менять NeoFPS vendor-code.
2. Не вызывать YG2 напрямую из gameplay/UI, если уже есть service/provider слой.
3. Новые gameplay-фичи по возможности оформлять как reusable module + project integration.
4. `_Project` — место для конкретной game logic, а не для копирования vendor / SDK кода.
5. Reusable core должен зависеть только вниз, но не обратно на `_Project`.

## Где Искать Документацию По Подсистемам

Project-side README:

- `Assets/_Project/README/README_Ads.md`
- `Assets/_Project/README/README_Purchases.md`
- `Assets/_Project/README/README_SaveSystem.md`
- `Assets/_Project/README/README_Enemies.md`
- `Assets/_Project/README/README_Rescue.md`
- `Assets/_Project/README/README_HUD.md`
- `Assets/_Project/README/README_Input.md`
- `Assets/_Project/README/README_Localization.md`

Module README:

- `Assets/Modules/SceneLoader/README.md`
- `Assets/Modules/HealthSystem/README.md`
- `Assets/Modules/DamageSystem/README.md`
- `Assets/Modules/EnemyAI_Base/README.md`
- `Assets/Modules/EnemySpawner/README.md`
- `Assets/Modules/RescueObjective/README.md`
- `Assets/Modules/NeoFPS_Adapter/README.md`
- `Assets/Modules/DoorBreachEncounter/README.md`

## Как Расширять Проект

### Добавить новую рекламу / новый ads provider

1. Реализовать новый provider по `IAdsProvider`
2. Подключить его в `AdsService`
3. Не менять gameplay/UI-код, который уже работает через `AdsService` или project bridge

### Добавить новый purchase product

1. Добавить товар в Yandex / PluginYG2 catalog
2. Добавить project-side mapping в `PurchaseRewardApplier`
3. При необходимости расширить project entitlement save
4. Добавить новую purchase zone / UI точку вызова

### Добавить новые save-поля

1. Расширить `GameSaveData`
2. Дополнить `SaveDataCollector`
3. Дополнить `SaveDataApplier`
4. Не менять reusable save-core без необходимости

### Добавить новый gameplay-модуль

Рекомендуемый путь:

1. Если логика переносима, положить её в `Assets/Modules/NewModule`
2. Если логика проектная, положить integration в `Assets/_Project/Scripts/...`
3. Избегать смешивания scene-specific кода с reusable API

## Что Проверять После Больших Изменений

- bootstrap корректно грузит `_Main` и нужный `Level_*`
- player/HUD живут в `_Main` и переживают смену уровня
- interstitial/rewarded/purchases не вызываются напрямую через YG2 из gameplay-кода
- save корректно восстанавливает inventory/state после reload
- project-side UI корректно переживает reload/additive load
- новые модули не тянут зависимости обратно из `Modules` в `_Project`

## Короткая Карта Зависимостей

Верхнеуровневая схема такая:

- `PluginYG2` -> provider-слои `AdsCore` / `PurchasesCore` / `SaveSystem`
- `NeoFPS` -> `NeoFPS_Adapter`
- reusable modules -> project-side orchestration
- `_Project` связывает:
  - scene flow
  - gameplay systems
  - HUD/UI
  - platform integrations

Если упростить до одной фразы:

проект строится как FPS-прототип на NeoFPS, где reusable gameplay/modules и reusable Yandex integrations отделены от конкретной game logic слоя `_Project`.
