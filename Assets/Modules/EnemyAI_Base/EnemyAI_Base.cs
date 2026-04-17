using Modules.EnemyAI_Base.Animation;
using Modules.EnemyAI_Base.Attack;
using Modules.EnemyAI_Base.Movement;
using Modules.EnemyAI_Base.Vision;
using Modules.HealthSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Modules.EnemyAI_Base
{
    public enum EnemyTargetPriorityMode
    {
        TagOrder = 0,
        NearestAllowedTarget = 1
    }

    public sealed class EnemyAI_Base : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Legacy fallback-тег цели. Используется только если список allowedTargetTags пуст.")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Список допустимых тегов целей для врага, например Player и RescueTarget.")]
        [SerializeField] private string[] allowedTargetTags = new string[0];
        [Tooltip("Режим выбора цели: по порядку тегов или по ближайшей допустимой цели.")]
        [SerializeField] private EnemyTargetPriorityMode targetPriorityMode = EnemyTargetPriorityMode.TagOrder;
        [Tooltip("Сколько секунд враг помнит цель после потери прямой видимости, прежде чем вернуться в idle.")]
        [SerializeField, Min(0f)] private float lostTargetCooldown = 2f;
        [Tooltip("Насколько новая цель должна быть ближе текущей, чтобы враг переключился на неё без дёрганья.")]
        [SerializeField, Min(0f)] private float retargetDistanceAdvantage = 0.75f;

        [Header("Move")]
        [Tooltip("Базовая скорость движения врага, с которой синхронизируются NavMeshAgent и EnemyMovement.")]
        [SerializeField] private float moveSpeed = 2f;
        [Tooltip("Дистанция, на которой враг считает цель доступной для начала атаки.")]
        [SerializeField] private float attackDistance = 1.5f;

        [Header("Attack")]
        [Tooltip("Минимальный интервал между запусками атакующего цикла.")]
        [SerializeField] private float attackCooldown = 1.5f;
        [Tooltip("Как часто враг пересчитывает выбор лучшей цели и обновляет поиск.")]
        [SerializeField] private float searchInterval = 1f;
        [Tooltip("Скорость разворота врага к цели во время подготовки и выполнения атаки.")]
        [SerializeField] private float attackTurnSpeed = 12f;

        private Transform target;
        private EnemyAttack enemyAttack;
        private EnemyAnimationController animationController;
        private EnemyMovement movement;
        private EnemyVision vision;
        private NavMeshAgent agent;
        private Rigidbody cachedRigidbody;
        private Health health;
        private float nextAttackTime;
        private float nextSearchTime;
        private float lastSeenTargetTime = float.NegativeInfinity;
        private float reachedLastKnownTargetTime = float.NegativeInfinity;
        private Vector3 lastKnownTargetPosition;
        private bool hasLastKnownTargetPosition;
        private bool hasReachedLastKnownTargetPosition;
        private bool isSubscribedToHealth;
        private bool wasAttackingLastFrame;
        private bool isInHitReaction;
        private bool hitReactionAnimationStarted;
        private Transform pendingAggroTarget;
        private readonly List<Transform> registeredTargetBuffer = new();

        private void Awake()
        {
            enemyAttack = GetComponent<EnemyAttack>();
            animationController = GetComponent<EnemyAnimationController>();
            movement = GetComponent<EnemyMovement>();
            vision = GetComponent<EnemyVision>();
            agent = GetComponent<NavMeshAgent>();
            cachedRigidbody = GetComponent<Rigidbody>();
            health = GetComponent<Health>();

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = attackDistance;
                agent.updateRotation = true;
            }

            if (movement != null)
            {
                movement.SetBaseSpeed(moveSpeed);
            }
        }

        private void OnEnable()
        {
            SubscribeToHealth();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void Update()
        {
            if (UpdateHitReaction())
            {
                return;
            }

            bool isAttacking = enemyAttack != null && enemyAttack.IsAttacking;

            if (isAttacking)
            {
                StopMovement();
                FaceTarget();
                wasAttackingLastFrame = true;
                return;
            }

            if (wasAttackingLastFrame)
            {
                animationController?.StopAttack();
                ResumeMovement();
                wasAttackingLastFrame = false;
            }

            if (target != null && !target.gameObject.activeInHierarchy)
            {
                StopMovement();
                ClearTarget();
                return;
            }

            if (target != null && !IsTargetValid(target))
            {
                StopMovement();
                ClearTarget();
            }

            RefreshTargetSelection();

            if (target == null)
            {
                StopMovement();
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= attackDistance)
            {
                Attack();
                return;
            }

            if (CanSeeTarget())
            {
                RememberTargetPosition(target);
                ResumeMovement();
                MoveToTarget();
                return;
            }

            if (HasTargetMemory())
            {
                ResumeMovement();
                MoveToLastKnownTargetPosition();
                return;
            }

            ClearTarget();
            StopMovement();
        }

        private void FindTarget()
        {
            // Для простого прототипа ищем игрока по стандартному тегу Unity.
            GameObject player = null;

            try
            {
                player = GameObject.FindGameObjectWithTag(playerTag);
            }
            catch (UnityException)
            {
                return;
            }

            if (player != null)
            {
                Transform foundTarget = player.transform;
                if (vision == null || vision.CanSee(foundTarget))
                {
                    SetTarget(foundTarget);
                }
            }
        }

        private bool CanSeeTarget()
        {
            if (target == null)
            {
                return false;
            }

            if (vision != null)
            {
                return vision.CanSee(target);
            }

            return true;
        }

        private void MoveToTarget()
        {
            if (target == null)
            {
                return;
            }

            if (movement != null)
            {
                movement.MoveTo(target.position);
                return;
            }

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.SetDestination(target.position);
                return;
            }

            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime);

            transform.position = nextPosition;
            FaceTarget();
        }

        private void Attack()
        {
            if (target == null)
            {
                return;
            }

            if (enemyAttack != null && enemyAttack.IsAttacking)
            {
                return;
            }

            if (animationController != null && animationController.IsAttackAnimationActive())
            {
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            StopMovement();
            FaceTarget();

            if (enemyAttack == null)
            {
                nextAttackTime = Time.time + attackCooldown;
                return;
            }

            enemyAttack?.StartAttack(target);
            animationController?.PlayAttack();

            nextAttackTime = Time.time + attackCooldown;
        }

        private void FaceTarget()
        {
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * attackTurnSpeed);
        }

        private void SubscribeToHealth()
        {
            if (health == null || isSubscribedToHealth)
            {
                return;
            }

            health.OnDamageApplied += OnDamageApplied;
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
            isSubscribedToHealth = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (health == null || !isSubscribedToHealth)
            {
                return;
            }

            health.OnDamageApplied -= OnDamageApplied;
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
            isSubscribedToHealth = false;
        }

        private void OnDamageApplied(DamageContext context, int appliedDamage)
        {
            if (appliedDamage <= 0 || health == null || health.IsDead)
            {
                return;
            }

            Transform aggressor = ResolveAggroTarget(context.Source);
            if (aggressor == null)
            {
                return;
            }

            pendingAggroTarget = aggressor;
            RememberTargetPosition(aggressor);

            if (!isInHitReaction)
            {
                PromotePendingAggroTarget();
            }
        }

        private void OnDamaged(int appliedDamage)
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            isInHitReaction = true;
            hitReactionAnimationStarted = false;

            StopMovement();
            animationController?.StopAttack();
            animationController?.PlayHit();
        }

        private void OnDeath()
        {
            isInHitReaction = false;
            hitReactionAnimationStarted = false;

            animationController?.PlayDeath();
            animationController?.StopAttack();
            pendingAggroTarget = null;
            ClearTarget();

            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (movement != null)
            {
                movement.enabled = false;
            }

            if (vision != null)
            {
                vision.enabled = false;
            }

            if (enemyAttack != null)
            {
                enemyAttack.enabled = false;
            }

            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = true;
            }

            enabled = false;
        }

        private bool UpdateHitReaction()
        {
            if (!isInHitReaction)
            {
                return false;
            }

            StopMovement();

            if (animationController == null || !animationController.HasAnimator)
            {
                isInHitReaction = false;
                hitReactionAnimationStarted = false;
                return false;
            }

            if (animationController.IsHitAnimationActive())
            {
                hitReactionAnimationStarted = true;
                return true;
            }

            if (!hitReactionAnimationStarted)
            {
                return true;
            }

            isInHitReaction = false;
            hitReactionAnimationStarted = false;
            PromotePendingAggroTarget();
            return false;
        }

        private void StopMovement()
        {
            if (agent == null || !agent.enabled)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void ResumeMovement()
        {
            if (agent == null || !agent.enabled)
            {
                return;
            }

            agent.isStopped = false;
        }

        private void MoveToLastKnownTargetPosition()
        {
            if (!hasLastKnownTargetPosition)
            {
                StopMovement();
                return;
            }

            if (HasReachedLastKnownTargetPosition())
            {
                StopMovement();
                return;
            }

            MoveToPosition(lastKnownTargetPosition);
        }

        private void MoveToPosition(Vector3 position)
        {
            if (movement != null)
            {
                movement.MoveTo(position);
                return;
            }

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.SetDestination(position);
                return;
            }

            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                position,
                moveSpeed * Time.deltaTime);

            transform.position = nextPosition;
            FaceTowardsPosition(position);
        }

        private void FaceTowardsPosition(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * attackTurnSpeed);
        }

        private bool HasTargetMemory()
        {
            if (target == null || !hasLastKnownTargetPosition)
            {
                return false;
            }

            if (!hasReachedLastKnownTargetPosition)
            {
                return true;
            }

            return Time.time <= reachedLastKnownTargetTime + lostTargetCooldown;
        }

        private void SetTarget(Transform newTarget)
        {
            if (newTarget == null)
            {
                return;
            }

            target = newTarget;
            RememberTargetPosition(newTarget);
        }

        private void ClearTarget()
        {
            target = null;
            hasLastKnownTargetPosition = false;
            hasReachedLastKnownTargetPosition = false;
            lastSeenTargetTime = float.NegativeInfinity;
            reachedLastKnownTargetTime = float.NegativeInfinity;
        }

        private void RememberTargetPosition(Transform trackedTarget)
        {
            if (trackedTarget == null)
            {
                return;
            }

            lastKnownTargetPosition = trackedTarget.position;
            lastSeenTargetTime = Time.time;
            hasLastKnownTargetPosition = true;
            hasReachedLastKnownTargetPosition = false;
            reachedLastKnownTargetTime = float.NegativeInfinity;
        }

        private bool HasReachedLastKnownTargetPosition()
        {
            if (!hasLastKnownTargetPosition)
            {
                return false;
            }

            if (!hasReachedLastKnownTargetPosition && IsAtPosition(lastKnownTargetPosition))
            {
                hasReachedLastKnownTargetPosition = true;
                reachedLastKnownTargetTime = Time.time;
            }

            return hasReachedLastKnownTargetPosition;
        }

        private bool IsAtPosition(Vector3 position)
        {
            float tolerance = 0.25f;

            if (agent != null && agent.enabled)
            {
                tolerance = Mathf.Max(tolerance, agent.stoppingDistance + 0.05f);
            }

            Vector3 flatCurrent = transform.position;
            flatCurrent.y = 0f;

            Vector3 flatTarget = position;
            flatTarget.y = 0f;

            return Vector3.Distance(flatCurrent, flatTarget) <= tolerance;
        }

        private void PromotePendingAggroTarget()
        {
            if (pendingAggroTarget == null)
            {
                return;
            }

            if (health != null && health.IsDead)
            {
                pendingAggroTarget = null;
                return;
            }

            if (!IsTargetValid(pendingAggroTarget))
            {
                pendingAggroTarget = null;
                return;
            }

            SetTarget(pendingAggroTarget);
            pendingAggroTarget = null;
        }

        private Transform ResolveAggroTarget(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            Transform current = source.transform;
            while (current != null)
            {
                if (IsAllowedTargetTag(current.tag) && IsTargetValid(current))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private void FindTargetExtended()
        {
            string[] targetTags = GetAllowedTargetTags();
            if (targetTags.Length == 0)
            {
                return;
            }

            if (targetPriorityMode == EnemyTargetPriorityMode.NearestAllowedTarget)
            {
                Transform nearestTarget = FindNearestTarget(targetTags);
                if (nearestTarget != null)
                {
                    SetTarget(nearestTarget);
                }

                return;
            }

            for (int i = 0; i < targetTags.Length; i++)
            {
                Transform prioritizedTarget = FindNearestTargetForTag(targetTags[i]);
                if (prioritizedTarget != null)
                {
                    SetTarget(prioritizedTarget);
                    return;
                }
            }
        }

        private void RefreshTargetSelection()
        {
            if (Time.time < nextSearchTime)
            {
                return;
            }

            nextSearchTime = Time.time + searchInterval;

            if (targetPriorityMode != EnemyTargetPriorityMode.NearestAllowedTarget)
            {
                if (target == null)
                {
                    FindTargetExtended();
                }

                return;
            }

            string[] targetTags = GetAllowedTargetTags();
            if (targetTags.Length == 0)
            {
                return;
            }

            Transform nearestTarget = FindNearestTarget(targetTags);
            if (nearestTarget == null)
            {
                if (target == null)
                {
                    FindTargetExtended();
                }

                return;
            }

            if (target == null || nearestTarget == target)
            {
                SetTarget(nearestTarget);
                return;
            }

            if (ShouldSwitchToTarget(nearestTarget))
            {
                SetTarget(nearestTarget);
            }
        }

        private string[] GetAllowedTargetTags()
        {
            if (allowedTargetTags != null && allowedTargetTags.Length > 0)
            {
                int validCount = 0;

                for (int i = 0; i < allowedTargetTags.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(allowedTargetTags[i]))
                    {
                        validCount++;
                    }
                }

                if (validCount > 0)
                {
                    string[] result = new string[validCount];
                    int index = 0;

                    for (int i = 0; i < allowedTargetTags.Length; i++)
                    {
                        string tag = allowedTargetTags[i];
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            result[index] = tag;
                            index++;
                        }
                    }

                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return System.Array.Empty<string>();
            }

            return new[] { playerTag };
        }

        private bool IsAllowedTargetTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            string[] targetTags = GetAllowedTargetTags();
            for (int i = 0; i < targetTags.Length; i++)
            {
                if (targetTags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        private Transform FindNearestTarget(string[] targetTags)
        {
            Transform nearestTarget = null;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < targetTags.Length; i++)
            {
                Transform candidate = FindNearestTargetForTag(targetTags[i], ref nearestDistanceSqr);
                if (candidate != null)
                {
                    nearestTarget = candidate;
                }
            }

            return nearestTarget;
        }

        private Transform FindNearestTargetForTag(string targetTag)
        {
            float nearestDistanceSqr = float.MaxValue;
            return FindNearestTargetForTag(targetTag, ref nearestDistanceSqr);
        }

        private Transform FindNearestTargetForTag(string targetTag, ref float nearestDistanceSqr)
        {
            if (string.IsNullOrWhiteSpace(targetTag))
            {
                return null;
            }

            Transform nearestRegisteredTarget = FindNearestRegisteredTargetForTag(targetTag, ref nearestDistanceSqr);
            if (nearestRegisteredTarget != null)
            {
                return nearestRegisteredTarget;
            }

            return FindNearestTargetForTagLegacy(targetTag, ref nearestDistanceSqr);
        }

        private Transform FindNearestRegisteredTargetForTag(string targetTag, ref float nearestDistanceSqr)
        {
            if (EnemyTargetRegistry.CopyTargets(targetTag, registeredTargetBuffer) == 0)
            {
                return null;
            }

            return FindNearestTargetInBuffer(registeredTargetBuffer, ref nearestDistanceSqr);
        }

        private Transform FindNearestTargetForTagLegacy(string targetTag, ref float nearestDistanceSqr)
        {
            if (string.IsNullOrWhiteSpace(targetTag))
            {
                return null;
            }

            GameObject[] candidates;

            try
            {
                candidates = GameObject.FindGameObjectsWithTag(targetTag);
            }
            catch (UnityException)
            {
                return null;
            }

            Transform nearestTarget = null;

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                Transform candidateTransform = candidate.transform;
                if (!IsTargetValid(candidateTransform))
                {
                    continue;
                }

                if (vision != null && !vision.CanSee(candidateTransform))
                {
                    continue;
                }

                float distanceSqr = (candidateTransform.position - transform.position).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestTarget = candidateTransform;
            }

            return nearestTarget;
        }

        private Transform FindNearestTargetInBuffer(List<Transform> candidates, ref float nearestDistanceSqr)
        {
            Transform nearestTarget = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                Transform candidateTransform = candidates[i];
                if (candidateTransform == null)
                {
                    continue;
                }

                if (!IsTargetValid(candidateTransform))
                {
                    continue;
                }

                if (vision != null && !vision.CanSee(candidateTransform))
                {
                    continue;
                }

                float distanceSqr = (candidateTransform.position - transform.position).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestTarget = candidateTransform;
            }

            return nearestTarget;
        }

        private bool ShouldSwitchToTarget(Transform candidate)
        {
            if (candidate == null || candidate == target)
            {
                return false;
            }

            if (target == null)
            {
                return true;
            }

            if (!IsTargetValid(target) || !CanSeeTarget())
            {
                return true;
            }

            float currentDistance = Vector3.Distance(transform.position, target.position);
            float candidateDistance = Vector3.Distance(transform.position, candidate.position);

            return candidateDistance + retargetDistanceAdvantage < currentDistance;
        }

        private bool IsTargetValid(Transform candidate)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!IsAllowedTargetTag(candidate.tag))
            {
                return false;
            }

            Health candidateHealth = candidate.GetComponentInParent<Health>();
            if (candidateHealth != null && candidateHealth.IsDead)
            {
                return false;
            }

            ITargetableEntity targetableEntity = candidate.GetComponentInParent<ITargetableEntity>();
            if (targetableEntity != null && !targetableEntity.IsValidEnemyTarget)
            {
                return false;
            }

            return true;
        }
    }
}
