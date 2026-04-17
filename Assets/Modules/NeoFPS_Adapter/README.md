# NeoFPS_Adapter

## Назначение

Адаптеры для интеграции NeoFPS с `HealthSystem` и `DamageSystem` без изменения кода NeoFPS.

## NeoFPS_PlayerAdapter

### Как подключить

1. Добавьте `Health` на корневой объект NeoFPS Player или на его родителя.
2. Добавьте `NeoFPS_PlayerAdapter` на объект игрока, который будет получать урон.
3. Убедитесь, что входящий урон идет через `DamageSystem`.

### Как работает интеграция

- `DamageSystem` ищет `IDamageable` на объекте игрока.
- `NeoFPS_PlayerAdapter` делегирует вызов в `Health`.
- `Health` ищется через `GetComponentInParent`.
- Адаптер подписывается на `Health.OnDeath`.
- При смерти игрока адаптер пока только пишет `Player died` в консоль.

## NeoFPS_AmmoEffectAdapter

### Назначение

Замена стандартного `BulletAmmoEffect`, которая отправляет попадание NeoFPS в наш `DamageSystem`.

### Как заменить BulletAmmoEffect

1. Откройте prefab оружия NeoFPS.
2. Найдите компонент `BulletAmmoEffect`.
3. Замените его на `NeoFPS_AmmoEffectAdapter`.
4. Убедитесь, что `HitscanShooter` или другой shooter использует этот ammo effect.

### Как работает интеграция

- NeoFPS выполняет выстрел через `HitscanShooter`.
- При попадании вызывается `Hit(...)` у активного ammo effect.
- `NeoFPS_AmmoEffectAdapter` получает цель из `RaycastHit`.
- Адаптер создает `DamageContext` с типом `Bullet`.
- Урон передается в `DamageSystem`, а затем в `IDamageable`.

## NeoFPS_ExplosionAdapter

### Назначение

Адаптер для взрывов NeoFPS, который наносит урон по радиусу через `DamageSystem`.

### Как подключить

1. Добавьте `NeoFPS_ExplosionAdapter` на объект взрыва или рядом с grenade logic.
2. При событии взрыва вызовите `Explode(position)`.
3. Если нужно полностью уйти от встроенного урона NeoFPS, не используйте стандартный damage path `PooledExplosion`.

### Отличие от AmmoEffectAdapter

- `NeoFPS_AmmoEffectAdapter` работает с одиночным попаданием из `RaycastHit`.
- `NeoFPS_ExplosionAdapter` сам ищет все цели в радиусе через `Physics.OverlapSphere`.

### Как работает интеграция

- Система гранаты или взрыва вызывает `Explode(position)`.
- Адаптер ищет все коллайдеры в заданном радиусе.
- Для каждой цели создается `DamageContext` с типом `Explosion`.
- Урон передается в `DamageSystem`, а затем в `IDamageable`.

## NeoFPS_ExplosionAmmoEffectAdapter

### Назначение

Замена `PooledExplosionAmmoEffect` для grenade launcher, которая вызывает `NeoFPS_ExplosionAdapter`.

### Как заменить PooledExplosionAmmoEffect

1. Откройте prefab grenade launcher в NeoFPS.
2. Найдите компонент `PooledExplosionAmmoEffect`.
3. Замените его на `NeoFPS_ExplosionAmmoEffectAdapter`.
4. Назначьте `explosionPrefab` с компонентом `NeoFPS_ExplosionAdapter`.

### Как работает интеграция

- NeoFPS weapon вызывает `Hit(...)` у ammo effect.
- `NeoFPS_ExplosionAmmoEffectAdapter` берет точку попадания `hit.point`.
- Адаптер создает экземпляр `NeoFPS_ExplosionAdapter`.
- `NeoFPS_ExplosionAdapter` наносит урон по радиусу через `DamageSystem`.

## NeoFPS_GrenadeProjectileAdapter

### Назначение

Замена `GrenadeThrownProjectile` и `ContactGrenadeThrownProjectile`, которая вызывает `NeoFPS_ExplosionAdapter`.

### Как заменить GrenadeThrownProjectile

1. Откройте prefab гранаты в NeoFPS.
2. Уберите `GrenadeThrownProjectile` или `ContactGrenadeThrownProjectile`.
3. Добавьте `NeoFPS_GrenadeProjectileAdapter`.
4. Назначьте `explosionPrefab` с компонентом `NeoFPS_ExplosionAdapter`.
5. Настройте `delay` для таймера взрыва.

### Как работает интеграция

- После спавна гранаты запускается таймер.
- При столкновении граната тоже может взорваться раньше таймера.
- Адаптер создает экземпляр `NeoFPS_ExplosionAdapter`.
- `NeoFPS_ExplosionAdapter` наносит урон по радиусу через `DamageSystem`.
