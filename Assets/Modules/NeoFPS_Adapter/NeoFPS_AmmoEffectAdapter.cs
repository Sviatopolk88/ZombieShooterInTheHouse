using DamageSystemUtility = Modules.DamageSystem.DamageSystem;
using HealthDamageType = Modules.HealthSystem.DamageType;
using Modules.HealthSystem;
using NeoFPS;
using NeoFPS.ModularFirearms;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    // Адаптер для NeoFPS ammo effect, который перенаправляет урон в наш DamageSystem.
    public sealed class NeoFPS_AmmoEffectAdapter : BaseAmmoEffect
    {
        [SerializeField] private int damage = 20;
        [SerializeField] private float bulletSize = 1f;
        [SerializeField] private float headshotOverlapTolerance = 0.08f;

        public override void Hit(RaycastHit hit, Vector3 rayDirection, float totalDistance, float speed, IDamageSource damageSource)
        {
            if (hit.collider != null)
            {
                // Сохраняем штатный surface hit FX NeoFPS, чтобы получать decals / particles по типу поверхности.
                SurfaceManager.ShowBulletHit(hit, rayDirection, bulletSize, hit.rigidbody != null);
            }

            GameObject target = GetTarget(hit);
            
            if (target == null)
            {
                return;
            }

            GameObject source = GetSource(damageSource);

            // Не используем встроенный damage pipeline NeoFPS. Урон идет только через наш DamageSystem.
            DamageContext context = BuildDamageContext(hit, rayDirection, target, source);

            DamageSystemUtility.ApplyDamage(target, context);
        }

        private DamageContext BuildDamageContext(RaycastHit hit, Vector3 rayDirection, GameObject target, GameObject source)
        {
            DamageContext bodyContext = new DamageContext(
                damage,
                HealthDamageType.Bullet,
                source,
                false,
                HitZone.Body);

            EnemyHeadshotProfile headshotProfile = target.GetComponentInParent<EnemyHeadshotProfile>();
            if (headshotProfile == null || !headshotProfile.IsHeadshotEnabled)
            {
                return bodyContext;
            }

            if (!IsHeadshot(hit, rayDirection, headshotProfile))
            {
                return bodyContext;
            }

            Health health = target.GetComponentInParent<Health>();
            return headshotProfile.BuildHeadshotContext(bodyContext, health);
        }

        private bool IsHeadshot(RaycastHit hit, Vector3 rayDirection, EnemyHeadshotProfile headshotProfile)
        {
            if (hit.collider == null)
            {
                return false;
            }

            if (hit.collider.GetComponent<EnemyHeadHitbox>() != null)
            {
                return true;
            }

            EnemyHeadHitbox[] headHitboxes = headshotProfile.GetComponentsInChildren<EnemyHeadHitbox>(true);
            if (headHitboxes.Length == 0)
            {
                return false;
            }

            Vector3 direction = rayDirection.sqrMagnitude > 0.0001f
                ? rayDirection.normalized
                : Vector3.forward;

            Vector3 rayOrigin = hit.point - direction * Mathf.Max(hit.distance, 0f);
            Ray ray = new Ray(rayOrigin, direction);
            float maxDistance = hit.distance + Mathf.Max(0f, headshotOverlapTolerance);

            for (int i = 0; i < headHitboxes.Length; ++i)
            {
                Collider headCollider = headHitboxes[i].HitCollider;
                if (headCollider == null || !headCollider.enabled)
                {
                    continue;
                }

                if (headCollider.Raycast(ray, out RaycastHit headHit, maxDistance))
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject GetTarget(RaycastHit hit)
        {
            if (hit.collider != null)
            {
                
                return hit.collider.gameObject;
            }

            if (hit.rigidbody != null)
            {
                return hit.rigidbody.gameObject;
            }

            return null;
        }

        private GameObject GetSource(IDamageSource damageSource)
        {
            Transform sourceTransform = damageSource?.GetOriginalSourceTransform();
            if (sourceTransform != null)
            {
                return sourceTransform.gameObject;
            }

            return gameObject;
        }
    }
}
