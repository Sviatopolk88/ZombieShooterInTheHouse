using Modules.HealthSystem;
using UnityEngine;
using _Project.Scripts.GameFlow;

namespace _Project.Scripts.Gameplay.Player
{
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private string itemRootName = "ItemRoot";
        [SerializeField] private float deathScreenDelay = 0.5f;

        private Health health;
        private GameObject itemRoot;
        private bool warnedAboutMissingHealth;
        private bool warnedAboutMissingGameFlowService;

        private void Awake()
        {
            // Ищем Health в родителях, чтобы скрипт работал с вложенной структурой игрока.
            health = GetComponentInParent<Health>();

            if (health == null)
            {
                WarnAboutMissingHealth();
                return;
            }

            GameObject rootObject = health.gameObject;
            itemRoot = FindChildByName(rootObject.transform, itemRootName)?.gameObject;
            health.OnDeath += OnPlayerDeath;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= OnPlayerDeath;
            }
        }

        private void OnPlayerDeath()
        {
            StartCoroutine(DeathRoutine());
        }

        private System.Collections.IEnumerator DeathRoutine()
        {
            // Даём короткую паузу после удара, чтобы смерть не ощущалась мгновенным обрывом.
            yield return new WaitForSeconds(Mathf.Max(0f, deathScreenDelay));

            // Отключаем предметы только после короткой задержки, чтобы NeoFPS успел завершить
            // внутренние действия выбора/переключения оружия и не пытался запускать корутины
            // на уже неактивных объектах.
            if (itemRoot != null)
            {
                itemRoot.SetActive(false);
            }

            if (GameFlowService.Instance != null)
            {
                GameFlowService.Instance.OnPlayerDied();
                yield break;
            }

            WarnAboutMissingGameFlowService();
            Time.timeScale = 0f;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), childName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void WarnAboutMissingHealth()
        {
            if (warnedAboutMissingHealth)
            {
                return;
            }

            warnedAboutMissingHealth = true;
            Debug.LogWarning("PlayerDeathHandler: Health component not found in parent hierarchy.", this);
        }

        private void WarnAboutMissingGameFlowService()
        {
            if (warnedAboutMissingGameFlowService)
            {
                return;
            }

            warnedAboutMissingGameFlowService = true;
            Debug.LogWarning("PlayerDeathHandler: GameFlowService instance not found in Main scene.", this);
        }
    }
}
