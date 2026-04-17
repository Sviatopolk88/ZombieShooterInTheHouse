using NeoFPS;
using NeoFPS.ModularFirearms;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    // Адаптер для grenade launcher, который спавнит наш explosion adapter вместо PooledExplosion.
    public sealed class NeoFPS_ExplosionAmmoEffectAdapter : BaseAmmoEffect
    {
        [SerializeField, NeoPrefabField(typeof(NeoFPS_ExplosionAdapter), required = true), Tooltip("Префаб взрыва с компонентом NeoFPS_ExplosionAdapter.")]
        private GameObject explosionPrefab;

        public override void Hit(RaycastHit hit, Vector3 rayDirection, float totalDistance, float speed, IDamageSource damageSource)
        {
            if (explosionPrefab == null)
            {
                Debug.LogWarning("NeoFPS_ExplosionAmmoEffectAdapter: explosionPrefab is not assigned.", this);
                return;
            }

            Vector3 explosionPosition = hit.point;
            GameObject explosionObject = Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
            NeoFPS_ExplosionAdapter explosion = explosionObject.GetComponent<NeoFPS_ExplosionAdapter>();
            if (explosion == null)
            {
                Debug.LogWarning("NeoFPS_ExplosionAmmoEffectAdapter: NeoFPS_ExplosionAdapter component not found on explosionPrefab.", this);
                Destroy(explosionObject);
                return;
            }

            explosion.Explode(explosionPosition);
        }
    }
}
