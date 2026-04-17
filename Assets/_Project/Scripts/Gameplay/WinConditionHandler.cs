using UnityEngine;
using _Project.Scripts.Gameplay.Enemy;

namespace _Project.Scripts.Gameplay
{
    public sealed class WinConditionHandler : MonoBehaviour
    {
        private void OnEnable()
        {
            if (EnemyTracker.Instance == null)
            {
                return;
            }

            EnemyTracker.Instance.OnAllEnemiesDead += OnAllEnemiesDead;
        }

        private void OnDisable()
        {
            if (EnemyTracker.Instance == null)
            {
                return;
            }

            EnemyTracker.Instance.OnAllEnemiesDead -= OnAllEnemiesDead;
        }

        private void OnAllEnemiesDead()
        {
            GameEndFlow.Instance?.Win();
        }
    }
}
