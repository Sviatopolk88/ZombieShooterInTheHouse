using UnityEngine;

namespace Modules.HealthSystem
{
    public readonly struct DamageContext
    {
        public DamageContext(
            int amount,
            DamageType damageType = DamageType.Default,
            GameObject source = null,
            bool isCritical = false,
            HitZone hitZone = HitZone.None)
        {
            Amount = amount;
            DamageType = damageType;
            Source = source;
            IsCritical = isCritical;
            HitZone = hitZone;
        }

        public int Amount { get; }
        public DamageType DamageType { get; }
        public GameObject Source { get; }
        public bool IsCritical { get; }
        public HitZone HitZone { get; }

        public DamageContext WithAmount(int amount)
        {
            return new DamageContext(amount, DamageType, Source, IsCritical, HitZone);
        }
    }
}
