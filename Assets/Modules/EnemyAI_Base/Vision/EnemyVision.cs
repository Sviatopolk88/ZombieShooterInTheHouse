using UnityEngine;
using UnityEngine.AI;

namespace Modules.EnemyAI_Base.Vision
{
    public sealed class EnemyVision : MonoBehaviour
    {
        private const float NavMeshSampleRadius = 1f;
        private const float NavMeshTargetReachTolerance = 1f;

        [Header("Vision")]
        [Tooltip("Максимальная дистанция, на которой враг может увидеть цель в обычном режиме обзора.")]
        [SerializeField] private float viewDistance = 10f;
        [Tooltip("Ближняя дистанция, на которой враг может 'почувствовать' цель даже вне сектора обзора, но только если до неё есть непрерывный путь по NavMesh.")]
        [SerializeField] private float aggroDistance = 3f;
        [Tooltip("Насколько реальная длина пути по NavMesh может превышать aggroDistance, чтобы ближнее обнаружение всё ещё сработало.")]
        [SerializeField, Min(1f)] private float aggroPathLengthMultiplier = 1.25f;
        [Tooltip("Полный угол обзора врага в градусах.")]
        [SerializeField] [Range(0f, 270f)] private float viewAngle = 90f;
        [Tooltip("Слои препятствий, которые могут перекрыть линию видимости до цели.")]
        [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;
        [Tooltip("Показывать ли в редакторе сферы дистанции обзора и ближнего чувства. В игре gizmo всё равно не рисуются.")]
        [SerializeField] private bool showVisionGizmos = true;

        private NavMeshAgent navMeshAgent;
        private NavMeshPath navMeshPath;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshPath = new NavMeshPath();
        }

        public bool CanSee(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, target.position);

            // Если цель совсем близко, реагируем без проверки угла, но только при наличии непрерывного пути.
            if (distance <= aggroDistance)
            {
                return HasContinuousNavMeshPath(target.position);
            }

            Vector3 origin = transform.position;
            Vector3 targetPosition = target.position;
            Vector3 toTarget = targetPosition - origin;
            float distanceToTarget = toTarget.magnitude;

            // Если цель дальше стандартной дистанции обзора, не видим её.
            if (distanceToTarget > viewDistance)
            {
                return false;
            }

            if (distanceToTarget <= 0f)
            {
                return true;
            }

            Vector3 directionToTarget = toTarget / distanceToTarget;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            // Проверяем, попадает ли цель в угол обзора.
            if (angleToTarget > viewAngle * 0.5f)
            {
                return false;
            }

            // Проверяем, нет ли препятствия между врагом и целью.
            if (Physics.Raycast(origin, directionToTarget, out RaycastHit hit, distanceToTarget, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return hit.transform == target || hit.transform.IsChildOf(target);
            }

            return true;
        }

        private bool HasContinuousNavMeshPath(Vector3 targetPosition)
        {
            int areaMask = navMeshAgent != null ? navMeshAgent.areaMask : NavMesh.AllAreas;

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit originHit, NavMeshSampleRadius, areaMask))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, NavMeshSampleRadius, areaMask))
            {
                return false;
            }

            navMeshPath ??= new NavMeshPath();

            if (!NavMesh.CalculatePath(originHit.position, targetHit.position, areaMask, navMeshPath))
            {
                return false;
            }

            if (navMeshPath.status != NavMeshPathStatus.PathComplete || navMeshPath.corners == null || navMeshPath.corners.Length == 0)
            {
                return false;
            }

            Vector3 lastCorner = navMeshPath.corners[navMeshPath.corners.Length - 1];
            if (Vector3.Distance(lastCorner, targetHit.position) > NavMeshTargetReachTolerance)
            {
                return false;
            }

            float maxAllowedPathLength = aggroDistance * aggroPathLengthMultiplier;
            return CalculatePathLength(navMeshPath) <= maxAllowedPathLength;
        }

        private static float CalculatePathLength(NavMeshPath path)
        {
            if (path == null || path.corners == null || path.corners.Length < 2)
            {
                return 0f;
            }

            float totalLength = 0f;

            for (int i = 1; i < path.corners.Length; i++)
            {
                totalLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return totalLength;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!showVisionGizmos)
            {
                return;
            }

            Vector3 origin = transform.position;

            Gizmos.color = new Color(1f, 0.76f, 0.12f, 0.12f);
            Gizmos.DrawSphere(origin, aggroDistance);
            Gizmos.color = new Color(1f, 0.76f, 0.12f, 0.9f);
            Gizmos.DrawWireSphere(origin, aggroDistance);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.08f);
            Gizmos.DrawSphere(origin, viewDistance);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(origin, viewDistance);
        }
    }
}
