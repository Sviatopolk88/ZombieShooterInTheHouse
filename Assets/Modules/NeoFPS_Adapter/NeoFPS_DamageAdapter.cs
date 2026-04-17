using DamageSystemUtility = Modules.DamageSystem.DamageSystem;
using Modules.HealthSystem;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    public sealed class NeoFPS_DamageAdapter : MonoBehaviour
    {
        [SerializeField] private int damage = 20;

        public void ApplyDamage(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            // Создаем простой контекст попадания и передаем его в общий DamageSystem.
            DamageContext context = new DamageContext(
                damage,
                DamageType.Bullet,
                gameObject,
                false,
                HitZone.Body);

            DamageSystemUtility.ApplyDamage(target, context);
        }

        public void ApplyDamage(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            ApplyDamage(collider.gameObject);
        }
    }
}
