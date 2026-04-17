# DamageSystem

## Назначение

Единый способ наносить урон объектам через интерфейс `IDamageable`.
Модуль не знает о конкретной реализации здоровья и работает только через публичный контракт.

## Публичный API

- `ApplyDamage(GameObject target, int amount)`
- `ApplyDamage(GameObject target, DamageContext context)`
- `ApplyDamage(Component target, int amount)`
- `ApplyDamage(Component target, DamageContext context)`
- `ApplyDamage(Collider collider, DamageContext context)`
- `TryGetDamageable(GameObject target, out IDamageable damageable)`

## Пример использования

```csharp
using Modules.DamageSystem;
using Modules.HealthSystem;
using UnityEngine;

public class ExampleDamageDealer : MonoBehaviour
{
    [SerializeField] private GameObject target;

    public void DealSimpleDamage()
    {
        DamageSystem.ApplyDamage(target, 10);
    }

    public void DealBulletDamage()
    {
        DamageContext context = new DamageContext(
            25,
            DamageType.Bullet,
            gameObject,
            true,
            HitZone.Head);

        DamageSystem.ApplyDamage(target, context);
    }
}
```

## Примечания

- `DamageSystem` не рассчитывает урон.
- `DamageSystem` только передает урон в `IDamageable`.
- Вся логика проверки урона находится в `HealthSystem`.
