using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemy
{
    public sealed class EnemyTracker : MonoBehaviour
    {
        public static EnemyTracker Instance { get; private set; }

        public event Action OnAllEnemiesDead;

        [SerializeField] private int aliveEnemies;

        public int AliveEnemies => aliveEnemies;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("EnemyTracker: duplicate instance detected, destroying component.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterEnemy()
        {
            aliveEnemies++;
        }

        public void UnregisterEnemy()
        {
            int previousAliveEnemies = aliveEnemies;
            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);

            // Событие вызывается только в момент, когда счётчик живых врагов дошёл до нуля.
            if (previousAliveEnemies > 0 && aliveEnemies == 0)
            {
                OnAllEnemiesDead?.Invoke();
            }
        }
    }
}
