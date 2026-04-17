# SceneLoader

## Назначение

Простой utility-модуль для загрузки, выгрузки и переключения сцен.
Подходит для схемы `Bootstrap -> Main -> Level`, но не знает про конкретные сцены проекта.

## Публичный API

- `LoadScene(string sceneName)`
- `LoadAdditive(string sceneName)`
- `UnloadScene(string sceneName)`
- `ReloadActiveScene()`
- `SetActiveScene(string sceneName)`
- `IsSceneLoaded(string sceneName)`

## Пример использования

```csharp
using Modules.SceneLoader;

public static class ExampleSceneFlow
{
    public static void LoadLevel()
    {
        SceneLoader.LoadAdditive("Level_01");
    }

    public static void UnloadLevel()
    {
        SceneLoader.UnloadScene("Level_01");
    }

    public static void ReloadCurrent()
    {
        SceneLoader.ReloadActiveScene();
    }
}
```
