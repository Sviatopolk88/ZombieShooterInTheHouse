# Module: AdsCore

## Назначение
Ядро показа рекламы: interstitial, rewarded, выбор provider-а и нормализация результата показа без знания о gameplay-наградах проекта.

## Расположение
`Assets/Modules/AdsCore`

## Статус переиспользования
Reusable with cleanup

## Основные классы
- `AdsService` — singleton-фасад показа рекламы.
- `IAdsProvider` — контракт provider-а.
- `YandexAdsProvider` — текущая реализация поверх `PluginYG2` / `YG2`.
- `AdsProjectSettings` — provider и конфигурация reward amount.
- `AdsEnums`, `AdsShowResult` — типы API.

## Публичные точки входа
- `AdsService.Instance`
- `Warmup()`
- `TryShowInterstitial(...)`
- `CanRequestReward(...)`
- `TryShowRewarded(...)`
- `GetConfiguredRewardAmount(...)`
- Интерфейс `IAdsProvider`

## Зависимости
- Unity runtime
- `PluginYG2` / namespace `YG` для `YandexAdsProvider`
- От `_Project` и gameplay-кода модуль не зависит

## Как подключить в новый проект
1. Скопировать папку `Assets/Modules/AdsCore`.
2. Оставить или заменить provider-реализацию в `CreateProvider`.
3. Поверх `AdsService` реализовать project-side слой, который решает, какую награду выдавать после успешного rewarded.

## Что важно не сломать
- `AdsCore` не должен знать о `Health`, `Inventory`, UI или конкретных наградах проекта.
- `AdsService` рассчитывает, что provider завершит callback даже при ошибке/отклонении.
- Сейчас reward ids и reward amounts жёстко привязаны к enum `AdsRewardType`.

## Риски переноса
- Средний риск.
- Сам фасад переносим, но `YandexAdsProvider` и enum наград привязаны к текущей web/Yandex-схеме.

## Рекомендации для Bake or Die
Использовать только если Bake or Die выходит на WebGL/Yandex Games и действительно требует ads. Перед переносом лучше вынести reward-конфиг и provider selection из жёстко прошитых enum.
