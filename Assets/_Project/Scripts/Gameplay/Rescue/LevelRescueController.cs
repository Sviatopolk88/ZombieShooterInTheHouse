using System;
using System.Collections.Generic;
using Modules.RescueObjective;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Gameplay.Rescue
{
    /// <summary>
    /// Project-side контроллер статистики спасения на текущем уровне.
    /// Собирает все RescueObjective, считает прогресс и отдаёт данные в UI / GameFlow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelRescueController : MonoBehaviour
    {
        public static LevelRescueController Active { get; private set; }
        public static event Action<LevelRescueController> ActiveControllerChanged;

        private readonly List<RescueObjective> objectives = new();
        private readonly Dictionary<RescueObjective, RescueObjectiveState> objectiveStates = new();

        public event Action CountsChanged;

        public int TotalCount { get; private set; }
        public int RescuedCount { get; private set; }
        public int FailedCount { get; private set; }
        public int RemainingCount { get; private set; }

        private void Awake()
        {
            CollectObjectives();
        }

        private void OnEnable()
        {
            Active = this;
            ActiveControllerChanged?.Invoke(this);
            NotifyCountsChanged();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
                ActiveControllerChanged?.Invoke(null);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromObjectives();
        }

        public void RefreshObjectives()
        {
            CollectObjectives();
            NotifyCountsChanged();
        }

        private void CollectObjectives()
        {
            UnsubscribeFromObjectives();
            objectives.Clear();
            objectiveStates.Clear();

            var foundObjectives = FindObjectsByType<RescueObjective>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var activeScene = gameObject.scene;

            foreach (var objective in foundObjectives)
            {
                if (objective == null)
                {
                    continue;
                }

                if (objective.gameObject.scene != activeScene)
                {
                    continue;
                }

                objectives.Add(objective);
                objectiveStates[objective] = objective.State;
                objective.StateChanged += OnObjectiveStateChanged;
            }

            RecalculateCounts();
        }

        private void UnsubscribeFromObjectives()
        {
            foreach (var objective in objectives)
            {
                if (objective != null)
                {
                    objective.StateChanged -= OnObjectiveStateChanged;
                }
            }
        }

        private void OnObjectiveStateChanged(RescueObjective objective, RescueObjectiveState previousState, RescueObjectiveState newState)
        {
            if (objective == null)
            {
                return;
            }

            if (!objectiveStates.TryGetValue(objective, out var currentState))
            {
                return;
            }

            if (currentState == newState)
            {
                return;
            }

            objectiveStates[objective] = newState;
            RecalculateCounts();
            NotifyCountsChanged();
        }

        private void RecalculateCounts()
        {
            TotalCount = objectiveStates.Count;
            RescuedCount = 0;
            FailedCount = 0;
            RemainingCount = 0;

            foreach (var state in objectiveStates.Values)
            {
                switch (state)
                {
                    case RescueObjectiveState.Rescued:
                        ++RescuedCount;
                        break;
                    case RescueObjectiveState.Failed:
                        ++FailedCount;
                        break;
                    default:
                        ++RemainingCount;
                        break;
                }
            }
        }

        private void NotifyCountsChanged()
        {
            CountsChanged?.Invoke();
        }
    }
}
