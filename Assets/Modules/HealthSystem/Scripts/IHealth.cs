namespace Modules.HealthSystem
{
    public interface IHealth : IDamageable, IHealable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsDead { get; }
        bool IsAlive { get; }
        float NormalizedHealth { get; }
        DamageContext LastDamageContext { get; }
        void Kill();
        void ResetHealth();
        void SetMaxHealth(int value, bool fillHealth = true);
    }
}
