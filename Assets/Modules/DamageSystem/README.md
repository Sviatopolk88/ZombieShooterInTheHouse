# Module: DamageSystem

## Назначение
Тонкий фасад нанесения урона, который ищет `IDamageable` на объекте или в родителях и передаёт туда `DamageContext`.

## Расположение
`Assets/Modules/DamageSystem`

## Статус переиспользования
Reusable

## Основные классы
- `DamageSystem` — статический вход для применения урона.

## Публичные точки входа
- `DamageSystem.ApplyDamage(GameObject, int)`
- `DamageSystem.ApplyDamage(GameObject, DamageContext)`
- `DamageSystem.ApplyDamage(Component, int|DamageContext)`
- `DamageSystem.ApplyDamage(Collider, DamageContext)`
- `DamageSystem.TryGetDamageable(GameObject, out IDamageable)`

## Зависимости
- `Modules.HealthSystem`

## Как подключить в новый проект
1. Скопировать папку вместе с `DamageSystem.asmdef`.
2. Подключить зависимость на `Modules.HealthSystem`.
3. Вызывать `DamageSystem.ApplyDamage(...)` из оружия, ловушек, снарядов и AI.

## Что важно не сломать
- Модуль не считает итоговый урон и не содержит gameplay-правил.
- Поиск `IDamageable` идёт сначала на объекте, потом по иерархии вверх.
- Валидация урона должна оставаться в реализации `IDamageable`, а не в фасаде.

## Риски переноса
- Низкий риск.
- Если новый проект ожидает попадание по дочерним хитбоксам, нужно сохранить текущую схему поиска в родителях.

## Рекомендации для Bake or Die
Использовать как общую точку нанесения урона для `player combat`, `enemy combat`, `wave spawning` последствий и интерактивных объектов.
