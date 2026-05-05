using Modules.EnemyAI_Base;
using Modules.EnemyAI_Base.Attack;
using Modules.HealthSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyAudioBridge : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Источник звука врага. Через него проигрываются агр, атака, получение урона и смерть.")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("Ссылка на AI врага, по которой определяется момент входа в преследование игрока.")]
        private EnemyAI_Base enemyAi;

        [SerializeField]
        [Tooltip("Ссылка на атакующий компонент врага. Используется для отслеживания начала атаки.")]
        private EnemyAttack enemyAttack;

        [SerializeField]
        [Tooltip("Ссылка на здоровье врага, чтобы проигрывать звуки получения урона и смерти.")]
        private Health health;

        [SerializeField]
        [Tooltip("Набор звуков, из которых случайно выбирается реплика при первом входе врага в преследование игрока.")]
        private AudioClip[] aggroClips = new AudioClip[0];

        [SerializeField]
        [Tooltip("Набор звуков, из которых случайно выбирается реплика в момент начала атаки врага.")]
        private AudioClip[] attackClips = new AudioClip[0];

        [SerializeField]
        [Tooltip("Набор звуков, из которых случайно выбирается реплика, когда враг получает урон и остаётся жив.")]
        private AudioClip[] hurtClips = new AudioClip[0];

        [SerializeField]
        [Tooltip("Набор звуков, из которых случайно выбирается реплика в момент смерти врага.")]
        private AudioClip[] deathClips = new AudioClip[0];

        [SerializeField]
        [Tooltip("Минимальный pitch, который будет случайно выбран перед проигрыванием очередного звука врага.")]
        [Min(0.1f)]
        private float pitchMin = 0.96f;

        [SerializeField]
        [Tooltip("Максимальный pitch, который будет случайно выбран перед проигрыванием очередного звука врага.")]
        [Min(0.1f)]
        private float pitchMax = 1.04f;

        [SerializeField]
        [Tooltip("Минимальная пауза между звуками агра, чтобы враг не спамил одинаковой реакцией при повторном входе в преследование.")]
        [Min(0f)]
        private float aggroCooldown = 0.75f;

        [SerializeField]
        [Tooltip("Минимальная пауза между звуками атаки, чтобы подряд идущие атаки не накладывали звук друг на друга слишком часто.")]
        [Min(0f)]
        private float attackCooldown = 0.35f;

        [SerializeField]
        [Tooltip("Минимальная пауза между звуками получения урона, чтобы серия попаданий не превращалась в непрерывный спам.")]
        [Min(0f)]
        private float hurtCooldown = 0.2f;

        private bool wasPursuingPlayer;
        private bool wasAttacking;
        private float lastAggroTime = float.NegativeInfinity;
        private float lastAttackTime = float.NegativeInfinity;
        private float lastHurtTime = float.NegativeInfinity;

        private void Awake()
        {
            CacheMissingReferences();
        }

        private void Reset()
        {
            CacheMissingReferences();
        }

        private void OnValidate()
        {
            CacheMissingReferences();

            if (pitchMax < pitchMin)
                pitchMax = pitchMin;
        }

        private void CacheMissingReferences()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (enemyAi == null)
                enemyAi = GetComponent<EnemyAI_Base>();

            if (enemyAttack == null)
                enemyAttack = GetComponent<EnemyAttack>();

            if (health == null)
                health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += OnDamaged;
                health.OnDeath += OnDeath;
            }

            wasPursuingPlayer = enemyAi != null && enemyAi.IsPursuingPlayer;
            wasAttacking = enemyAttack != null && enemyAttack.IsAttacking;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
                health.OnDeath -= OnDeath;
            }

            wasPursuingPlayer = false;
            wasAttacking = false;
        }

        private void Update()
        {
            if (enemyAi != null)
            {
                bool isPursuingPlayer = enemyAi.IsPursuingPlayer;
                if (isPursuingPlayer && !wasPursuingPlayer)
                    PlayClip(aggroClips, aggroCooldown, ref lastAggroTime);

                wasPursuingPlayer = isPursuingPlayer;
            }

            if (enemyAttack != null)
            {
                bool isAttacking = enemyAttack.IsAttacking;
                if (isAttacking && !wasAttacking)
                    PlayClip(attackClips, attackCooldown, ref lastAttackTime);

                wasAttacking = isAttacking;
            }
        }

        private void OnDamaged(int appliedDamage)
        {
            if (appliedDamage <= 0)
                return;

            if (health != null && health.IsDead)
                return;

            PlayClip(hurtClips, hurtCooldown, ref lastHurtTime);
        }

        private void OnDeath()
        {
            PlayClip(deathClips);
        }

        private void PlayClip(AudioClip[] clips)
        {
            if (audioSource == null)
                return;

            AudioClip clip = SelectClip(clips);
            if (clip == null)
                return;

            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clip);
        }

        private void PlayClip(AudioClip[] clips, float cooldown, ref float lastPlayTime)
        {
            if (Time.time < lastPlayTime + cooldown)
                return;

            if (audioSource == null)
                return;

            AudioClip clip = SelectClip(clips);
            if (clip == null)
                return;

            lastPlayTime = Time.time;
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clip);
        }

        private static AudioClip SelectClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            if (clips.Length == 1)
                return clips[0];

            return clips[Random.Range(0, clips.Length)];
        }
    }
}
