using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    /// <summary>
    /// Маркер отдельного hitbox головы. Используется для headshot-логики в adapter-layer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyHeadHitbox : MonoBehaviour
    {
        private Collider cachedCollider;

        public Collider HitCollider
        {
            get
            {
                if (cachedCollider == null)
                {
                    cachedCollider = GetComponent<Collider>();
                }

                return cachedCollider;
            }
        }
    }
}
