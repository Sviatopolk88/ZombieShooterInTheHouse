# HealthSystem

Автономный модуль здоровья: HP, урон, лечение, смерть.

## Публичный API

### Методы

- `TakeDamage(int amount)`
- `TakeDamage(DamageContext context) -> bool`
- `Heal(int amount)`
- `Kill()`
- `ResetHealth()`
- `SetMaxHealth(int value, bool fillHealth = true)`
- `CanApplyDamage(in DamageContext context) -> bool`

### Свойства

- `CurrentHealth`
- `MaxHealth`
- `IsDead`
- `IsAlive`
- `NormalizedHealth`
- `CanTakeDamage`
- `LastDamageContext`

### События

- `OnDamageApplied(DamageContext context, int appliedDamage)`
- `OnDamaged(int appliedDamage)`
- `OnHealthChanged(int currentHealth, int maxHealth)`
- `OnHealed(int restoredHealth)`
- `OnDeath()`

## Назначение

- `Health` хранит здоровье и события.
- `HealthDamageableAdapter` связывает `Health` с `DamageSystem` через `IDamageable`.

## Интеграция с DamageSystem

- Добавь на объект `Health`.
- Добавь на тот же объект или дочерний объект `HealthDamageableAdapter`.
- `DamageSystem` найдёт `IDamageable` и передаст урон в `Health`.

## Правила урона

- Урон игнорируется, если объект мертв.
- Урон игнорируется, если включена неуязвимость.
- Урон игнорируется, если `amount <= 0`.
- Здоровье всегда находится в диапазоне `0..MaxHealth`.
- `TakeDamage(int)` вызывает `TakeDamage(DamageContext)` внутри.
- `Kill()` не вызывает события урона.
- `Kill()` вызывает только `OnHealthChanged` и `OnDeath`.
- `CanApplyDamage(...)` используется только для простых проверок.
- `CanApplyDamage(...)` не должен содержать сложной логики.

## Порядок событий

### Нанесение урона

1. Проверка
2. Изменение HP
3. `OnDamageApplied`
4. `OnDamaged`
5. `OnHealthChanged`
6. `OnDeath`

### Лечение

1. Проверка
2. Изменение HP
3. `OnHealed`
4. `OnHealthChanged`
