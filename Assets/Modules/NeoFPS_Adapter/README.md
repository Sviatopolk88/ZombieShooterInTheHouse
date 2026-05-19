# Module: NeoFPS_Adapter

## Назначение
Адаптеры между reusable-модулями проекта и vendor-фреймворком NeoFPS: урон, игрок, взрывы, стартовый loadout и headshot-профиль.

## Расположение
`Assets/Modules/NeoFPS_Adapter`

## Статус переиспользования
Project-specific

## Основные классы
- `NeoFPS_PlayerAdapter` — делегирует входящий урон в `Health`.
- `NeoFPS_AmmoEffectAdapter` — переводит hitscan NeoFPS в `DamageSystem`.
- `NeoFPS_ExplosionAdapter`, `NeoFPS_ExplosionAmmoEffectAdapter`, `NeoFPS_GrenadeProjectileAdapter` — мосты для explosive damage.
- `NeoFPS_PlayerLoadoutAdapter` — project-side стартовый loadout через API NeoFPS.
- `EnemyHeadshotProfile`, `EnemyHeadHitbox` — headshot-обвязка поверх врагов проекта.

## Публичные точки входа
- Компоненты `NeoFPS_PlayerAdapter`, `NeoFPS_PlayerLoadoutAdapter`, ammo/explosion adapters.
- Scriptable/prefab setup NeoFPS weapon effects.
- `EnemyHeadshotProfile` на prefab врага.

## Зависимости
- `NeoFPS`
- `NeoFPS.Managers`
- `NeoSaveGames`
- `Modules.HealthSystem`
- `Modules.DamageSystem`

## Как подключить в новый проект
1. Переносить только если новый проект тоже построен на NeoFPS.
2. Скопировать модуль вместе с `NeoFPS_Adapter.asmdef`.
3. Переназначить weapon effects, player prefab и headshot setup под конкретные prefab нового проекта.

## Что важно не сломать
- Модуль критично зависит от типов и lifecycle NeoFPS.
- `NeoFPS_PlayerLoadoutAdapter` рассчитывает на порядок инициализации NeoFPS inventory.
- `EnemyHeadshotProfile` и `EnemyHeadHitbox` привязаны к текущей иерархии вражеских prefab.

## Риски переноса
- Высокий риск.
- Без того же vendor-стека и близкой prefab-структуры модуль бесполезен.

## Рекомендации для Bake or Die
Не рекомендован для переноса в Bake or Die, если MVP не использует NeoFPS как базовый gameplay framework.
