# Ads Module

## AdsRewardZone Usage Modes

`AdsRewardZone` now supports three usage modes:

- `ByRewardType`
- `LimitedUses`
- `ResourceBased`

Recommended setup:

- Heal zone:
  - use `LimitedUses` when the station must be finite
  - `maxUses` keeps working as before
- Ammo zone:
  - use `ResourceBased`
  - the zone is not depleted by `remainingUses`
  - availability comes from current ammo state via `ProjectAdsRewardService` and `AdsRewardApplier`

`ByRewardType` is the safe default for already placed zones:

- `Heal` resolves to `LimitedUses`
- `Ammo9mm` resolves to `ResourceBased`

This prevents a soft-lock where the player already used the only ammo reward, spent all pistol ammo again, and could no longer restore the resource through ads.

## Граница Reusable / Project-Specific

Reusable ads-core:

- `Assets/Modules/AdsCore/Core/AdsService.cs`
- `Assets/Modules/AdsCore/Core/IAdsProvider.cs`
- `Assets/Modules/AdsCore/Core/AdsShowResult.cs`
- `Assets/Modules/AdsCore/Core/AdsEnums.cs`
- `Assets/Modules/AdsCore/Core/AdsProjectSettings.cs`
- `Assets/Modules/AdsCore/Providers/Yandex/YandexAdsProvider.cs`

Project-side integration:

- `Assets/_Project/Scripts/Ads/Rewards/AdsRewardApplier.cs`
- `Assets/_Project/Scripts/Ads/ProjectAdsRewardService.cs`
- `Assets/_Project/Scripts/Ads/AdsRewardZone.cs`
- project flow / scene integration points

Правильная граница теперь такая:

- ads-core показывает рекламу и возвращает `AdsShowResult`
- `_Project` решает, можно ли выдать награду и как именно её применить в gameplay

## Reusable AdsCore

Namespace reusable слоя:

- `Modules.AdsCore`

`AdsService` больше не знает:

- про игрока
- про здоровье
- про патроны
- про NeoFPS inventory

Он отвечает только за:

- interstitial
- rewarded
- provider orchestration
- результат рекламного показа

`YandexAdsProvider` остаётся reusable provider для PluginYG2 / YG2.

## Project-Side Reward Integration

В `_Project` reward logic остаётся отдельно:

- `AdsRewardApplier` содержит gameplay reward logic
- `ProjectAdsRewardService` связывает rewarded-result из ads-core с project-side наградой
- `AdsRewardZone` работает через `ProjectAdsRewardService`, а не через low-level provider API

Текущие project-side награды:

- `Heal`
- `Ammo9mm`

## Interstitial И GameReady

Interstitial по-прежнему вызывается через reusable `Modules.AdsCore.AdsService`:

- `BootstrapSceneStartup`
- `LevelReloadService`

`YG2.GameReadyAPI()` по-прежнему вызывается project-side через:

- `Assets/_Project/Scripts/Systems/SceneFlow/ProjectPlatformLifecycle.cs`

## Зависимости

Направление зависимостей теперь такое:

- `_Project` зависит от `Modules.AdsCore`
- `Modules.AdsCore` не зависит от `_Project`

Отдельный asmdef для `AdsCore` не добавлялся осознанно:

- `PluginYG2` в текущем проекте живёт без собственного asmdef
- отдельный asmdef для ads-core создал бы некорректную зависимость на default assembly

Поэтому разделение сделано через:

- папки
- namespace
- отсутствие обратных ссылок из ads-core в `_Project`

## Как Подключить В Другой Проект

Для другого проекта на PluginYG2 можно переносить:

- `Assets/Modules/AdsCore/`

Новый проект должен отдельно реализовать:

- свою reward logic
- свой bridge над rewarded-result
- свои UI / zones / flow points

## Что Проверить Руками

1. Interstitial работает на старте игры.
2. Interstitial работает после `RestartLevel`.
3. Rewarded работает для лечения.
4. Rewarded работает для `Ammo9mm`.
5. `AdsRewardZone` выдаёт награду только после успешного rewarded.
6. `YG2.GameReadyAPI()` вызывается после загрузки `Level_1`.
7. `Modules.AdsCore` не содержит ссылок на `HealthSystem`, `NeoFPS`, `IInventory`.
8. `Level_2` не изменялся.
