using UnityEngine;
using UnityEngine.AI;

namespace Modules.EnemyAI_Base.Movement
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [Header("Speed")]
        [Tooltip("Средняя скорость движения, вокруг которой строятся все вариации шага.")]
        [SerializeField] private float baseSpeed = 2.5f;
        [Tooltip("Максимальная случайная добавка или убавка к базовой скорости движения.")]
        [SerializeField] private float speedVariation = 0.5f;
        [Tooltip("Частота синусоидального покачивания скорости, чтобы шаг выглядел более живым.")]
        [SerializeField] private float wobbleFrequency = 3f;
        [Tooltip("Сила синусоидального покачивания скорости движения.")]
        [SerializeField] private float wobbleAmplitude = 0.3f;

        [Header("Twitch")]
        [Tooltip("Вероятность короткого сбоя шага в секунду, когда зомби внезапно подламывается.")]
        [SerializeField] private float twitchChance = 0.2f;
        [Tooltip("Длительность такого краткого сбоя или подламывания шага.")]
        [SerializeField] private float twitchDuration = 0.2f;
        [Tooltip("Во сколько раз замедляется враг во время twitch-фазы.")]
        [SerializeField] private float twitchSlowMultiplier = 0.2f;

        private NavMeshAgent agent;
        private float twitchEndTime;
        private bool isTwitching;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (agent == null)
            {
                return;
            }

            UpdateSpeed();
        }

        public void MoveTo(Vector3 position)
        {
            if (agent == null)
            {
                return;
            }

            agent.SetDestination(position);
        }

        public void SetBaseSpeed(float speed)
        {
            baseSpeed = Mathf.Max(0.1f, speed);
        }

        private void UpdateSpeed()
        {
            // Синус даёт базовое покачивание скорости, чтобы шаг выглядел неровным.
            float wobble = Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude;

            // Небольшой случайный шум делает движение менее предсказуемым.
            float randomOffset = Random.Range(-speedVariation, speedVariation) * 0.5f;

            float speed = baseSpeed + wobble + randomOffset;

            // Иногда зомби резко "подламывается" и на короткое время замедляется.
            if (!isTwitching && Random.value < twitchChance * Time.deltaTime)
            {
                isTwitching = true;
                twitchEndTime = Time.time + twitchDuration;
            }

            if (isTwitching)
            {
                if (Time.time < twitchEndTime)
                {
                    speed *= twitchSlowMultiplier;
                }
                else
                {
                    isTwitching = false;
                }
            }

            speed = Mathf.Clamp(speed, 0.5f, baseSpeed + speedVariation);
            agent.speed = speed;
        }
    }
}
