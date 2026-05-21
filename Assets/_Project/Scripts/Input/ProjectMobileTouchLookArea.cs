using System.Collections.Generic;
using NeoFPS;
using UnityEngine;

namespace _Project.Scripts.Input
{
    /// <summary>
    /// Project-side look-зона для мобильного NeoFPS input с исключениями для UI-областей.
    /// </summary>
    public sealed class ProjectMobileTouchLookArea : BaseTouchControl
    {
        [Header("Оси обзора")]

        [SerializeField]
        [Tooltip("Ось NeoFPS, в которую записывается горизонтальное смещение touch для поворота камеры.")]
        private FpsInputAxis m_HorizontalAxis = FpsInputAxis.LookX;

        [SerializeField]
        [Tooltip("Ось NeoFPS, в которую записывается вертикальное смещение touch для поворота камеры.")]
        private FpsInputAxis m_VerticalAxis = FpsInputAxis.LookY;

        [SerializeField]
        [Tooltip("Множитель чувствительности touch-drag для обзора камеры.")]
        private float m_Multiplier = 0.12f;

        [Header("Разрешённая look-зона")]

        [SerializeField]
        [Tooltip("Разрешить look-input только touch-ам, которые начались внутри явной правой зоны обзора.")]
        private bool m_UseAllowedLookZone = true;

        [SerializeField]
        [Tooltip("RectTransform правой зоны обзора. Если поле пустое, используется RectTransform этого объекта.")]
        private RectTransform m_AllowedLookZoneRect = null;

        [Header("Исключённые UI-зоны")]

        [SerializeField]
        [Tooltip("UI-зоны, начатые в которых touch не должны передавать look-input до завершения касания.")]
        private RectTransform[] m_ExcludedUIZones = new RectTransform[0];

        private readonly HashSet<int> trackedFingerIds = new HashSet<int>();
        private readonly HashSet<int> allowedFingerIds = new HashSet<int>();
        private readonly HashSet<int> ignoredFingerIds = new HashSet<int>();
        private readonly List<int> inactiveFingerIds = new List<int>();

        private Camera uiCamera;

        protected override void Awake()
        {
            base.Awake();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }
        }

        public override bool HandleTouch(Touch touch)
        {
            PruneInactiveFingerIds();

            if (trackedFingerIds.Add(touch.fingerId))
            {
                if (IsAllowedLookStart(touch.position) && !IsInExcludedUIZone(touch.position))
                {
                    allowedFingerIds.Add(touch.fingerId);
                }
                else
                {
                    // Не пишем look axes, но не consume-им touch, чтобы кнопки поверх зоны продолжали работать.
                    ignoredFingerIds.Add(touch.fingerId);
                }
            }

            if (!allowedFingerIds.Contains(touch.fingerId) || ignoredFingerIds.Contains(touch.fingerId))
            {
                return false;
            }

            controller.axes[m_HorizontalAxis] += touch.deltaPosition.x * m_Multiplier;
            controller.axes[m_VerticalAxis] += touch.deltaPosition.y * m_Multiplier;

            return consume;
        }

        protected override void OnTouchStarted()
        {
        }

        protected override void OnTouchEnded()
        {
            trackedFingerIds.Clear();
            allowedFingerIds.Clear();
            ignoredFingerIds.Clear();
            inactiveFingerIds.Clear();
        }

        private bool IsAllowedLookStart(Vector2 screenPosition)
        {
            if (!m_UseAllowedLookZone)
            {
                return true;
            }

            RectTransform allowedZone = m_AllowedLookZoneRect != null ? m_AllowedLookZoneRect : rectTransform;
            return allowedZone != null && RectTransformUtility.RectangleContainsScreenPoint(allowedZone, screenPosition, uiCamera);
        }

        private bool IsInExcludedUIZone(Vector2 screenPosition)
        {
            for (int i = 0; i < m_ExcludedUIZones.Length; i++)
            {
                RectTransform zone = m_ExcludedUIZones[i];
                if (zone != null && RectTransformUtility.RectangleContainsScreenPoint(zone, screenPosition, uiCamera))
                {
                    return true;
                }
            }

            return false;
        }

        private void PruneInactiveFingerIds()
        {
            inactiveFingerIds.Clear();

            foreach (int fingerId in trackedFingerIds)
            {
                if (!IsFingerActive(fingerId))
                {
                    inactiveFingerIds.Add(fingerId);
                }
            }

            for (int i = 0; i < inactiveFingerIds.Count; i++)
            {
                int fingerId = inactiveFingerIds[i];
                trackedFingerIds.Remove(fingerId);
                allowedFingerIds.Remove(fingerId);
                ignoredFingerIds.Remove(fingerId);
            }
        }

        private static bool IsFingerActive(int fingerId)
        {
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch activeTouch = UnityEngine.Input.GetTouch(i);
                if (activeTouch.fingerId == fingerId &&
                    activeTouch.phase != TouchPhase.Ended &&
                    activeTouch.phase != TouchPhase.Canceled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
