# Module: DoorBreachEncounter

## Назначение
Сценарный encounter выбивания двери: серия ударов, финальное открытие и активация связанных объектов в сцене.

## Расположение
`Assets/Modules/DoorBreachEncounter`

## Статус переиспользования
Reusable with cleanup

## Основные классы
- `DoorBreachEncounter` — основной state machine `Idle -> BreachSequence -> Breached`.
- `DoorBreachTriggerActivator` — опциональный trigger-активатор encounter.

## Публичные точки входа
- Компонент `DoorBreachEncounter`.
- Методы `Activate()`, `ForceBreach()`, `ResetEncounter()`.
- Компонент `DoorBreachTriggerActivator`.
- UnityEvents `onSequenceStarted`, `onBreach`.

## Зависимости
- Unity runtime и `UnityEvent`
- Scene references на дверь, blocker и объекты из `activateOnBreach`

## Как подключить в новый проект
1. Скопировать папку вместе с `DoorBreachEncounter.asmdef`.
2. Повесить `DoorBreachEncounter` на scene object рядом с дверью и назначить `shakeTarget`/`openTarget`.
3. Либо вызывать `Activate()` внешним кодом, либо поставить `DoorBreachTriggerActivator` на trigger-зону.

## Что важно не сломать
- Модуль целиком завязан на scene references, а не на данные-конфиги.
- В `RotateLocalY` меняется только локальная ось `Y`; pivot двери должен быть настроен заранее.
- `activateOnBreach` используется как orchestration hook и не должен содержать критичные объекты, которые обязаны быть активны до старта sequence.

## Риски переноса
- Средний риск.
- Логику легко перенести, но придётся заново собирать prefab/scene setup и door pivot.

## Рекомендации для Bake or Die
Использовать только если в MVP есть scripted breach/ambush encounter. Для обычной интерактивной двери модуль избыточен.
