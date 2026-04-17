# Архетипы врагов

## 1. Общая информация

- Базовая логика врагов остаётся общей и живёт в модуле `EnemyAI_Base`.
- Различия между архетипами задаются через project-side prefab в [Assets/_Project/Prefabs/Enemies](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Prefabs/Enemies).
- `EnemyBase.prefab` сохраняется как технический референс и совместим с текущей сценой. Для новой расстановки на уровне используйте именованные prefab архетипов.

## 2. Доступные архетипы

| Архетип | Prefab | Роль | Здоровье | Скорость | Урон | Агро | Дистанция атаки | Кулдаун атаки |
| ------- | ------ | ---- | -------- | -------- | ---- | ---- | --------------- | ------------- |
| Обычный зомби | `Enemy_Zombie_Normal` | Базовый средний враг | 100 | 2.0 | 10 | 3.0 | 1.5 | 1.0 |
| Быстрый зомби | `Enemy_Zombie_Fast` | Rush-враг, давит скоростью | 65 | 3.6 | 8 | 4.0 | 1.35 | 0.7 |
| Танк-зомби | `Enemy_Zombie_Tank` | Медленный тяжёлый враг | 240 | 1.35 | 24 | 2.5 | 1.7 | 1.6 |
| Зомби-босс | `Enemy_Zombie_Boss` | Мини-босс, держит урон и продавливает игрока | 600 | 1.7 | 35 | 5.0 | 1.9 | 1.15 |

## 3. Анимации движения

- Обычный зомби использует базовый controller [ZombieAnimator.controller](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Controller/ZombieAnimator.controller) с walk-клипом [Zombie_Walk.anim](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Clips/Zombie_Walk.anim).
- Быстрый зомби использует отдельный controller [ZombieAnimator_Fast.controller](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Controller/ZombieAnimator_Fast.controller), где движение заменено на локальный clip [Zombie_Sprint_02_Forward_InPlace.fbx](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Clips/Zombie_Sprint_02_Forward_InPlace.fbx).
- Танк-зомби использует отдельный controller [ZombieAnimator_Tank.controller](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Controller/ZombieAnimator_Tank.controller), где движение заменено на локальный clip [Zombie_Walk_02_Forward_InPlace.fbx](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Clips/Zombie_Walk_02_Forward_InPlace.fbx).
- Зомби-босс сейчас использует тот же тяжёлый controller [ZombieAnimator_Tank.controller](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Animations/Zombie/Controller/ZombieAnimator_Tank.controller), чтобы сохранить читаемую тяжёлую походку без отдельной анимационной ветки.
- Остальные состояния `Idle / Attack / Hit / Death` остаются общими и не дублируются по логике.

## 4. Где настраиваются параметры

- `Health.maxHealth` отвечает за запас здоровья.
- `EnemyAI_Base.moveSpeed` задаёт целевую скорость и прокидывается в `EnemyMovement` и `NavMeshAgent`.
- `EnemyAI_Base.attackDistance` определяет дистанцию входа в атаку.
- `EnemyAI_Base.attackCooldown` определяет частоту атак.
- `EnemyAI_Base.allowedTargetTags` задаёт список допустимых тегов целей. Если список пуст, используется legacy-поле `playerTag`, поэтому старые prefab продолжают работать как раньше.
- `EnemyAI_Base.targetPriorityMode` задаёт режим выбора цели:
  - `TagOrder` — теги проверяются по порядку в `allowedTargetTags`, и враг берёт ближайшую видимую цель первого подходящего тега;
  - `NearestAllowedTarget` — враг выбирает ближайшую видимую цель среди всех допустимых тегов.
- `EnemyAI_Base.lostTargetCooldown` задаёт, сколько секунд враг ещё помнит цель после потери прямой видимости, прежде чем вернуться в режим покоя.
- `EnemyVision.viewDistance` и `EnemyVision.aggroDistance` отвечают за обнаружение и ближний агро-радиус.
- `EnemyAttack.damage` задаёт урон за успешный удар.
- `EnemyAttack.attackDistance` и `EnemyAttack.attackDelay` задают фактическую дистанцию нанесения урона и задержку удара внутри анимации.
- `EnemyAI_Base` теперь также отвечает за hit-reaction lock: при `Health.OnDamaged` он временно стопает `NavMeshAgent` и не даёт chase/attack возобновиться, пока `EnemyAnimationController` видит state `Hit`.
- `EnemyAI_Base` также подписан на `Health.OnDamageApplied`: если враг получает урон от объекта игрока и выживает, он запоминает этого игрока как `pendingAggroTarget`. После завершения hit reaction цель переводится в обычный chase flow.
- При потере прямой видимости враг не падает в idle мгновенно: пока не истёк `lostTargetCooldown`, он продолжает двигаться к последней известной позиции игрока. После истечения таймера цель очищается, и враг возвращается в покой.
- Если цель имеет `RescueObjective`, она считается валидной только в состоянии `WaitingForRescue`. Цели в состояниях `Rescued` и `Failed` автоматически исключаются из выбора.
- `EnemyAnimationController` во время state `Hit` принудительно подаёт в Animator `Speed = 0`, чтобы реакция на урон не смешивалась с locomotion.
- Брызги крови при попадании берутся из штатного surface-impact pipeline NeoFPS: `SurfaceManager` использует `SurfaceHitFxData.asset`, где для `Flesh` уже назначен `HitFX_Flesh.prefab`.
- Project-side компонент `EnemyFleshSurface` помечает врага как поверхность `Flesh`, а `NeoFPS_AmmoEffectAdapter` теперь снова вызывает `SurfaceManager.ShowBulletHit(...)` в точке `RaycastHit`.
- Проблема первой версии была в том, что NeoFPS для ammo effect приносит один итоговый `RaycastHit` по ближайшему collider. Если root/body collider пересекал область головы, луч мог вернуть тело раньше `Head`, даже когда траектория визуально проходила через голову.
- Headshot теперь определяется в два шага: сначала проверяется прямое попадание в `EnemyHeadHitbox`, затем выполняется secondary validation по тому же лучу через `Collider.Raycast(...)` для всех head hitbox на враге с небольшим overlap-tolerance. Это сохраняет текущий hit-pipeline и убирает большую часть ложных body-hit при пересечении body/head collider.
- На корне врага стоит `EnemyHeadshotProfile`: для обычных и быстрых зомби он настроен в `InstantKill`, для tank/boss — в `DamageMultiplier` со значением `2`.
- Итоговые параметры head collider для текущих врагов: `SphereCollider radius = 0.15`, `center.y = +0.05`.
- `Transform.localScale` используется как минимальная визуальная дифференциация для быстрых, танк- и boss-вариантов.

## 5. Правила использования

- Для обычного врага используйте [Enemy_Zombie_Normal.prefab](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Prefabs/Enemies/Enemy_Zombie_Normal.prefab).
- Для rush-врага используйте [Enemy_Zombie_Fast.prefab](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Prefabs/Enemies/Enemy_Zombie_Fast.prefab).
- Для тяжёлого врага используйте [Enemy_Zombie_Tank.prefab](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Prefabs/Enemies/Enemy_Zombie_Tank.prefab).
- Для босса используйте [Enemy_Zombie_Boss.prefab](/c:/Users/User/Documents/KnightMarten%20Games/Test/ZombieShooterInTheHouse/Assets/_Project/Prefabs/Enemies/Boss/Enemy_Zombie_Boss.prefab).
- Чтобы сохранить текущее поведение, ничего не меняйте: при пустом `allowedTargetTags` враг, как и раньше, ориентируется только на `playerTag = Player`.
- Чтобы враг мог охотиться и на мирных жителей, назначьте жителям тег, например `RescueTarget`, и добавьте его в `allowedTargetTags`.
- Для режима `PlayerFirst` используйте `targetPriorityMode = TagOrder` и порядок тегов `Player`, затем `RescueTarget`.
- Для режима `RescueTargetFirst` используйте `targetPriorityMode = TagOrder` и порядок тегов `RescueTarget`, затем `Player`.
- Для режима выбора по ближайшей цели используйте `targetPriorityMode = NearestAllowedTarget`.
- `EnemyBase.prefab` не редактируйте под конкретную сцену. От него проще делать новые archetype prefab.
- `EnemySpawner` уже принимает любой enemy prefab, поэтому новые архетипы можно спавнить без изменения кода.
- Если нужен новый тип походки, копируйте clip в `Assets/_Project/Animations/Zombie/Clips/` и делайте отдельный controller-вариант в `Assets/_Project/Animations/Zombie/Controller/`, а не меняйте общий controller.
- Если новый враг должен давать blood impact, на его prefab должен стоять `EnemyFleshSurface` или другой `BaseSurface`, который возвращает `FpsSurfaceMaterial.Flesh`.
- Если новый враг должен поддерживать headshot, на его prefab должны быть:
  1. `EnemyHeadshotProfile` на корне.
  2. Маленький `SphereCollider` на кости `Head` с базовыми параметрами `radius = 0.15` и `center.y = +0.05`.
  3. `EnemyHeadHitbox` на том же объекте `Head`.

## 6. Как добавлять новые архетипы

1. Скопировать `EnemyBase.prefab` или существующий archetype prefab в `Assets/_Project/Prefabs/Enemies/`.
2. Переименовать prefab по шаблону `Enemy_Zombie_<Role>`.
3. Настроить `Health`, `EnemyAI_Base`, `EnemyMovement`, `EnemyVision`, `EnemyAttack`.
   Для поведения преследования на archetype-уровне отдельно настройте `lostTargetCooldown`: меньшие значения дают более резкий возврат в idle, большие — более настойчивый поиск игрока.
   Для выбора типа жертв отдельно настройте `allowedTargetTags` и `targetPriorityMode`.
4. При необходимости слегка изменить `Transform.localScale` для читаемости роли.
5. Настроить `EnemyHeadshotProfile`:
   для обычного врага `InstantKill`, для тяжёлого — `DamageMultiplier` со значением `2`.
6. Проверить в Unity движение по NavMesh, hit reaction, body/head урон, анимацию атаки и смерть.
