# Module: EnemySpawner

## Назначение
Простой спавнер, который создаёт заданное число экземпляров prefab в случайных точках.

## Расположение
`Assets/Modules/EnemySpawner`

## Статус переиспользования
Reusable with cleanup

## Основные классы
- `EnemySpawner` — MonoBehaviour для одномоментного спавна `enemyCount` объектов.

## Публичные точки входа
- Компонент `EnemySpawner`.
- Метод `Spawn()`.
- Inspector-поля `enemyPrefab`, `spawnPoints`, `parent`, `enemyCount`, `spawnOnStart`.

## Зависимости
- Unity runtime (`Instantiate`, `Transform`, `Random`).

## Как подключить в новый проект
1. Скопировать папку вместе с `EnemySpawner.asmdef`.
2. Поставить компонент на scene object.
3. Назначить prefab и массив `spawnPoints`, затем вызывать `Spawn()` вручную или через `spawnOnStart`.

## Что важно не сломать
- Спавнер не управляет жизненным циклом врагов после создания.
- Одна и та же точка может использоваться несколько раз.
- Здесь нет волн, таймеров, лимитов по occupancy, pooling или respawn-логики.

## Риски переноса
- Низкий runtime-риск.
- Средний архитектурный риск, если ожидать от него `wave spawning`: текущая реализация этого не покрывает.

## Рекомендации для Bake or Die
Подходит как временный MVP-спавнер для прототипа волн, но для полноценного `wave spawning` нужен отдельный orchestration-слой поверх него.
