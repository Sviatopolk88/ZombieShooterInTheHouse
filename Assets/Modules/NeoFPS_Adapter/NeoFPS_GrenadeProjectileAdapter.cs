using System.Collections;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    // Адаптер для гранаты, который вызывает наш ExplosionAdapter вместо встроенного урона NeoFPS.
    public sealed class NeoFPS_GrenadeProjectileAdapter : MonoBehaviour
    {
        [SerializeField] private NeoFPS_ExplosionAdapter explosionPrefab;
        [SerializeField] private float delay = 2f;

        private bool hasExploded;

        private void Start()
        {
            StartCoroutine(ExplodeAfterDelay());
        }

        private IEnumerator ExplodeAfterDelay()
        {
            yield return new WaitForSeconds(delay);
            Explode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
            {
                return;
            }

            Explode();
        }

        public void Explode()
        {
            if (hasExploded)
            {
                return;
            }

            hasExploded = true;

            if (explosionPrefab == null)
            {
                Debug.LogWarning("NeoFPS_GrenadeProjectileAdapter: explosionPrefab is not assigned.", this);
                Destroy(gameObject);
                return;
            }

            Vector3 explosionPosition = transform.position;
            NeoFPS_ExplosionAdapter explosion = Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
            explosion.Explode(explosionPosition);

            Destroy(gameObject);
        }
    }
}
