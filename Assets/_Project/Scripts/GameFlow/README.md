## Назначение
Централизованное управление flow игры в `Main` и project-side логикой завершения уровня.

## Ответственность
- обработка смерти игрока
- управление death UI
- управление паузой и режимом курсора
- перезапуск уровня
- project-side завершение уровня через выход после смерти босса

## Использование
- При смерти игрока вызывать `GameFlowService.Instance.OnPlayerDied()`.
- Для рестарта использовать `GameFlowService.Instance.RestartLevel()`.

## Локализация Result UI
- Текущее result UI в проекте это `DeathScreen` в `Main`.
- Тексты локализуются project-side bridge-слоем над `PluginYourGames`.
- Источник языка: `YG2.lang`.
- Для `ru` показывается русский текст, для остальных случаев английский.

## Exit Flow
- `LevelExitController` живёт в `_Project` и настраивается явно в сцене `Level`.
- В `LevelExitController` вручную назначаются боссы, чья смерть открывает выход, и `Collider` trigger-зоны завершения уровня.
- `LevelExitTriggerZone` живёт отдельным компонентом на отдельном trigger-объекте и хранит явную ссылку на `LevelExitController`.
- Legacy `WinConditionHandler` отключается самим `LevelExitController`, поэтому смерть босса больше не завершает уровень автоматически.
- Если `LevelRescueController.RemainingCount > 0`, перед выходом показывается `LevelExitConfirmationController` в `Main`.
- `Leave` открывает project-side экран результата уровня.
- `Stay` закрывает окно подтверждения и возвращает игрока в gameplay.
- Если `RemainingCount == 0`, экран результата открывается сразу без confirmation.
- `LevelCompleteScreenController` живёт в `Main`, автоматически находит `VictoryScreen` и показывает итог:
  `Boss defeated`, `Rescued`, `Failed`, `Remaining`.
- Кнопка `Restart` на экране результата использует текущий `GameFlowService.RestartLevel()`.

## Scene Setup
- Добавьте на сцену объект `LevelExitController`.
- В список `bossRoots` назначьте одного или нескольких боссов.
- Создайте отдельный объект `ExitTriggerZone` с `Collider` в режиме `Is Trigger` и компонентом `LevelExitTriggerZone`.
- В `LevelExitTriggerZone` укажите ссылку на `LevelExitController`.
- В поле `exitTrigger` у `LevelExitController` назначьте `Collider` этого же объекта `ExitTriggerZone`.
- При необходимости заполните `activateOnUnlock` и `deactivateOnUnlock` для подсветки выхода, включения индикатора или отключения блокера.
- Визуальная анимация двери не обязательна: выход может быть дверью, световой зоной или любой другой trigger-областью.
- Отдельно настраивать result screen не нужно: используется существующий `VictoryScreen` в сцене `Main`.

## Поток Курсора
- `CursorStateService` живёт в `Main` как единая точка управления курсором.
- `Gameplay`: курсор скрыт и заблокирован через `NeoFpsInputManagerBase.captureMouseCursor`.
- `UI`: курсор видим и разблокирован.
- При переходе в `UI` сервис дополнительно отключает у текущего NeoFPS-персонажа `allowMoveInput`, `allowLookInput` и `allowWeaponInput`.
- При `GameOver` возврат в gameplay через `Tab` блокируется до рестарта уровня.

## Настройка Сцены
Добавить в `Main`:
- `GameFlowService`
- `CursorStateService`
- `CursorToggleInput`

Рекомендуется размещать эти компоненты рядом и не делать прямых cross-scene ссылок на объекты из `Level`.
