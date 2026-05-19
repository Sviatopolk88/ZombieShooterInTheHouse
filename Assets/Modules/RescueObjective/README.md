# Module: RescueObjective

## Назначение
Нейтральное ядро rescue-цели со статусами `WaitingForRescue`, `Rescued`, `Failed` и опциональным trigger-входом спасения.

## Расположение
`Assets/Modules/RescueObjective`

## Статус переиспользования
Reusable

## Основные классы
- `RescueObjective` — хранит состояние цели и публикует `StateChanged`.
- `RescueInteractionTrigger` — trigger-адаптер, который вызывает `Rescue()`.
- `RescueObjectiveState` — enum состояний.

## Публичные точки входа
- Компонент `RescueObjective`.
- Методы `Rescue()`, `Fail()`, `ResetState()`.
- Свойства `State`, `IsWaitingForRescue`, `IsRescued`, `IsFailed`, `IsValidEnemyTarget`.
- Событие `StateChanged`.
- Компонент `RescueInteractionTrigger`.

## Зависимости
- `Modules.HealthSystem`
- UnityEvents и trigger-collider runtime

## Как подключить в новый проект
1. Скопировать папку вместе с `RescueObjective.asmdef`.
2. Повесить `RescueObjective` на NPC/объект цели.
3. При необходимости добавить `RescueInteractionTrigger` рядом с целью или вызывать `Rescue()` / `Fail()` из внешнего flow-кода.

## Что важно не сломать
- `RescueObjective` не должен знать о HUD, сценах, статистике уровня или конкретных визуальных реакциях.
- `IsValidEnemyTarget` должен отражать только состояние objective, а не scene-specific условия.
- При использовании `failOnHealthDeath` цель должна иметь корректно найденный `Health`.

## Риски переноса
- Низкий риск.
- Потребуется отдельно перенести визуальные bridge-компоненты, если в новом проекте тоже нужны анимации, fade-out и level statistics.

## Рекомендации для Bake or Die
Использовать только если в Bake or Die есть rescue/escort/hostage-механика. Для чистого боевого MVP модуль не обязателен.
