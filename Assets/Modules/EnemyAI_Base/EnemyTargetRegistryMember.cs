using UnityEngine;

namespace Modules.EnemyAI_Base
{
    /// <summary>
    /// Регистрирует объект как потенциальную цель для EnemyAI_Base.
    /// Если targetRoot не задан, используется transform этого объекта.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyTargetRegistryMember : MonoBehaviour
    {
        [SerializeField] private Transform targetRoot;

        private Transform registeredTarget;
        private string registeredTag;

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Register()
        {
            registeredTarget = targetRoot != null ? targetRoot : transform;
            registeredTag = registeredTarget != null ? registeredTarget.tag : null;

            if (string.IsNullOrWhiteSpace(registeredTag))
            {
                return;
            }

            EnemyTargetRegistry.Register(registeredTag, registeredTarget);
        }

        private void Unregister()
        {
            if (registeredTarget == null || string.IsNullOrWhiteSpace(registeredTag))
            {
                return;
            }

            EnemyTargetRegistry.Unregister(registeredTag, registeredTarget);
            registeredTarget = null;
            registeredTag = null;
        }
    }
}
