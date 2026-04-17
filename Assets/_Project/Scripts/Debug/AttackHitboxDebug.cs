using Modules.EnemyAI_Base.Attack;
using UnityEngine;

namespace _Project.Scripts.DebugTools
{
    public sealed class AttackHitboxDebug : MonoBehaviour
    {
        [SerializeField] private Color idleColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color activeColor = new Color(1f, 0f, 0f, 0.4f);

        private Collider cachedCollider;
        private EnemyAttack enemyAttack;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnDrawGizmos()
        {
            CacheReferences();

            if (cachedCollider == null)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.color = enemyAttack != null && enemyAttack.IsAttacking
                ? activeColor
                : idleColor;

            Gizmos.matrix = transform.localToWorldMatrix;
            DrawColliderGizmo();

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private void CacheReferences()
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider>();
            }

            if (enemyAttack == null)
            {
                enemyAttack = GetComponentInParent<EnemyAttack>();
            }
        }

        private void DrawColliderGizmo()
        {
            if (cachedCollider is BoxCollider boxCollider)
            {
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                return;
            }

            if (cachedCollider is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
                return;
            }

            if (cachedCollider is CapsuleCollider capsuleCollider)
            {
                DrawCapsuleApproximation(capsuleCollider);
            }
        }

        private void DrawCapsuleApproximation(CapsuleCollider capsuleCollider)
        {
            Vector3 size = Vector3.one * (capsuleCollider.radius * 2f);

            switch (capsuleCollider.direction)
            {
                case 0:
                    size.x = capsuleCollider.height;
                    break;
                case 1:
                    size.y = capsuleCollider.height;
                    break;
                case 2:
                    size.z = capsuleCollider.height;
                    break;
            }

            Gizmos.DrawCube(capsuleCollider.center, size);
        }
    }
}
