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
        private FpsInputAxis m_HorizontalAxis = FpsInputAxis.MouseX;

        [SerializeField]
        [Tooltip("Ось NeoFPS, в которую записывается вертикальное смещение touch для поворота камеры.")]
        private FpsInputAxis m_VerticalAxis = FpsInputAxis.MouseY;

        [SerializeField]
        [Tooltip("Множитель чувствительности touch-drag для обзора камеры.")]
        private float m_Multiplier = 0.12f;

        [Header("Исключения")]

        [SerializeField]
        [Tooltip("UI-зоны, начатые в которых touch не должны передавать look-input до завершения касания.")]
        private RectTransform[] m_ExcludedTouchZones = new RectTransform[0];

        private readonly HashSet<int> trackedFingerIds = new HashSet<int>();
        private readonly HashSet<int> blockedFingerIds = new HashSet<int>();

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
            if (trackedFingerIds.Add(touch.fingerId) && IsInExcludedTouchZone(touch.position))
            {
                // Блокируем весь drag, если палец начался на левом джойстике или другой исключённой зоне.
                blockedFingerIds.Add(touch.fingerId);
            }

            if (blockedFingerIds.Contains(touch.fingerId))
            {
                return true;
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
            blockedFingerIds.Clear();
        }

        private bool IsInExcludedTouchZone(Vector2 screenPosition)
        {
            for (int i = 0; i < m_ExcludedTouchZones.Length; i++)
            {
                RectTransform zone = m_ExcludedTouchZones[i];
                if (zone != null && RectTransformUtility.RectangleContainsScreenPoint(zone, screenPosition, uiCamera))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
