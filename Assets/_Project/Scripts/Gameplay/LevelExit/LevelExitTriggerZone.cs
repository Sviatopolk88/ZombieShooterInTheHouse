using UnityEngine;

namespace _Project.Scripts.Gameplay.LevelExit
{
    /// <summary>
    /// Trigger-зона выхода, которая делегирует попытку завершения уровня в LevelExitController.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LevelExitTriggerZone : MonoBehaviour
    {
        [Tooltip("Контроллер выхода, который будет вызван при входе игрока в trigger-зону.")]
        [SerializeField] private LevelExitController controller;

        [Tooltip("Какие слои могут активировать выход.")]
        [SerializeField] private LayerMask activatorLayers = ~0;

        [Tooltip("Обязательный тег объекта-активатора. Обычно Player.")]
        [SerializeField] private string requiredTag = "Player";

        public LevelExitController Controller => controller;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (controller == null || other == null)
            {
                return;
            }

            if (((1 << other.gameObject.layer) & activatorLayers.value) == 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            controller.TryExit(other.gameObject);
        }

        private void EnsureTriggerCollider()
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }
    }
}
