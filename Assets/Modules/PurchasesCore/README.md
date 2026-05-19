# Module: PurchasesCore

## Назначение
Ядро purchase-flow: проверка доступности товара, запуск покупки через provider и возврат нормализованного `PurchaseResult`.

## Расположение
`Assets/Modules/PurchasesCore`

## Статус переиспользования
Reusable with cleanup

## Основные классы
- `PurchaseService` — singleton-фасад покупок.
- `IPurchaseProvider` — контракт provider-а.
- `YandexPurchaseProvider` — реализация поверх `PluginYG2` purchases API.
- `PurchaseProductInfo`, `PurchaseResult`, `PurchaseEnums`, `PurchaseProjectSettings` — типы и конфигурация.

## Публичные точки входа
- `PurchaseService.Instance`
- `Warmup()`
- `GetProducts()`
- `TryGetProduct(string, out PurchaseProductInfo)`
- `CanPurchase(string, out string)`
- `TryPurchase(string, Action<PurchaseResult>)`

## Зависимости
- Unity runtime
- `PluginYG2` / namespace `YG`
- От project-side entitlement и runtime reward logic модуль не зависит

## Как подключить в новый проект
1. Скопировать папку `Assets/Modules/PurchasesCore`.
2. Подключить или заменить provider в `CreateProvider`.
3. Сделать отдельный project-side слой для entitlement storage и выдачи купленного контента в gameplay.

## Что важно не сломать
- `PurchasesCore` не должен знать, как выдать оружие, валюту или entitlement в конкретной игре.
- Каталог товаров приходит от provider-а; сам модуль не хранит проектный product catalog.
- Сейчас нет отдельного статуса отмены от `PluginYG2`, поэтому provider интерпретирует часть ошибок как `Failed`.

## Риски переноса
- Средний риск.
- Архитектурно модуль переносим, но текущая реализация почти полностью опирается на Yandex storefront и `YG2` callbacks.

## Рекомендации для Bake or Die
Использовать только если MVP реально требует in-app purchases и если storefront близок к текущему. Для чистого боевого MVP без монетизации модуль можно не переносить.
