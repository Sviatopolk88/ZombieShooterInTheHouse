using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Modules.DoorBreachEncounter
{
    public enum DoorBreachOpenMode
    {
        ToggleObjects = 0,
        RotateLocalY = 1
    }

    public enum DoorBreachState
    {
        Idle = 0,
        BreachSequence = 1,
        Breached = 2
    }

    /// <summary>
    /// Скриптовый encounter прорыва двери.
    /// Не является общей системой урона по дверям: управляет только постановочной последовательностью ударов и открытия.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorBreachEncounter : MonoBehaviour
    {
        [Header("Door Setup")]
        [Tooltip("Transform двери или hinge-root, который будет трястись при ударах.")]
        [SerializeField] private Transform shakeTarget;
        [Tooltip("Объект закрытой двери. Используется в режиме переключения состояний и при сбросе encounter.")]
        [SerializeField] private GameObject closedDoorObject;
        [Tooltip("Опциональный объект открытой или разрушенной версии двери. Включается после прорыва в режиме переключения состояний.")]
        [SerializeField] private GameObject openedDoorObject;
        [Tooltip("Опциональный блокер прохода или NavMesh-препятствие, которое отключается после открытия двери.")]
        [SerializeField] private GameObject blockingObject;

        [Header("Open")]
        [Tooltip("Способ финального открытия двери: переключение объектов или поворот по локальной оси Y.")]
        [SerializeField] private DoorBreachOpenMode openMode = DoorBreachOpenMode.ToggleObjects;
        [Tooltip("Transform, который будет открываться поворотом. Если не задан, используется shakeTarget.")]
        [SerializeField] private Transform openTarget;
        [Tooltip("Длительность быстрого открытия двери в секундах при режиме RotateLocalY.")]
        [SerializeField, Min(0.01f)] private float openDuration = 0.18f;
        [Tooltip("Итоговый локальный угол Y в открытом состоянии. Оси X и Z сохраняются из исходного поворота.")]
        [SerializeField] private float openedLocalY = -146f;

        [Header("Flow")]
        [Tooltip("Количество ударов по двери перед финальным прорывом.")]
        [SerializeField, Min(1)] private int hitCount = 3;
        [Tooltip("Начальная задержка перед первым ударом после активации encounter.")]
        [SerializeField, Min(0f)] private float initialDelay = 0.25f;
        [Tooltip("Интервал между ударами по двери.")]
        [SerializeField, Min(0f)] private float intervalBetweenHits = 0.7f;
        [Tooltip("Пауза между последним ударом и началом финального открытия двери.")]
        [SerializeField, Min(0f)] private float delayBeforeBreach = 0.15f;

        [Header("Shake")]
        [Tooltip("Максимальное локальное смещение двери на одном ударе по осям X/Y/Z.")]
        [SerializeField] private Vector3 localPositionShake = new Vector3(0.03f, 0.015f, 0.05f);
        [Tooltip("Максимальный локальный поворот двери на одном ударе по осям X/Y/Z.")]
        [SerializeField] private Vector3 localRotationShake = new Vector3(2f, 3f, 1f);
        [Tooltip("Длительность одного удара с возвратом двери в исходное положение.")]
        [SerializeField, Min(0.01f)] private float singleHitDuration = 0.16f;

        [Header("Activation Targets")]
        [Tooltip("Список scene-объектов, которые должны активироваться после прорыва двери.")]
        [SerializeField] private GameObject[] activateOnBreach = new GameObject[0];
        [Tooltip("Если включено, объекты из activateOnBreach будут автоматически выключаться при OnEnable и сбросе encounter.")]
        [SerializeField] private bool deactivateTargetsOnEnable = true;

        [Header("Events")]
        [Tooltip("Событие вызывается в момент старта последовательности ударов по двери.")]
        [SerializeField] private UnityEvent onSequenceStarted;
        [Tooltip("Событие вызывается после финального открытия двери и активации связанных объектов.")]
        [SerializeField] private UnityEvent onBreach;

        private DoorBreachState state = DoorBreachState.Idle;
        private bool activated;
        private Coroutine breachRoutine;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private bool cachedShakeTransform;
        private Quaternion initialOpenLocalRotation;
        private bool cachedOpenTransform;
        private bool cachedClosedDoorState;
        private bool cachedOpenedDoorState;
        private bool cachedBlockingState;
        private bool cachedObjectStates;

        public DoorBreachState State => state;
        public bool IsActivated => activated;

        private void Awake()
        {
            CacheInitialStates();
        }

        private void OnEnable()
        {
            ResetEncounterState();
        }

        private void OnDisable()
        {
            if (breachRoutine != null)
            {
                StopCoroutine(breachRoutine);
                breachRoutine = null;
            }

            RestoreShakeTarget();
            RestoreOpenTarget();
        }

        public bool Activate()
        {
            if (activated || state != DoorBreachState.Idle)
            {
                return false;
            }

            activated = true;
            breachRoutine = StartCoroutine(RunEncounter());
            return true;
        }

        public void ForceBreach()
        {
            if (state == DoorBreachState.Breached)
            {
                return;
            }

            if (breachRoutine != null)
            {
                StopCoroutine(breachRoutine);
                breachRoutine = null;
            }

            activated = true;
            ApplyBreachedStateImmediate();
        }

        public void ResetEncounter()
        {
            ResetEncounterState();
        }

        private IEnumerator RunEncounter()
        {
            state = DoorBreachState.BreachSequence;
            onSequenceStarted?.Invoke();

            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            int totalHits = Mathf.Max(1, hitCount);

            for (int i = 0; i < totalHits; i++)
            {
                yield return PlayHitShake();

                if (i < totalHits - 1 && intervalBetweenHits > 0f)
                {
                    yield return new WaitForSeconds(intervalBetweenHits);
                }
            }

            if (delayBeforeBreach > 0f)
            {
                yield return new WaitForSeconds(delayBeforeBreach);
            }

            yield return ApplyBreachedStateRoutine();
            breachRoutine = null;
        }

        private IEnumerator PlayHitShake()
        {
            if (shakeTarget == null)
            {
                yield break;
            }

            Vector3 positionOffset = BuildRandomOffset(localPositionShake);
            Vector3 rotationOffset = BuildRandomOffset(localRotationShake);

            float duration = Mathf.Max(0.01f, singleHitDuration);
            float halfDuration = duration * 0.5f;

            for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / halfDuration);
                SetShakePose(
                    Vector3.LerpUnclamped(initialLocalPosition, initialLocalPosition + positionOffset, t),
                    Quaternion.Euler(Vector3.LerpUnclamped(Vector3.zero, rotationOffset, t)));
                yield return null;
            }

            for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / halfDuration);
                SetShakePose(
                    Vector3.LerpUnclamped(initialLocalPosition + positionOffset, initialLocalPosition, t),
                    Quaternion.Euler(Vector3.LerpUnclamped(rotationOffset, Vector3.zero, t)));
                yield return null;
            }

            RestoreShakeTarget();
        }

        private IEnumerator ApplyBreachedStateRoutine()
        {
            state = DoorBreachState.Breached;
            RestoreShakeTarget();

            if (ShouldRotateOpen())
            {
                yield return RotateDoorToOpenedPose();
            }
            else
            {
                ApplyObjectSwitchOpenState();
            }

            FinalizeBreachedState();
        }

        private void ApplyBreachedStateImmediate()
        {
            state = DoorBreachState.Breached;
            RestoreShakeTarget();

            if (ShouldRotateOpen())
            {
                SnapDoorToOpenedPose();
            }
            else
            {
                ApplyObjectSwitchOpenState();
            }

            FinalizeBreachedState();
        }

        private void ResetEncounterState()
        {
            CacheInitialStates();

            if (breachRoutine != null)
            {
                StopCoroutine(breachRoutine);
                breachRoutine = null;
            }

            activated = false;
            state = DoorBreachState.Idle;

            RestoreShakeTarget();
            RestoreOpenTarget();

            if (closedDoorObject != null)
            {
                closedDoorObject.SetActive(cachedClosedDoorState);
            }

            if (openedDoorObject != null)
            {
                openedDoorObject.SetActive(cachedOpenedDoorState);
            }

            if (blockingObject != null)
            {
                blockingObject.SetActive(cachedBlockingState);
            }

            if (deactivateTargetsOnEnable)
            {
                SetActivationTargetsState(false);
            }
        }

        private void CacheInitialStates()
        {
            if (!cachedShakeTransform && shakeTarget != null)
            {
                initialLocalPosition = shakeTarget.localPosition;
                initialLocalRotation = shakeTarget.localRotation;
                cachedShakeTransform = true;
            }

            if (!cachedOpenTransform)
            {
                Transform resolvedOpenTarget = GetResolvedOpenTarget();
                if (resolvedOpenTarget != null)
                {
                    initialOpenLocalRotation = resolvedOpenTarget.localRotation;
                    cachedOpenTransform = true;
                }
            }

            if (!cachedObjectStates)
            {
                cachedClosedDoorState = closedDoorObject == null || closedDoorObject.activeSelf;
                cachedOpenedDoorState = openedDoorObject != null && openedDoorObject.activeSelf;
                cachedBlockingState = blockingObject == null || blockingObject.activeSelf;
                cachedObjectStates = true;
            }
        }

        private void SetActivationTargetsState(bool value)
        {
            if (activateOnBreach == null)
            {
                return;
            }

            for (int i = 0; i < activateOnBreach.Length; i++)
            {
                GameObject target = activateOnBreach[i];
                if (target != null)
                {
                    target.SetActive(value);
                }
            }
        }

        private void RestoreShakeTarget()
        {
            if (shakeTarget == null)
            {
                return;
            }

            shakeTarget.localPosition = initialLocalPosition;
            shakeTarget.localRotation = initialLocalRotation;
        }

        private void RestoreOpenTarget()
        {
            Transform resolvedOpenTarget = GetResolvedOpenTarget();
            if (resolvedOpenTarget == null || !cachedOpenTransform)
            {
                return;
            }

            resolvedOpenTarget.localRotation = initialOpenLocalRotation;
        }

        private void SetShakePose(Vector3 localPosition, Quaternion localRotationOffset)
        {
            if (shakeTarget == null)
            {
                return;
            }

            shakeTarget.localPosition = localPosition;
            shakeTarget.localRotation = initialLocalRotation * localRotationOffset;
        }

        private static Vector3 BuildRandomOffset(Vector3 amplitude)
        {
            return new Vector3(
                Random.Range(-amplitude.x, amplitude.x),
                Random.Range(-amplitude.y, amplitude.y),
                Random.Range(-amplitude.z, amplitude.z));
        }

        private bool ShouldRotateOpen()
        {
            return openMode == DoorBreachOpenMode.RotateLocalY && GetResolvedOpenTarget() != null;
        }

        private Transform GetResolvedOpenTarget()
        {
            return openTarget != null ? openTarget : shakeTarget;
        }

        private Quaternion GetOpenedLocalRotation()
        {
            Vector3 initialEuler = initialOpenLocalRotation.eulerAngles;
            return Quaternion.Euler(initialEuler.x, openedLocalY, initialEuler.z);
        }

        private IEnumerator RotateDoorToOpenedPose()
        {
            Transform resolvedOpenTarget = GetResolvedOpenTarget();
            if (resolvedOpenTarget == null)
            {
                yield break;
            }

            Quaternion startRotation = initialOpenLocalRotation;
            Quaternion targetRotation = GetOpenedLocalRotation();
            float duration = Mathf.Max(0.01f, openDuration);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                resolvedOpenTarget.localRotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, easedT);
                yield return null;
            }

            resolvedOpenTarget.localRotation = targetRotation;
        }

        private void SnapDoorToOpenedPose()
        {
            Transform resolvedOpenTarget = GetResolvedOpenTarget();
            if (resolvedOpenTarget == null)
            {
                return;
            }

            resolvedOpenTarget.localRotation = GetOpenedLocalRotation();
        }

        private void ApplyObjectSwitchOpenState()
        {
            if (closedDoorObject != null)
            {
                closedDoorObject.SetActive(false);
            }

            if (openedDoorObject != null)
            {
                openedDoorObject.SetActive(true);
            }
        }

        private void FinalizeBreachedState()
        {
            if (blockingObject != null)
            {
                blockingObject.SetActive(false);
            }

            SetActivationTargetsState(true);
            onBreach?.Invoke();
        }
    }
}
