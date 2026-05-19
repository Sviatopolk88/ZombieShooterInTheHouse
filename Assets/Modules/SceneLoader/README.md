# Module: SceneLoader

## Назначение
Синхронный utility для загрузки, выгрузки, перезагрузки и активации сцен.

## Расположение
`Assets/Modules/SceneLoader`

## Статус переиспользования
Reusable

## Основные классы
- `SceneLoader` — статический helper над `SceneManager`.

## Публичные точки входа
- `LoadScene(string)`
- `LoadAdditive(string)`
- `UnloadScene(string)`
- `ReloadActiveScene()`
- `SetActiveScene(string)`
- `IsSceneLoaded(string)`

## Зависимости
- Unity `SceneManagement`

## Как подключить в новый проект
1. Скопировать папку вместе с `SceneLoader.asmdef`.
2. Добавить вызывающий bootstrap/flow-код в новом проекте.
3. Использовать `LoadAdditive` и `SetActiveScene` там, где нужен multi-scene flow.

## Что важно не сломать
- API синхронный: модуль не возвращает `AsyncOperation`.
- `LoadAdditive` не грузит уже загруженную сцену.
- `SetActiveScene` работает только для уже загруженных сцен.

## Риски переноса
- Низкий риск.
- Если новому проекту нужен полностью асинхронный flow, этот модуль придётся оборачивать дополнительным orchestration-слоем.

## Рекомендации для Bake or Die
Использовать для `scene flow / bootstrap`, если MVP останется на additive-схеме сцен.
