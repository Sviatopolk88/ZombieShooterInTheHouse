using UnityEngine;

namespace Modules.EnemySpawner
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform parent;
        [SerializeField] private int enemyCount = 3;

        private void Start()
        {
            if (spawnOnStart)
            {
                Spawn();
            }
        }

        public void Spawn()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("EnemySpawner: enemyPrefab is not assigned.", this);
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("EnemySpawner: spawnPoints is empty.", this);
                return;
            }

            int spawnAmount = Mathf.Max(0, enemyCount);

            // Спавним простое заданное количество врагов без волн и дополнительной логики.
            // В текущей реализации одна и та же точка может использоваться несколько раз.
            for (int i = 0; i < spawnAmount; i++)
            {
                Transform spawnPoint = GetRandomSpawnPoint();

                if (parent == null)
                {
                    Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                }
                else
                {
                    Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation, parent);
                }
            }
        }

        private Transform GetRandomSpawnPoint()
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex];
        }
    }
}
