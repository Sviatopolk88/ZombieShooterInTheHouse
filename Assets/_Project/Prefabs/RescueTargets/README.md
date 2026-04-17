# Rescue Targets

## Назначение

- Эта папка хранит project-side prefab мирных жителей, подготовленных под механику спасения.
- Базовый готовый prefab для сцены: `SM_Chr_Business_Male_04 1.prefab`.
- Reusable-логика состояния остаётся в модуле `RescueObjective`, а визуальная реакция жителя живёт в project-side helper-компонентах.

## Что уже настроено на prefab

- `Health`
  Житель может погибнуть до спасения и перейти в `Failed`.
- `RescueObjective`
  Состояния `WaitingForRescue / Rescued / Failed`.
- `EnemyTargetRegistryMember`
  Житель регистрируется как потенциальная цель для врагов через tag `RescueTarget`.
- `RescueTargetAnimatorBridge`
  Связывает состояние `RescueObjective` с Animator prefab.
- `RescueTargetFadeOutOnRescue`
  После успешного спасения запускает плавное исчезновение жителя и в конце отключает его корневой объект.
- `RescueTargetHealthAnimatorBridge`
  Связывает `Health.OnDamaged` и `Health.OnDeath` с hit/death анимациями жителя.
- `EnemyHeadshotProfile`
  Р”РѕР±Р°РІР»СЏРµС‚ instant-kill headshot-Р»РѕРіРёРєСѓ РґР»СЏ Р¶РёС‚РµР»СЏ, РµСЃР»Рё РЅР° РіРѕР»РѕРІРµ РµСЃС‚СЊ РѕС‚РґРµР»СЊРЅС‹Р№ collider СЃ `EnemyHeadHitbox`.
- `CapsuleCollider`
  Даёт стабильную цель для попаданий NeoFPS/hitscan оружия игрока и другого collider-based урона.
- `LevelRescueController`
  Не висит на самом prefab, но автоматически подхватывает этого жителя на уровне через `RescueObjective` и учитывает в статистике спасения.

## Анимации

- До спасения используется scared/waiting-анимация `Terrified.anim`.
- Для неё включён loop, чтобы житель не выпадал в T/A-pose после одного прохода.
- После успешного спасения используется project-side копия клипа `CA_Idle16_Happy2.FBX` в `Assets/_Project/Animations/RescueTargets/Clips/`.
- Выбран именно этот clip как наиболее близкий по смыслу к облегчению и радости после спасения. Если позже потребуется более сдержанная реакция, ближайшая альтернатива в пакете — `@CA_Idle15_Happy1.FBX`.
- Для урона и смерти сейчас используются гарантированно совместимые project-side `.anim` клипы:
  - hit: `Assets/_Project/Animations/Zombie/Clips/Zombie_Hit.anim`
  - death: `Assets/_Project/Animations/Zombie/Clips/Zombie_Death.anim`
- Дополнительно в project-side папку скопированы human-ориентированные FBX из `KAWAII_ANIMATIOMS_CoolAction`:
  - `Assets/_Project/Animations/RescueTargets/Clips/CA_Damage1.FBX`
  - `Assets/_Project/Animations/RescueTargets/Clips/CA_Damage_Knockdown.FBX`
  Они сохранены рядом как локальные assets для дальнейшей визуальной замены/ретаргета в редакторе.

## Animator setup

- Controller: `RescueTargetAnimator.controller`
- Default state: `Scared`
- Trigger: `Rescued`
- Trigger: `Hit`
- Bool: `IsDead`
- State `RescuedReaction` запускается bridge-компонентом, когда `RescueObjective` переходит в `Rescued`
- State `HitReaction` запускается через `Health.OnDamaged`
- State `Death` запускается через `Health.OnDeath`

`RescueTargetAnimatorBridge` не меняет reusable-модуль `RescueObjective`, а только слушает его текущее состояние и:
- держит жителя в scared state, пока цель ждёт спасения
- запускает позитивную реакцию после `Rescued`
- возвращает в scared state после `ResetState()`

`RescueTargetFadeOutOnRescue` работает отдельно от анимационного bridge:
- слушает переход `RescueObjective` в `Rescued`
- берёт только активные renderer'ы текущей визуальной версии жителя
- создаёт runtime-инстансы материалов через `renderer.materials`, поэтому не ломает shared material у других prefab
- переводит материалы в transparent-режим и плавно уводит alpha
- в конце fade отключает корень жителя через `SetActive(false)`

`RescueTargetHealthAnimatorBridge` работает отдельно от rescue/fade-логики:
- слушает `Health.OnDamaged`
- запускает trigger `Hit`
- слушает `Health.OnDeath`
- включает bool `IsDead`
- не вмешивается в reusable-модуль `RescueObjective`

## Как ставить на сцену

1. Поставь prefab жителя на уровень.
2. При необходимости рядом добавь отдельный trigger с `RescueInteractionTrigger`.
3. Настрой `requiredTag`, `rescueOnEnter` и слой активатора на trigger-объекте.
4. Если враги должны атаковать жителя, у prefab уже должен оставаться tag `RescueTarget`.
5. Для врагов, которые должны реагировать на жителей, добавь `RescueTarget` в `allowedTargetTags`.
6. Для HUD и статистики больше ничего вручную подключать не нужно: `LevelRescueController` собирает все активные `RescueObjective` в сцене уровня автоматически.
7. Для попаданий оружия игрока по жителю отдельный hitbox больше не нужен: корневой `CapsuleCollider` уже позволяет NeoFPS/hitscan урону доходить до `Health`.

## Что важно помнить

- Trigger взаимодействия для MVP лучше держать отдельным объектом рядом с жителем, а не зашивать в сам prefab.
- `RescueObjective` остаётся нейтральным reusable-модулем; визуальные реакции и конкретный prefab setup — это project-layer.
- Fade запускается только для `Rescued`. Логику `Failed` и смерти этот helper не трогает.
- Если нужен ещё один житель, проще делать variant или копию от уже подготовленного prefab с тем же набором компонентов и animator setup.

## Что проверить руками

1. Житель на сцене стоит в loop scared-анимации и не уходит в невалидную позу.
2. При входе игрока в trigger цель сразу получает `Rescued`.
3. После спасения проигрывается позитивная reaction-анимация.
4. Во время happy animation житель начинает плавно исчезать.
5. После завершения fade корневой объект жителя отключается и не остаётся висеть на сцене.
6. После смертельного урона житель уходит в `Failed`.
7. Враги, настроенные на `RescueTarget`, видят и выбирают жителя как цель.
8. Выстрелы игрока по жителю реально наносят урон через `Health`.
9. При обычном уроне проигрывается hit reaction, при смертельном — death animation.
