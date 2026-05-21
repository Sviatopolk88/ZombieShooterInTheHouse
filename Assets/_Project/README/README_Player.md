# Player prefab и hands melee

## Project-owned player prefab

- Runtime player в build-сценах `Level_1`, `Level_2`, `Level_3` теперь использует project-owned prefab `Assets/_Project/Prefabs/Player/PrototypeSpawnerlessCharacter_Project.prefab`.
- Prefab скопирован из NeoFPS `PrototypeSpawnerlessCharacter`, чтобы дальнейшие настройки игрока не требовали правок vendor assets.
- В project copy отключён inherited NeoFPS `m_BackupItem`; hands melee выдаётся как обычный selectable item через `NeoFPS_PlayerLoadoutAdapter`.
- Scene-added project components и текущий loadout flow сохранены на scene instances.

## Hands melee damage

- Hands prefab: `Assets/_Project/Resources/Weapons/BackupWeapon_Hands_Project.prefab`.
- GameObject внутри prefab: `BackupWeapon_Hands_Project`.
- Component: `NeoFPS.MeleeWeapon`.
- Field: `Damage`.
- Текущее значение: `10`.
- `Range` оставлен `2.25`, hit delay и animation triggers оставлены из NeoFPS prefab.

## Enemy hit reaction от hands

- NeoFPS melee вызывает `IDamageHandler` только на collider transform, в который попал raycast.
- Для project enemy prefabs на root collider добавлен `Modules.NeoFPS_Adapter.NeoFPS_DamageHandlerAdapter`.
- Adapter переводит melee hit в `Modules.HealthSystem.DamageContext` с `DamageType.Melee` и `HitZone.Body`.
- Для попаданий по голове `EnemyHeadHitbox` сам реализует NeoFPS `IDamageHandler` и передаёт урон как `DamageType.Melee`, `HitZone.Head`, `critical = true`.
- Hit reaction не дублируется: она идёт через существующий `Health.OnDamaged -> EnemyAI_Base -> EnemyAnimationController.PlayHit()` pipeline.

## Save/load

- Hands melee остаётся baseline weapon и не сохраняется.
- Старые save, где мог встретиться `melee_hands`, должны игнорироваться существующей save/load логикой.
- Assault rifle, magazine и `ammo556mm` сохраняются отдельно и не зависят от player prefab copy.
