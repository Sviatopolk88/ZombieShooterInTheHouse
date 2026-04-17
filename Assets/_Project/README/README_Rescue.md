# README_Rescue

## 1. Общая Схема
- `RescueObjective` остаётся reusable-модулем и хранит только состояние конкретной цели спасения.
- Подсчёт целей на уровне и вывод прогресса в HUD живут в `_Project` как integration layer.
- Источник истины по rescue-статусам это состояния `RescueObjective`.

## 2. Project-Side Слой
- `LevelRescueController`
  Живёт в `Level`, собирает все `RescueObjective` текущей сцены и считает:
  - `TotalCount`
  - `RescuedCount`
  - `FailedCount`
  - `RemainingCount`
- `LevelRescueControllerBootstrap`
  Автоматически создаёт `LevelRescueController` в сцене уровня, если в ней найден хотя бы один `RescueObjective`.
- `RescueHudPresenter`
  Живёт в `Main` и показывает на HUD локализованную строку прогресса через `PluginYourGames`.

## 3. Как Работает Подсчёт
- При старте уровня контроллер собирает все активные `RescueObjective` в своей сцене.
- Контроллер подписывается на нейтральное событие `RescueObjective.StateChanged`.
- После перехода цели в `Rescued` или `Failed` счётчики обновляются один раз и больше не теряются, даже если объект позже отключается после fade-out.
- При рестарте уровня сцена пересоздаётся, а контроллер собирает новый список целей заново.

## 4. Как Цель Попадает В Статистику
- Достаточно, чтобы на объекте уровня был `RescueObjective`.
- Никакой ручной регистрации в `LevelRescueController` не требуется.
- Если на уровень добавлен ещё один житель с `RescueObjective`, он автоматически попадёт в статистику после загрузки сцены.

## 5. HUD
- HUD живёт в `Main`.
- `NeoFPS_MinimalHudInstaller` создаёт project-side rescue-счётчик внутри `HUD_PC`.
- `RescueHudPresenter` подписывается на активный `LevelRescueController` без cross-scene ссылок через инспектор.
- Текущий минимальный формат берётся из project-side bridge над `PluginYourGames`: `Спасено: X / Y` для `ru`, `Rescued: X / Y` для остальных языков.

## 6. Exit Flow
- `RemainingCount` означает только живых и ещё не спасённых жителей в состоянии `WaitingForRescue`.
- `LevelExitController` использует `RemainingCount` при попытке выйти с уровня после смерти босса.
- `LevelExitTriggerZone` висит на отдельном trigger-объекте и через явную ссылку вызывает `LevelExitController.TryExit()`.
- Если `RemainingCount > 0`, игрок видит confirmation перед завершением уровня.
- Жители в состоянии `Failed` не вызывают это предупреждение.
- Если `RemainingCount == 0`, выход с уровня происходит сразу без дополнительного окна.

## 7. Что Проверить Руками
1. При старте уровня с жителями HUD показывает корректное общее число целей.
2. При спасении жителя счётчик увеличивает `RescuedCount`, а `TotalCount` остаётся прежним.
3. При гибели жителя HUD не увеличивает спасённых, но контроллер увеличивает `FailedCount`.
4. После исчезновения спасённого жителя статистика не сбрасывается.
5. После рестарта уровня статистика собирается заново из нового состояния сцены.
6. После смерти босса при `RemainingCount > 0` выход показывает предупреждение перед завершением уровня.
