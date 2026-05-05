using UnityEngine;

namespace _Project.Scripts.Gameplay.Interactive
{
    [DisallowMultipleComponent]
    public sealed class DoorBreachAudioBridge : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Источник звука, через который воспроизводятся удары по двери и финальное открытие.")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("Клип, который проигрывается в момент начала выбивания двери.")]
        private AudioClip impactClip;

        [SerializeField]
        [Tooltip("Клип, который проигрывается после финального открытия или прорыва двери.")]
        private AudioClip breachOpenClip;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnValidate()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void PlayImpact()
        {
            PlayClip(impactClip);
        }

        public void PlayBreachOpen()
        {
            PlayClip(breachOpenClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }
    }
}
