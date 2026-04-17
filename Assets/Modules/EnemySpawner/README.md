# EnemySpawner

## Назначение

Простой спавнер врагов для прототипа.
Создает заданное количество врагов в случайных точках спавна и не управляет ими после создания.

## Публичный API

Поля компонента `EnemySpawner`:

- `spawnOnStart`
- `enemyPrefab`
- `spawnPoints`
- `parent`
- `enemyCount`

Методы:

- `Spawn()`

## Пример использования

```csharp
using Modules.EnemySpawner;
using UnityEngine;

public class ExampleSpawnerSetup : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;

    private void Start()
    {
        enemySpawner.Spawn();
    }
}
```

## Примечания

- Спавн может использовать одну и ту же точку несколько раз.
- Модуль не управляет жизненным циклом врагов.
- Автоспавн можно отключить через `spawnOnStart`.
