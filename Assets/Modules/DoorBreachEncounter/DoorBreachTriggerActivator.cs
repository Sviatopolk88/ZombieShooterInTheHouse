using UnityEngine;

namespace Modules.DoorBreachEncounter
{
    /// <summary>
    /// Опциональный helper для запуска scripted breach encounter через trigger.
    /// Сам модуль можно активировать и внешним вызовом DoorBreachEncounter.Activate().
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DoorBreachTriggerActivator : MonoBehaviour
    {
        [Tooltip("Encounter, который будет запущен при входе подходящего объекта в trigger.")]
        [SerializeField] private DoorBreachEncounter encounter;
        [Tooltip("Слои объектов, которым разрешено активировать encounter.")]
        [SerializeField] private LayerMask activatorLayers = ~0;
        [Tooltip("Необязательный тег активатора. Если заполнен, trigger сработает только для объектов с этим тегом.")]
        [SerializeField] private string requiredTag;
        [Tooltip("Если включено, trigger сможет запустить encounter только один раз за жизненный цикл объекта.")]
        [SerializeField] private bool triggerOnce = true;

        private bool triggered;

        private void Reset()
        {
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null)
            {
                ownCollider.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            triggered = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || encounter == null)
            {
                return;
            }

            if (MatchesFilter(other.gameObject))
            {
                Debug.Log(
                    $"DoorBreach: игрок вошёл в триггер '{name}' у encounter '{encounter.name}' (root: '{transform.root.name}').",
                    this);
            }

            if (triggerOnce && triggered)
            {
                return;
            }

            if (!MatchesFilter(other.gameObject))
            {
                return;
            }

            bool activated = encounter.Activate();
            if (activated && triggerOnce)
            {
                triggered = true;
            }
        }

        private bool MatchesFilter(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if ((activatorLayers.value & (1 << target.layer)) == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !target.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }
    }
}
