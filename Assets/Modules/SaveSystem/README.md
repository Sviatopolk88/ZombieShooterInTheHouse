# Module: SaveSystem

## Назначение
Сериализация и хранение произвольных DTO по ключу через единый фасад `SaveService`, без знания о конкретных gameplay-данных.

## Расположение
`Assets/Modules/SaveSystem`

## Статус переиспользования
Reusable with cleanup

## Основные классы
- `SaveService` — singleton-фасад сохранений.
- `ISaveProvider` — контракт backend-а хранения.
- `YandexSaveProvider` — реализация поверх generic key/value storage в `YG2.saves`.
- `SaveProjectSettings`, `SaveEnums` — конфигурация provider-а.
- `YandexSaveStorageData` — расширение partial `SavesYG` для provider storage.

## Публичные точки входа
- `SaveService.Instance`
- `Warmup()`
- `HasKey(string)`
- `SaveRaw(string, string)`
- `TryLoadRaw(string, out string)`
- `Save<T>(string, T)`
- `TryLoad<T>(string, out T)`
- `Delete(string)`

## Зависимости
- Unity runtime / `JsonUtility`
- `PluginYG2` / namespace `YG` для `YandexSaveProvider`
- От project DTO и `_Project` модуль не зависит

## Как подключить в новый проект
1. Скопировать папку `Assets/Modules/SaveSystem`.
2. Оставить или заменить provider в `CreateProvider`.
3. В новом проекте сделать отдельный orchestration-слой, который собирает и применяет игровые DTO через `SaveService`.

## Что важно не сломать
- `SaveSystem` должен работать только с serializable DTO и не знать о сценах, инвентаре или player prefab.
- `TryLoad<T>` ожидает корректный `JsonUtility`-совместимый класс.
- `YandexSaveProvider` использует generic key/value storage в `YG2.saves`, а не project-specific поля.

## Риски переноса
- Средний риск.
- Ядро переносимо, но текущий provider жёстко завязан на `PluginYG2` и lifecycle `YG2.isSDKEnabled`.

## Рекомендации для Bake or Die
Использовать как основу save-ядра, если MVP нужен persistence layer. Для другого storefront/provider достаточно заменить `ISaveProvider`.
