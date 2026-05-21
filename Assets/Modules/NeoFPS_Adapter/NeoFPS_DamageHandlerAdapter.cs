using DamageSystemUtility = Modules.DamageSystem.DamageSystem;
using HealthDamageType = Modules.HealthSystem.DamageType;
using Modules.HealthSystem;
using NeoFPS;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    [RequireComponent(typeof(Collider))]
    public sealed class NeoFPS_DamageHandlerAdapter : MonoBehaviour, IDamageHandler
    {
        [Tooltip("Множитель входящего урона NeoFPS перед передачей в HealthSystem.")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        [Tooltip("Считать ли попадание критическим для HealthSystem.")]
        [SerializeField] private bool critical;

        [Tooltip("Зона попадания, которая передается в HealthSystem.")]
        [SerializeField] private HitZone hitZone = HitZone.Body;

        private Collider damageCollider;

        public IHealthManager healthManager => null;

        public DamageFilter inDamageFilter { get; set; } = DamageFilter.AllDamageAllTeams;

        private void Awake()
        {
            damageCollider = GetComponent<Collider>();
        }

        public DamageResult AddDamage(float damage)
        {
            Vector3 hitPoint = damageCollider != null ? damageCollider.bounds.center : transform.position;
            return ApplyDamage(damage, null, hitPoint);
        }

        public DamageResult AddDamage(float damage, RaycastHit hit)
        {
            return ApplyDamage(damage, null, hit.point);
        }

        public DamageResult AddDamage(float damage, IDamageSource source)
        {
            Vector3 hitPoint = damageCollider != null ? damageCollider.bounds.center : transform.position;
            return ApplyDamage(damage, source, hitPoint);
        }

        public DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source)
        {
            return ApplyDamage(damage, source, hit.point);
        }

        private DamageResult ApplyDamage(float damage, IDamageSource source, Vector3 hitPoint)
        {
            if (!enabled || damage <= 0f || !CanAcceptDamage(source))
            {
                ReportHit(source, hitPoint, DamageResult.Blocked, 0f);
                return DamageResult.Blocked;
            }

            if (!DamageSystemUtility.TryGetDamageable(gameObject, out IDamageable damageable) || !damageable.CanTakeDamage)
            {
                ReportHit(source, hitPoint, DamageResult.Ignored, 0f);
                return DamageResult.Ignored;
            }

            int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
            if (finalDamage <= 0)
            {
                ReportHit(source, hitPoint, DamageResult.Ignored, 0f);
                return DamageResult.Ignored;
            }

            DamageContext context = new DamageContext(
                finalDamage,
                HealthDamageType.Melee,
                ResolveSource(source),
                critical,
                hitZone);

            if (!damageable.CanApplyDamage(context))
            {
                ReportHit(source, hitPoint, DamageResult.Blocked, 0f);
                return DamageResult.Blocked;
            }

            bool applied = damageable.TakeDamage(context);
            DamageResult result = applied
                ? (critical ? DamageResult.Critical : DamageResult.Standard)
                : DamageResult.Ignored;

            if (applied)
            {
                source?.controller?.currentCharacter?.ReportTargetHit(critical);
            }

            ReportHit(source, hitPoint, result, applied ? finalDamage : 0f);
            return result;
        }

        private bool CanAcceptDamage(IDamageSource source)
        {
            return source == null || source.outDamageFilter.CollidesWith(inDamageFilter, FpsGameMode.friendlyFire);
        }

        private GameObject ResolveSource(IDamageSource source)
        {
            Transform sourceTransform = source?.GetOriginalSourceTransform();
            return sourceTransform != null ? sourceTransform.gameObject : gameObject;
        }

        private void ReportHit(IDamageSource source, Vector3 hitPoint, DamageResult result, float damage)
        {
            DamageEvents.ReportDamageHandlerHit(this, source, hitPoint, result, damage);
        }
    }
}
