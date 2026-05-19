# Module: HealthSystem

## Назначение
Базовый модуль здоровья, урона, лечения и смерти через `Health`, `DamageContext` и интерфейсы `IDamageable` / `IHealable` / `IHealth`.

## Расположение
`Assets/Modules/HealthSystem`

## Статус переиспользования
Reusable

## Основные классы
- `Health` — runtime-компонент HP, событий урона, лечения и смерти.
- `DamageContext` — типизированный контекст урона.
- `DamageType`, `HitZone` — перечисления типа и зоны попадания.
- `IDamageable`, `IHealable`, `IHealth`, `ITargetableEntity` — публичные контракты модуля.

## Публичные точки входа
- Компонент `Health`.
- Методы `TakeDamage`, `Heal`, `Kill`, `ResetHealth`, `SetMaxHealth`.
- События `OnHealthChanged`, `OnDamageApplied`, `OnDamaged`, `OnHealed`, `OnDeath`.
- Интерфейсы `IDamageable`, `IHealable`, `IHealth`.

## Зависимости
- Unity runtime (`UnityEngine`).
- Внешних project-specific или vendor-зависимостей нет.

## Как подключить в новый проект
1. Скопировать папку вместе с `HealthSystem.asmdef`.
2. Повесить `Health` на сущности, которые должны иметь HP.
3. Подключать атакующие системы через `IDamageable` или через модуль `DamageSystem`.

## Что важно не сломать
- Порядок событий в `Health.TakeDamage`: `OnDamageApplied` -> `OnDamaged` -> `OnHealthChanged` -> `OnDeath`.
- `CanTakeDamage` и `CanApplyDamage` должны оставаться единой точкой валидации.
- `Health` сам управляет `destroyOnDeath`; внешний код не должен дублировать это поведение без необходимости.

## Риски переноса
- Низкий риск.
- Потребуется заново назначить подписчиков на события и inspector-настройки компонентов.

## Рекомендации для Bake or Die
Использовать как базу для `enemy health / damage`, `player health`, `pickups / resources` и любых интерактивных сущностей с HP.
