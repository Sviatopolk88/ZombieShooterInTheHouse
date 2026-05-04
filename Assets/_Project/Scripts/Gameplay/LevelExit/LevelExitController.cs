using System;
using System.Collections.Generic;
using _Project.Scripts.GameFlow;
using _Project.Scripts.Gameplay.Rescue;
using _Project.Scripts.Systems.SceneFlow;
using _Project.Scripts.UI;
using _Project.Scripts.UI.LevelComplete;
using Modules.HealthSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.LevelExit
{
    /// <summary>
    /// Project-side контроллер завершения уровня через явно заданную exit-зону.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelExitController : MonoBehaviour
    {
        [Header("Боссы")]
        [Tooltip("Корневые Transform боссов, смерть которых разблокирует выход.")]
        [SerializeField] private Transform[] bossRoots = Array.Empty<Transform>();

        [Header("Выход")]
        [Tooltip("Trigger-зона выхода. До смерти всех боссов она выключена, после победы включается.")]
        [SerializeField] private Collider exitTrigger;

        [Header("Визуал")]
        [Tooltip("Опциональные объекты, которые включаются после разблокировки выхода.")]
        [SerializeField] private GameObject[] activateOnUnlock = Array.Empty<GameObject>();

        [Tooltip("Опциональные объекты, которые выключаются после разблокировки выхода.")]
        [SerializeField] private GameObject[] deactivateOnUnlock = Array.Empty<GameObject>();

        [Header("Совместимость")]
        [Tooltip("Отключать старый WinConditionHandler в этой сцене, чтобы он не конфликтовал с новым flow.")]
        [SerializeField] private bool disableLegacyWinConditionHandler = true;

        private readonly List<Health> trackedBossHealths = new();
        private int aliveBossCount;
        private bool exitUnlocked;
        private bool completionRequested;

        public static LevelExitController Active { get; private set; }
        public IReadOnlyList<Health> BossHealths => trackedBossHealths;

        private void OnValidate()
        {
            if (exitTrigger != null)
            {
                exitTrigger.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                Debug.LogWarning("LevelExitController: duplicate instance detected, destroying component.", this);
                Destroy(this);
                return;
            }

            Active = this;

            if (disableLegacyWinConditionHandler)
            {
                DisableLegacyWinHandlers();
            }

            CacheBosses();
            SetupExitTrigger();
            ApplyLockedState();

            if (trackedBossHealths.Count == 0)
            {
                Debug.LogWarning("LevelExitController: no bosses assigned. Exit will stay locked until unlocked manually.", this);
            }
            else if (aliveBossCount <= 0)
            {
                UnlockExit();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromBosses();

            if (Active == this)
            {
                Active = null;
            }
        }

        public void TryExit(GameObject activator)
        {
            if (!exitUnlocked || completionRequested)
            {
                return;
            }

            LevelRescueController rescueController = GetRescueController();
            int remainingCount = rescueController != null ? rescueController.RemainingCount : 0;
            if (remainingCount > 0)
            {
                LevelExitConfirmationController confirmationController = LevelExitConfirmationController.EnsureInstance();
                if (confirmationController == null)
                {
                    Debug.LogWarning("LevelExitController: confirmation UI not found, exit attempt cancelled.", this);
                    completionRequested = false;
                    return;
                }

                completionRequested = true;
                confirmationController.Show(
                    remainingCount,
                    CompleteLevel,
                    CancelExitAttempt);
                return;
            }

            completionRequested = true;
            CompleteLevel();
        }

        public void UnlockExit()
        {
            if (exitUnlocked)
            {
                return;
            }

            exitUnlocked = true;

            if (exitTrigger != null)
            {
                exitTrigger.enabled = true;
            }

            SetObjectsActive(activateOnUnlock, true);
            SetObjectsActive(deactivateOnUnlock, false);
        }

        private void CacheBosses()
        {
            UnsubscribeFromBosses();
            trackedBossHealths.Clear();
            aliveBossCount = 0;

            for (int i = 0; i < bossRoots.Length; i++)
            {
                Transform bossRoot = bossRoots[i];
                if (bossRoot == null)
                {
                    Debug.LogWarning($"LevelExitController: boss slot {i} is empty.", this);
                    continue;
                }

                Health health = bossRoot.GetComponent<Health>();
                if (health == null)
                {
                    Debug.LogWarning($"LevelExitController: boss '{bossRoot.name}' has no Health component.", bossRoot);
                    continue;
                }

                trackedBossHealths.Add(health);
                health.OnDeath += OnBossDeath;

                if (!health.IsDead)
                {
                    ++aliveBossCount;
                }
            }
        }

        private void UnsubscribeFromBosses()
        {
            for (int i = 0; i < trackedBossHealths.Count; i++)
            {
                if (trackedBossHealths[i] != null)
                {
                    trackedBossHealths[i].OnDeath -= OnBossDeath;
                }
            }
        }

        private void SetupExitTrigger()
        {
            if (exitTrigger == null)
            {
                Debug.LogWarning("LevelExitController: exit trigger is not assigned.", this);
                return;
            }

            exitTrigger.isTrigger = true;
            LevelExitTriggerZone triggerZone = exitTrigger.GetComponent<LevelExitTriggerZone>();
            if (triggerZone == null)
            {
                Debug.LogWarning("LevelExitController: assigned exit trigger has no LevelExitTriggerZone component.", exitTrigger);
                return;
            }

            if (triggerZone.Controller != this)
            {
                Debug.LogWarning("LevelExitController: LevelExitTriggerZone must reference this controller explicitly in the inspector.", triggerZone);
            }
        }

        private void ApplyLockedState()
        {
            if (exitTrigger != null)
            {
                exitTrigger.enabled = false;
            }

            SetObjectsActive(activateOnUnlock, false);
        }

        private void SetObjectsActive(GameObject[] objects, bool value)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(value);
                }
            }
        }

        private void OnBossDeath()
        {
            aliveBossCount = Mathf.Max(0, aliveBossCount - 1);
            if (aliveBossCount == 0)
            {
                UnlockExit();
            }
        }

        private void CancelExitAttempt()
        {
            completionRequested = false;
        }

        private void CompleteLevel()
        {
            completionRequested = false;
            LevelExitConfirmationController.Instance?.HideImmediately();
            CursorStateService.Instance?.SetUiMode();

            LevelRescueController rescueController = GetRescueController();
            int totalCount = rescueController != null ? rescueController.TotalCount : 0;
            int rescuedCount = rescueController != null ? rescueController.RescuedCount : 0;
            int failedCount = rescueController != null ? rescueController.FailedCount : 0;
            int remainingCount = rescueController != null ? rescueController.RemainingCount : 0;

            LevelCompleteScreenController completeScreen = LevelCompleteScreenController.EnsureInstance();
            if (completeScreen != null)
            {
                completeScreen.ShowResult(true, rescuedCount, totalCount, failedCount, remainingCount);
                return;
            }

            Debug.LogWarning("LevelExitController: level complete screen not found, exit completion cancelled.", this);
            completionRequested = false;
            CursorStateService.Instance?.SetGameplayMode();
        }

        private void DisableLegacyWinHandlers()
        {
            WinConditionHandler[] handlers = FindObjectsByType<WinConditionHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i] != null && handlers[i].gameObject.scene == gameObject.scene && handlers[i].enabled)
                {
                    handlers[i].enabled = false;
                }
            }
        }

        private LevelRescueController GetRescueController()
        {
            if (LevelRescueController.Active != null && LevelRescueController.Active.gameObject.scene == gameObject.scene)
            {
                return LevelRescueController.Active;
            }

            LevelRescueController[] controllers = FindObjectsByType<LevelRescueController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null && controllers[i].gameObject.scene == gameObject.scene)
                {
                    return controllers[i];
                }
            }

            return null;
        }
    }
}
