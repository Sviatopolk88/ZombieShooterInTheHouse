using Modules.HealthSystem;
using UnityEngine;

namespace Modules.DamageSystem
{
    public static class DamageSystem
    {
        public static bool ApplyDamage(GameObject target, int amount)
        {
            if (target == null)
            {
                return false;
            }

            return ApplyDamage(target, new DamageContext(amount));
        }

        public static bool ApplyDamage(GameObject target, DamageContext context)
        {
            if (!TryGetDamageable(target, out IDamageable damageable))
            {
                return false;
            }

            if (!damageable.CanTakeDamage)
            {
                return false;
            }

            return damageable.TakeDamage(context);
        }

        public static bool ApplyDamage(Component target, int amount)
        {
            if (target == null)
            {
                return false;
            }

            return ApplyDamage(target.gameObject, amount);
        }

        public static bool ApplyDamage(Component target, DamageContext context)
        {
            if (target == null)
            {
                return false;
            }

            return ApplyDamage(target.gameObject, context);
        }

        public static bool ApplyDamage(Collider collider, DamageContext context)
        {
            if (collider == null)
            {
                return false;
            }

            return ApplyDamage(collider.gameObject, context);
        }

        public static bool TryGetDamageable(GameObject target, out IDamageable damageable)
        {
            damageable = null;

            if (target == null)
            {
                return false;
            }

            // Пример использования:
            // DamageSystem.ApplyDamage(target, 10);
            //
            // DamageContext context = new DamageContext(
            //     25,
            //     DamageType.Bullet,
            //     gameObject,
            //     true,
            //     HitZone.Head);
            //
            // DamageSystem.ApplyDamage(target, context);
            damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                return true;
            }

            damageable = target.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                return true;
            }

#if UNITY_EDITOR
            Debug.LogWarning("DamageSystem: IDamageable not found", target);
#endif

            return false;
        }
    }
}
