# Module: EnemyAI_Base

## Назначение
Базовый AI-модуль врага на `NavMeshAgent`: поиск цели, преследование, видимость, атака, hit reaction и возврат в idle.

## Расположение
`Assets/Modules/EnemyAI_Base`

## Статус переиспользования
Reusable with cleanup

## Основные классы
- `EnemyAI_Base` — orchestrator поведения.
- `EnemyMovement` — движение к цели или позиции.
- `EnemyVision` — проверка поля зрения и ближнего aggro через NavMesh path.
- `EnemyAttack` — тайминг и применение урона по цели.
- `EnemyAnimationController` — мост к `Animator`.
- `EnemyTargetRegistry` и `EnemyTargetRegistryMember` — реестр допустимых целей.

## Публичные точки входа
- Компоненты `EnemyAI_Base`, `EnemyMovement`, `EnemyVision`, `EnemyAttack`, `EnemyAnimationController`.
- Компонент `EnemyTargetRegistryMember` на игроке, жителях и других целях.
- Inspector-поля `allowedTargetTags`, `targetPriorityMode`, `lostTargetCooldown`, `retargetDistanceAdvantage`.
- Публичные свойства `CurrentTarget`, `HasTarget`, `IsPursuingPlayer`.

## Зависимости
- `Modules.HealthSystem`
- `Modules.DamageSystem`
- Unity `NavMeshAgent`, `Animator`, physics raycast
- Не зависит от `_Project`, но ожидает корректные теги, анимационные state names и prefab-структуру

## Как подключить в новый проект
1. Скопировать папку вместе с `EnemyAI_Base.asmdef`.
2. На prefab врага повесить минимум `EnemyAI_Base`, `NavMeshAgent`, `EnemyVision`, `EnemyAttack`; при необходимости `EnemyMovement` и `EnemyAnimationController`.
3. На цели повесить `EnemyTargetRegistryMember` и настроить у врага `allowedTargetTags`.

## Что важно не сломать
- `EnemyAnimationController` ожидает параметры/состояния `Speed`, `IsAttacking`, `IsDead`, `Hit`, `Attack`, `Hit`.
- `EnemyAttack` работает в связке с `HealthSystem` и ожидает `IDamageable` на цели.
- Логика агро от входящего урона строится на `DamageContext.Source`.

## Риски переноса
- Средний риск.
- Потребуется перенастроить теги, NavMesh, Animator и collider/layout цели.
- Без cleanup модуль переносим, но содержит много конфигурационных допущений зомби-прототипа.

## Рекомендации для Bake or Die
Использовать как базу для `enemy combat`, `enemy health / damage` и преследования, если Bake or Die тоже строится на humanoid-врагах и `NavMeshAgent`. Перед переносом стоит отделить зомби-специфичные animator assumptions в адаптер/конфиг.
