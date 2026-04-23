using Modules.HealthSystem;
using Modules.NeoFPS_Adapter;
using NeoFPS;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI.HUD
{
    /// <summary>
    /// Показывает экранную реакцию HUD на фактически примененный урон игроку.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class PlayerDamageFeedbackPresenter : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("Изображение полноэкранного красного оверлея, которое кратко появляется при получении урона игроком.")]
        private Image overlayImage;

        [SerializeField, Tooltip("Максимальная прозрачность красного оверлея в момент получения урона.")]
        [Range(0f, 1f)] private float peakAlpha = 0.85f;

        [SerializeField, Tooltip("Время, в течение которого оверлей остается на максимальной прозрачности после удара.")]
        [Min(0f)] private float holdDuration = 0.04f;

        [SerializeField, Tooltip("Время плавного исчезновения красного оверлея после получения урона.")]
        [Min(0.01f)] private float fadeDuration = 0.35f;

        private Health playerHealth;
        private float feedbackTimer;

        protected override void Awake()
        {
            base.Awake();

            if (overlayImage == null)
            {
                overlayImage = GetComponent<Image>();
            }

            if (overlayImage != null)
            {
                overlayImage.raycastTarget = false;
            }

            SetOverlayAlpha(0f);
        }

        private void Update()
        {
            if (overlayImage == null || feedbackTimer <= 0f)
            {
                return;
            }

            feedbackTimer = Mathf.Max(0f, feedbackTimer - Time.unscaledDeltaTime);

            float alpha;
            if (feedbackTimer > fadeDuration)
            {
                alpha = peakAlpha;
            }
            else
            {
                alpha = peakAlpha * (feedbackTimer / fadeDuration);
            }

            SetOverlayAlpha(alpha);
        }

        protected override void OnDestroy()
        {
            DetachFromHealth();
            base.OnDestroy();
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            DetachFromHealth();

            Component characterComponent = character as Component;
            if (characterComponent == null)
            {
                return;
            }

            NeoFPS_PlayerAdapter playerAdapter = characterComponent.GetComponentInChildren<NeoFPS_PlayerAdapter>(true);
            if (playerAdapter != null)
            {
                playerHealth = playerAdapter.GetHealth();
            }

            if (playerHealth == null)
            {
                playerHealth = characterComponent.GetComponentInChildren<Health>(true);
            }

            if (playerHealth == null)
            {
                return;
            }

            playerHealth.OnDamageApplied += OnDamageApplied;
        }

        private void OnDamageApplied(DamageContext context, int appliedDamage)
        {
            PlayDamageFeedback();
        }

        private void PlayDamageFeedback()
        {
            feedbackTimer = holdDuration + fadeDuration;
            SetOverlayAlpha(peakAlpha);
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (overlayImage == null)
            {
                return;
            }

            Color color = overlayImage.color;
            color.a = Mathf.Clamp01(alpha);
            overlayImage.color = color;
        }

        private void DetachFromHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.OnDamageApplied -= OnDamageApplied;
            playerHealth = null;
        }
    }
}
