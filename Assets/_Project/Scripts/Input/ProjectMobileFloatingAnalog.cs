using NeoFPS;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Scripts.Input
{
    /// <summary>
    /// Project-side analog stick для NeoFPS: зона активации остаётся на месте,
    /// а визуал джойстика переносится в точку первого касания.
    /// </summary>
    public sealed class ProjectMobileFloatingAnalog : BaseTouchControl
    {
        [Header("Оси движения")]

        [SerializeField]
        [Tooltip("Ось NeoFPS, в которую записывается горизонтальное смещение джойстика.")]
        private FpsInputAxis m_HorizontalAxis = FpsInputAxis.MoveX;

        [SerializeField]
        [Tooltip("Ось NeoFPS, в которую записывается вертикальное смещение джойстика.")]
        private FpsInputAxis m_VerticalAxis = FpsInputAxis.MoveY;

        [SerializeField]
        [Tooltip("Кривая силы ввода: X — нормализованная дистанция от точки первого касания, Y — итоговая сила оси.")]
        private AnimationCurve m_InputCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Визуализация")]

        [SerializeField]
        [Tooltip("Корневой RectTransform визуала джойстика, который переносится в точку первого касания.")]
        private RectTransform m_JoystickRootTransform = null;

        [SerializeField]
        [Tooltip("Маркер положения стика, который смещается внутри визуала джойстика.")]
        private RectTransform m_TouchMarkerTransform = null;

        [SerializeField]
        [Tooltip("Максимальная дистанция смещения маркера стика внутри визуала.")]
        private float m_MaxMarkerDistance = 80f;

        [SerializeField]
        [Tooltip("Радиус в пикселях, на котором ввод достигает полной силы. Если 0, используется половина ширины визуала джойстика.")]
        private float m_InputRadius = 0f;

        [Header("События")]

        [SerializeField]
        [Tooltip("Событие при первом касании джойстика.")]
        private UnityEvent m_OnTouchStarted = null;

        [SerializeField]
        [Tooltip("Событие при завершении касания джойстика.")]
        private UnityEvent m_OnTouchEnded = null;

        private Camera uiCamera;
        private Vector2 startScreenPosition;
        private Vector2 initialJoystickRootPosition;
        private int activeFingerId = -1;
        private bool hasActiveTouch;

        private void OnValidate()
        {
            m_InputCurve.preWrapMode = WrapMode.Clamp;
            m_InputCurve.postWrapMode = WrapMode.Clamp;
            m_MaxMarkerDistance = Mathf.Max(0f, m_MaxMarkerDistance);
            m_InputRadius = Mathf.Max(0f, m_InputRadius);
        }

        protected override void Awake()
        {
            base.Awake();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            if (m_JoystickRootTransform != null)
            {
                initialJoystickRootPosition = m_JoystickRootTransform.anchoredPosition;
            }

            if (m_TouchMarkerTransform != null)
            {
                m_TouchMarkerTransform.gameObject.SetActive(false);
            }
        }

        public override bool HandleTouch(Touch touch)
        {
            if (!hasActiveTouch)
            {
                BeginTouch(touch);
            }

            if (touch.fingerId != activeFingerId)
            {
                return consume;
            }

            Vector2 offset = touch.position - startScreenPosition;
            Vector2 normalised = GetNormalisedInput(offset);

            if (normalised.sqrMagnitude > 0.0001f)
            {
                controller.axes[m_HorizontalAxis] += normalised.x;
                controller.axes[m_VerticalAxis] += normalised.y;
            }

            if (m_TouchMarkerTransform != null)
            {
                m_TouchMarkerTransform.anchoredPosition = normalised * m_MaxMarkerDistance;
            }

            return consume;
        }

        protected override void OnTouchStarted()
        {
            m_OnTouchStarted?.Invoke();

            if (m_TouchMarkerTransform != null)
            {
                m_TouchMarkerTransform.gameObject.SetActive(true);
            }
        }

        protected override void OnTouchEnded()
        {
            hasActiveTouch = false;
            activeFingerId = -1;

            if (m_JoystickRootTransform != null)
            {
                m_JoystickRootTransform.anchoredPosition = initialJoystickRootPosition;
            }

            if (m_TouchMarkerTransform != null)
            {
                m_TouchMarkerTransform.anchoredPosition = Vector2.zero;
                m_TouchMarkerTransform.gameObject.SetActive(false);
            }

            m_OnTouchEnded?.Invoke();
        }

        private void BeginTouch(Touch touch)
        {
            hasActiveTouch = true;
            activeFingerId = touch.fingerId;
            startScreenPosition = touch.position;

            if (m_JoystickRootTransform != null)
            {
                MoveJoystickRootToTouch(touch.position);
            }

            if (m_TouchMarkerTransform != null)
            {
                m_TouchMarkerTransform.anchoredPosition = Vector2.zero;
            }
        }

        private Vector2 GetNormalisedInput(Vector2 offset)
        {
            float inputRadius = GetInputRadius();
            if (inputRadius <= 0.01f)
            {
                return Vector2.zero;
            }

            Vector2 normalised = offset / inputRadius;
            float magnitude = normalised.magnitude;
            if (magnitude <= 0.01f)
            {
                return Vector2.zero;
            }

            return normalised * (m_InputCurve.Evaluate(magnitude) / magnitude);
        }

        private float GetInputRadius()
        {
            if (m_InputRadius > 0f)
            {
                return m_InputRadius;
            }

            if (m_JoystickRootTransform != null)
            {
                return Mathf.Min(m_JoystickRootTransform.rect.width, m_JoystickRootTransform.rect.height) * 0.5f;
            }

            return rectTransform.rect.width * 0.5f;
        }

        private void MoveJoystickRootToTouch(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPoint))
            {
                return;
            }

            RectTransform parent = m_JoystickRootTransform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            Vector2 anchorReference = rectTransform.rect.min + Vector2.Scale(rectTransform.rect.size, m_JoystickRootTransform.anchorMin);
            Vector2 pivotOffset = Vector2.Scale(m_JoystickRootTransform.rect.size, m_JoystickRootTransform.pivot - new Vector2(0.5f, 0.5f));
            m_JoystickRootTransform.anchoredPosition = localPoint + pivotOffset - anchorReference;
        }
    }
}
