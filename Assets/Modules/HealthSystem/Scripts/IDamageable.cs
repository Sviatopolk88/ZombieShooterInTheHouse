namespace Modules.HealthSystem
{
    public interface ITargetableEntity
    {
        bool IsValidEnemyTarget { get; }
    }

    public interface IDamageable
    {
        bool CanTakeDamage { get; }
        bool CanApplyDamage(in DamageContext context);
        void TakeDamage(int amount);
        bool TakeDamage(DamageContext context);
    }
}
