using DamageSystemUtility = Modules.DamageSystem.DamageSystem;
using Modules.HealthSystem;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    // Адаптер для взрывов, который наносит урон через наш DamageSystem.
    public sealed class NeoFPS_ExplosionAdapter : MonoBehaviour
    {
        [SerializeField] private int damage = 40;
        [SerializeField] private float radius = 5f;
        [SerializeField] private LayerMask targetLayers = Physics.DefaultRaycastLayers;

        public void Explode(Vector3 position)
        {
            if (radius <= 0f)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(position, radius, targetLayers);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                GameObject target = hit.gameObject;
                if (target == null)
                {
                    continue;
                }

                DamageContext context = new DamageContext(
                    damage,
                    DamageType.Explosion,
                    gameObject,
                    false,
                    HitZone.Body);

                DamageSystemUtility.ApplyDamage(target, context);
            }
        }
    }
}
