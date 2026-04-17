using Modules.HealthSystem;
using UnityEngine;

namespace Modules.NeoFPS_Adapter
{
    public enum EnemyHeadshotMode
    {
        Disabled = 0,
        InstantKill = 1,
        DamageMultiplier = 2
    }

    /// <summary>
    /// Конфигурация реакции врага на headshot без изменения vendor-кода.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHeadshotProfile : MonoBehaviour
    {
        [Header("Headshot")]
        [Tooltip("Режим реакции на попадание в head hitbox: отключено, мгновенная смерть или множитель урона.")]
        [SerializeField] private EnemyHeadshotMode headshotMode = EnemyHeadshotMode.InstantKill;
        [Tooltip("Множитель урона для режима DamageMultiplier. В режиме InstantKill не используется.")]
        [SerializeField, Min(1f)] private float headshotDamageMultiplier = 2f;

        public bool IsHeadshotEnabled => headshotMode != EnemyHeadshotMode.Disabled;

        public DamageContext BuildHeadshotContext(in DamageContext bodyContext, Health health)
        {
            int finalDamage = bodyContext.Amount;

            switch (headshotMode)
            {
                case EnemyHeadshotMode.InstantKill:
                    finalDamage = health != null && health.CurrentHealth > 0
                        ? health.CurrentHealth
                        : Mathf.Max(bodyContext.Amount, 1);
                    break;

                case EnemyHeadshotMode.DamageMultiplier:
                    finalDamage = Mathf.Max(
                        bodyContext.Amount,
                        Mathf.RoundToInt(bodyContext.Amount * headshotDamageMultiplier));
                    break;
            }

            return new DamageContext(
                finalDamage,
                bodyContext.DamageType,
                bodyContext.Source,
                true,
                HitZone.Head);
        }
    }
}
