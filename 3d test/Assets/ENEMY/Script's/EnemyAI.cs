using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    public Transform player;
    public Transform attackPoint;
    public Animator animator;
    public NavMeshAgent agent;
    public EnemyBlock enemyBlock;
    public Transform visualModel;

    // ==================================================
    // DETECTION
    // ==================================================

    [Header("Detection")]
    public float detectionRange = 20f;

    // ==================================================
    // COMBAT DISTANCE
    // ==================================================

    [Header("Combat Distance")]
    public float combatRange = 7f;

    public float preferredDistance = 4f;
    public float distanceTolerance = 0.3f;

    public float dangerDistance = 2.2f;

    // ==================================================
    // COMBAT MOVEMENT
    // ==================================================

    [Header("Combat Movement")]
    public float combatMoveSpeed = 4f;
    public float retreatSpeed = 6f;

    public float circleDistance = 1.5f;
    public float decisionInterval = 1.5f;

    [Range(0f, 1f)]
    public float circleChance = 0.7f;

    // ==================================================
    // GROUP SPACING
    // ==================================================

    [Header("Enemy Separation")]
    public float enemySeparationDistance = 4f;
    public float enemySeparationStrength = 5f;

    public float separationSmoothTime = 0.15f;

    public float minimumCombatMoveDistance = 0.15f;

    [Tooltip(
        "If enemies get closer than this, " +
        "separation takes priority even while waiting."
    )]
    public float emergencySeparationDistance = 1.8f;

    // ==================================================
    // NAVMESH AVOIDANCE
    // ==================================================

    [Header("NavMesh Avoidance")]

    [Tooltip(
        "Uses Unity NavMesh local avoidance in addition " +
        "to the custom separation system."
    )]
    public bool configureAvoidanceAutomatically = true;

    [Tooltip(
        "NavMesh personal-space radius. " +
        "This does NOT replace Enemy Separation Distance."
    )]
    public float avoidanceRadius = 0.6f;

    // ==================================================
    // FACING
    // ==================================================

    [Header("Facing")]

    [Tooltip(
        "Normally 0. Use 180 if the model faces backward."
    )]
    public float facingOffset = 0f;

    // ==================================================
    // ATTACK
    // ==================================================

    [Header("Attack")]
    public float attackRange = 2.5f;

    public float attackCooldown = 1.5f;

    public float attackHandoffDelay = 0.5f;

    // ==================================================
    // AFTER ATTACK RETREAT
    // ==================================================

    [Header("After Attack Retreat")]

    [Range(0f, 1f)]
    public float afterAttackRetreatChance = 0.35f;

    public float afterAttackRetreatDistance = 2f;

    public float afterAttackRetreatDuration = 0.5f;

    // ==================================================
    // DAMAGE
    // ==================================================

    [Header("Attack Damage")]
    public float attackDamage = 10f;

    public float damageRadius = 1.5f;

    public LayerMask playerLayer;

    // ==================================================
    // BLOCKING
    // ==================================================

    [Header("Blocking")]
    public float blockCheckDelay = 0.3f;

    // ==================================================
    // KNOCKBACK
    // ==================================================

    [Header("Knockback")]
    public string knockbackTrigger = "Knockback";

    // ==================================================
    // PRIVATE REFERENCES
    // ==================================================

    private PlayerHealth playerHealth;
    private PlayerLockOn playerLockOn;

    // ==================================================
    // STATE
    // ==================================================

    private float originalSpeed;

    private float nextAttackTime;
    private float nextBlockCheckTime;
    private float nextCombatDecisionTime;

    private bool inCombatMode;
    private bool isKnockedBack;
    private bool isRetreatingAfterAttack;

    public bool IsAttacking { get; private set; }

    // -1 = left
    //  0 = hold
    //  1 = right
    private int combatDirection;

    private Quaternion originalVisualLocalRotation;

    private Vector3 smoothedSeparationOffset;
    private Vector3 separationSmoothVelocity;

    private Coroutine knockbackCoroutine;
    private Coroutine afterAttackRetreatCoroutine;

    // ==================================================
    // SHARED ENEMY LIST
    // ==================================================

    private static readonly List<EnemyAI> allEnemies =
        new List<EnemyAI>();

    // ==================================================
    // ENABLE / DISABLE
    // ==================================================

    void OnEnable()
    {
        if (!allEnemies.Contains(this))
            allEnemies.Add(this);

        EnemyAttackCoordinator.Register(this);
    }

    void OnDisable()
    {
        allEnemies.Remove(this);

        EnemyAttackCoordinator.Unregister(this);
    }

    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        FindReferences();

        if (agent != null)
        {
            originalSpeed = agent.speed;

            agent.updatePosition = true;
            agent.updateRotation = true;

            if (configureAvoidanceAutomatically)
            {
                agent.radius =
                    Mathf.Max(
                        0.1f,
                        avoidanceRadius
                    );

                agent.obstacleAvoidanceType =
                    ObstacleAvoidanceType
                    .HighQualityObstacleAvoidance;

                // Slightly different priorities help
                // prevent perfectly equal agents fighting
                // over the same movement solution.
                int priorityVariation =
                    Mathf.Abs(GetInstanceID()) % 11;

                agent.avoidancePriority =
                    Mathf.Clamp(
                        45 + priorityVariation,
                        0,
                        99
                    );
            }
        }

        if (visualModel != null)
        {
            originalVisualLocalRotation =
                visualModel.localRotation;
        }

        nextCombatDecisionTime =
            Time.time + decisionInterval;
    }

    // ==================================================
    // FIND REFERENCES
    // ==================================================

    void FindReferences()
    {
        if (player == null)
        {
            GameObject obj =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (obj != null)
                player = obj.transform;
        }

        if (player != null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth =
                    player.GetComponentInParent
                    <PlayerHealth>();
            }

            playerLockOn =
                player.GetComponent<PlayerLockOn>();

            if (playerLockOn == null)
            {
                playerLockOn =
                    player.GetComponentInParent
                    <PlayerLockOn>();
            }
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>();

        if (enemyBlock == null)
            enemyBlock = GetComponent<EnemyBlock>();

        if (visualModel == null &&
            animator != null)
        {
            visualModel = animator.transform;
        }

        if (player == null)
            Debug.LogError(
                "ENEMY: Player not assigned!",
                this
            );

        if (animator == null)
            Debug.LogError(
                "ENEMY: Animator not assigned!",
                this
            );

        if (agent == null)
            Debug.LogError(
                "ENEMY: NavMeshAgent not assigned!",
                this
            );

        if (attackPoint == null)
            Debug.LogError(
                "ENEMY: AttackPoint not assigned!",
                this
            );
    }

    // ==================================================
    // UPDATE
    // ==================================================

    void Update()
    {
        if (player == null ||
            animator == null ||
            agent == null)
        {
            return;
        }

        if (isKnockedBack)
            return;

        if (isRetreatingAfterAttack)
            return;

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            ExitCombatMode();
            StopMoving();

            animator.ResetTrigger("Attack");

            if (enemyBlock != null)
                enemyBlock.ForceStopBlock();

            return;
        }

        float distance =
            GetFlatDistance(
                agent.transform.position,
                player.position
            );

        if (distance > detectionRange)
        {
            ExitCombatMode();
            StopMoving();

            if (enemyBlock != null)
                enemyBlock.ForceStopBlock();

            return;
        }

        if (distance <= combatRange)
        {
            EnterCombatMode();

            UpdateCombat(distance);

            return;
        }

        ExitCombatMode();

        ChasePlayer();
    }

    // ==================================================
    // COMBAT MODE
    // ==================================================

    void EnterCombatMode()
    {
        if (inCombatMode)
            return;

        inCombatMode = true;

        agent.updateRotation = true;
        agent.speed = combatMoveSpeed;

        nextCombatDecisionTime = 0f;

        ChooseCombatMovement();
    }

    void ExitCombatMode()
    {
        if (!inCombatMode)
            return;

        inCombatMode = false;
        combatDirection = 0;

        if (agent != null)
        {
            agent.speed = originalSpeed;
            agent.updateRotation = true;
        }

        RestoreVisualRotation();

        if (enemyBlock != null &&
            enemyBlock.IsBlocking)
        {
            enemyBlock.StopBlocking();
        }
    }

    // ==================================================
    // COMBAT
    // ==================================================

    void UpdateCombat(float distanceToPlayer)
    {
        if (!agent.isOnNavMesh)
            return;

        FacePlayer();

        // Current attack animation owns the enemy.
        if (IsAttacking)
        {
            StopMoving();
            FacePlayer();

            return;
        }

        // ----------------------------------------------
        // EMERGENCY ENEMY-TO-ENEMY SPACING
        // ----------------------------------------------

        if (IsTooCloseToAnotherEnemy())
        {
            SeparateFromEnemies();

            return;
        }

        float attackDistance =
            GetAttackDistance();

        // ----------------------------------------------
        // ATTACK TURN
        // ----------------------------------------------

        if (attackDistance <= attackRange &&
            EnemyAttackCoordinator.CanThisEnemyAttack(
                this,
                player,
                attackHandoffDelay
            ))
        {
            StopMoving();
            FacePlayer();

            if (enemyBlock != null &&
                enemyBlock.IsBlocking)
            {
                enemyBlock.StopBlocking();
            }

            if (Time.time >= nextAttackTime)
            {
                TryAttack();

                return;
            }

            TryBlock();

            return;
        }

        // ----------------------------------------------
        // TOO CLOSE TO PLAYER
        // ----------------------------------------------

        if (distanceToPlayer <= dangerDistance)
        {
            if (enemyBlock != null &&
                enemyBlock.IsBlocking)
            {
                enemyBlock.StopBlocking();
            }

            RetreatFromPlayer();

            return;
        }

        // ----------------------------------------------
        // IN ATTACK RANGE BUT WAITING FOR TURN
        // ----------------------------------------------

        if (attackDistance <= attackRange)
        {
            FacePlayer();

            // Do not simply freeze if another enemy is
            // standing too close.
            Vector3 separation =
                GetEnemySeparationOffset();

            if (separation.sqrMagnitude > 0.05f)
            {
                Vector3 desiredPosition =
                    agent.transform.position +
                    separation;

                MoveToCombatPosition(
                    desiredPosition,
                    combatMoveSpeed
                );
            }
            else
            {
                StopMoving();
                TryBlock();
            }

            return;
        }

        // ----------------------------------------------
        // NORMAL COMBAT MOVEMENT
        // ----------------------------------------------

        if (enemyBlock != null &&
            enemyBlock.IsBlocking)
        {
            enemyBlock.StopBlocking();
        }

        if (distanceToPlayer <
            preferredDistance - distanceTolerance)
        {
            RetreatFromPlayer();

            return;
        }

        if (distanceToPlayer >
            preferredDistance + distanceTolerance)
        {
            MoveTowardPlayer();

            return;
        }

        if (Time.time >= nextCombatDecisionTime)
        {
            ChooseCombatMovement();

            nextCombatDecisionTime =
                Time.time + decisionInterval;
        }

        if (combatDirection == -1)
        {
            CirclePlayer(-1);
        }
        else if (combatDirection == 1)
        {
            CirclePlayer(1);
        }
        else
        {
            HoldPosition();
        }
    }

    // ==================================================
    // COMBAT DECISION
    // ==================================================

    void ChooseCombatMovement()
    {
        if (Random.value <= circleChance)
        {
            combatDirection =
                Random.value < 0.5f
                    ? -1
                    : 1;
        }
        else
        {
            combatDirection = 0;
        }
    }

    // ==================================================
    // MOVE TOWARD PLAYER
    // ==================================================

    void MoveTowardPlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        Vector3 direction =
            agent.transform.position -
            player.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction =
                IsPlayerLockedOntoMe()
                    ? -player.forward
                    : player.forward;
        }

        direction.Normalize();

        Vector3 desiredPosition =
            player.position +
            direction * preferredDistance;

        MoveToCombatPosition(
            desiredPosition,
            combatMoveSpeed
        );
    }

    // ==================================================
    // RETREAT FROM PLAYER
    // ==================================================

    void RetreatFromPlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        Vector3 position =
            agent.transform.position;

        Vector3 away =
            position -
            player.position;

        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -player.forward;

        away.Normalize();

        Vector3 desiredPosition =
            position +
            away * preferredDistance;

        MoveToCombatPosition(
            desiredPosition,
            retreatSpeed
        );
    }

    // ==================================================
    // CIRCLE PLAYER
    // ==================================================

    void CirclePlayer(int direction)
    {
        if (!agent.isOnNavMesh)
            return;

        Vector3 fromPlayer =
            agent.transform.position -
            player.position;

        fromPlayer.y = 0f;

        if (fromPlayer.sqrMagnitude < 0.001f)
            fromPlayer = player.forward;

        fromPlayer.Normalize();

        Vector3 tangent =
            Vector3.Cross(
                Vector3.up,
                fromPlayer
            ) * direction;

        Vector3 radialPosition =
            player.position +
            fromPlayer * preferredDistance;

        Vector3 desiredPosition =
            radialPosition +
            tangent * circleDistance;

        MoveToCombatPosition(
            desiredPosition,
            combatMoveSpeed
        );
    }

    // ==================================================
    // COMBAT POSITION
    // ==================================================

    void MoveToCombatPosition(
        Vector3 desiredPosition,
        float moveSpeed)
    {
        if (!agent.isOnNavMesh)
            return;

        Vector3 targetSeparation =
            GetEnemySeparationOffset();

        smoothedSeparationOffset =
            Vector3.SmoothDamp(
                smoothedSeparationOffset,
                targetSeparation,
                ref separationSmoothVelocity,
                separationSmoothTime
            );

        desiredPosition +=
            smoothedSeparationOffset;

        Vector3 difference =
            desiredPosition -
            agent.transform.position;

        difference.y = 0f;

        if (difference.magnitude <
            minimumCombatMoveDistance)
        {
            HoldPosition();

            return;
        }

        if (NavMesh.SamplePosition(
            desiredPosition,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            agent.speed = moveSpeed;

            agent.isStopped = false;

            // SetDestination requests a new NavMesh path.
            agent.SetDestination(hit.position);

            UpdateMovementAnimation();
        }
        else
        {
            HoldPosition();
        }

        FacePlayer();
    }

    // ==================================================
    // SEPARATION
    // ==================================================

    Vector3 GetEnemySeparationOffset()
    {
        if (enemySeparationDistance <= 0f ||
            enemySeparationStrength <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 myPosition =
            GetAgentPosition();

        Vector3 separation =
            Vector3.zero;

        int nearbyCount = 0;

        for (int i = 0;
             i < allEnemies.Count;
             i++)
        {
            EnemyAI other =
                allEnemies[i];

            if (other == null ||
                other == this ||
                !other.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 otherPosition =
                other.GetAgentPosition();

            Vector3 away =
                myPosition -
                otherPosition;

            away.y = 0f;

            float distance =
                away.magnitude;

            if (distance <= 0.001f ||
                distance >=
                enemySeparationDistance)
            {
                continue;
            }

            float normalizedDistance =
                distance /
                enemySeparationDistance;

            // Stronger push when extremely close.
            float strength =
                1f -
                normalizedDistance;

            strength *= strength;

            separation +=
                away.normalized *
                strength;

            nearbyCount++;
        }

        if (nearbyCount == 0)
            return Vector3.zero;

        separation /= nearbyCount;

        return separation *
               enemySeparationStrength;
    }

    // ==================================================
    // EMERGENCY SEPARATION
    // ==================================================

    bool IsTooCloseToAnotherEnemy()
    {
        Vector3 myPosition =
            GetAgentPosition();

        float sqrLimit =
            emergencySeparationDistance *
            emergencySeparationDistance;

        for (int i = 0;
             i < allEnemies.Count;
             i++)
        {
            EnemyAI other =
                allEnemies[i];

            if (other == null ||
                other == this ||
                !other.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 difference =
                myPosition -
                other.GetAgentPosition();

            difference.y = 0f;

            if (difference.sqrMagnitude <
                sqrLimit)
            {
                return true;
            }
        }

        return false;
    }

    void SeparateFromEnemies()
    {
        Vector3 separation =
            GetEnemySeparationOffset();

        if (separation.sqrMagnitude <
            0.001f)
        {
            HoldPosition();

            return;
        }

        Vector3 desiredPosition =
            GetAgentPosition() +
            separation;

        MoveToCombatPosition(
            desiredPosition,
            combatMoveSpeed
        );
    }

    // ==================================================
    // HOLD
    // ==================================================

    void HoldPosition()
    {
        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(0f);

        FacePlayer();
    }

    // ==================================================
    // CHASE
    // ==================================================

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.updateRotation = true;

        agent.speed = originalSpeed;

        agent.isStopped = false;

        agent.SetDestination(
            player.position
        );

        RestoreVisualRotation();

        UpdateMovementAnimation();
    }

    // ==================================================
    // STOP
    // ==================================================

    void StopMoving()
    {
        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(0f);
    }

    // ==================================================
    // MOVEMENT ANIMATION
    // ==================================================

    void UpdateMovementAnimation()
    {
        if (animator == null ||
            agent == null)
        {
            return;
        }

        animator.SetFloat(
            "Speed",
            agent.velocity.magnitude,
            0.1f,
            Time.deltaTime
        );
    }

    void SetMovementAnimation(float value)
    {
        if (animator == null)
            return;

        animator.SetFloat(
            "Speed",
            value,
            0.1f,
            Time.deltaTime
        );
    }

    // ==================================================
    // FACE PLAYER
    // ==================================================

    void FacePlayer()
    {
        if (visualModel == null ||
            player == null)
        {
            return;
        }

        Vector3 direction =
            player.position -
            visualModel.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        rotation *=
            Quaternion.Euler(
                0f,
                facingOffset,
                0f
            );

        visualModel.rotation = rotation;
    }

    void RestoreVisualRotation()
    {
        if (visualModel != null)
        {
            visualModel.localRotation =
                originalVisualLocalRotation;
        }
    }

    // ==================================================
    // LOCK ON
    // ==================================================

    bool IsPlayerLockedOntoMe()
    {
        if (playerLockOn == null ||
            !playerLockOn.IsLockedOn)
        {
            return false;
        }

        Transform target =
            playerLockOn.CurrentTarget;

        if (target == null)
            return false;

        EnemyHealth myHealth =
            GetComponent<EnemyHealth>();

        if (myHealth == null)
        {
            myHealth =
                GetComponentInParent
                <EnemyHealth>();
        }

        EnemyHealth targetHealth =
            target.GetComponent<EnemyHealth>();

        if (targetHealth == null)
        {
            targetHealth =
                target.GetComponentInParent
                <EnemyHealth>();
        }

        return myHealth != null &&
               targetHealth != null &&
               myHealth == targetHealth;
    }

    // ==================================================
    // ATTACK
    // ==================================================

    void TryAttack()
    {
        if (!IsReadyForAttackTurn())
            return;

        if (!EnemyAttackCoordinator.TryClaimAttack(
            this,
            player,
            attackHandoffDelay))
        {
            return;
        }

        StopMoving();
        FacePlayer();

        if (enemyBlock != null)
            enemyBlock.StopBlocking();

        animator.SetBool(
            "Blocking",
            false
        );

        animator.ResetTrigger(
            "Attack"
        );

        IsAttacking = true;

        animator.SetTrigger(
            "Attack"
        );

        nextAttackTime =
            Time.time +
            attackCooldown;

        nextBlockCheckTime =
            Time.time +
            blockCheckDelay;

        nextCombatDecisionTime =
            Time.time + 0.5f;
    }

    // ==================================================
    // END ATTACK
    // Animation Event
    // ==================================================

    public void EndAttack()
    {
        if (!IsAttacking)
            return;

        IsAttacking = false;

        EnemyAttackCoordinator.ReleaseAttack(
            this,
            attackHandoffDelay
        );

        // Retreat only sometimes.
        if (Random.value <=
            afterAttackRetreatChance)
        {
            StartAfterAttackRetreat();
        }
        else
        {
            nextCombatDecisionTime = 0f;
        }
    }

    // ==================================================
    // AFTER ATTACK RETREAT
    // ==================================================

    void StartAfterAttackRetreat()
    {
        if (afterAttackRetreatCoroutine != null)
        {
            StopCoroutine(
                afterAttackRetreatCoroutine
            );
        }

        afterAttackRetreatCoroutine =
            StartCoroutine(
                AfterAttackRetreat()
            );
    }

    IEnumerator AfterAttackRetreat()
    {
        if (agent == null ||
            !agent.isOnNavMesh ||
            player == null)
        {
            afterAttackRetreatCoroutine = null;

            yield break;
        }

        isRetreatingAfterAttack = true;

        Vector3 startPosition =
            GetAgentPosition();

        Vector3 away =
            startPosition -
            player.position;

        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -player.forward;

        away.Normalize();

        Vector3 separation =
            GetEnemySeparationOffset();

        Vector3 direction = away;

        if (separation.sqrMagnitude > 0.001f)
        {
            direction +=
                separation.normalized *
                0.75f;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = away;

        direction.Normalize();

        Vector3 wantedPosition =
            startPosition +
            direction *
            afterAttackRetreatDistance;

        if (NavMesh.SamplePosition(
            wantedPosition,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            agent.speed = retreatSpeed;

            agent.isStopped = false;

            agent.SetDestination(
                hit.position
            );

            float timer = 0f;

            while (timer <
                   afterAttackRetreatDuration)
            {
                if (isKnockedBack)
                {
                    isRetreatingAfterAttack =
                        false;

                    afterAttackRetreatCoroutine =
                        null;

                    yield break;
                }

                timer += Time.deltaTime;

                FacePlayer();

                UpdateMovementAnimation();

                yield return null;
            }
        }

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(0f);

        isRetreatingAfterAttack = false;

        afterAttackRetreatCoroutine = null;

        nextCombatDecisionTime = 0f;
    }

    // ==================================================
    // BLOCK
    // ==================================================

    void TryBlock()
    {
        if (enemyBlock == null ||
            enemyBlock.IsBlocking ||
            Time.time < nextBlockCheckTime)
        {
            return;
        }

        enemyBlock.TryBlock();

        nextBlockCheckTime =
            Time.time +
            blockCheckDelay;
    }

    // ==================================================
    // DEAL DAMAGE
    // Animation Event
    // ==================================================

    public void DealAttackDamage()
    {
        if (!IsAttacking)
            return;

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

        if (!EnemyAttackCoordinator
            .IsActiveAttacker(this))
        {
            return;
        }

        if (attackPoint == null)
            return;

        Collider[] hits =
            Physics.OverlapSphere(
                attackPoint.position,
                damageRadius,
                playerLayer
            );

        foreach (Collider hit in hits)
        {
            PlayerHealth health =
                hit.GetComponentInParent
                <PlayerHealth>();

            if (health != null &&
                !health.IsDead)
            {
                health.TakeDamage(
                    attackDamage
                );

                break;
            }
        }
    }

    // ==================================================
    // CANCEL ATTACK
    // ==================================================

    public void CancelAttack()
    {
        bool wasAttacking =
            IsAttacking;

        IsAttacking = false;

        if (wasAttacking)
        {
            EnemyAttackCoordinator.ReleaseAttack(
                this,
                attackHandoffDelay
            );
        }

        if (animator != null)
            animator.ResetTrigger("Attack");
    }

    // ==================================================
    // COORDINATOR API
    // ==================================================

    public bool CanParticipateInAttackSelection()
    {
        if (!isActiveAndEnabled)
            return false;

        if (player == null ||
            agent == null)
        {
            return false;
        }

        if (!agent.isOnNavMesh)
            return false;

        if (isKnockedBack ||
            isRetreatingAfterAttack ||
            IsAttacking)
        {
            return false;
        }

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return false;
        }

        EnemyHealth health =
            GetComponent<EnemyHealth>();

        if (health == null)
        {
            health =
                GetComponentInParent
                <EnemyHealth>();
        }

        if (health != null &&
            health.IsDead)
        {
            return false;
        }

        float distance =
            GetFlatDistance(
                GetAgentPosition(),
                player.position
            );

        return distance <= combatRange;
    }

    public bool IsReadyForAttackTurn()
    {
        if (!CanParticipateInAttackSelection())
            return false;

        if (Time.time < nextAttackTime)
            return false;

        return IsInAttackRange();
    }

    public bool IsInAttackRange()
    {
        if (player == null)
            return false;

        return GetAttackDistance() <=
               attackRange;
    }

    public float GetSqrDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        Vector3 difference =
            GetAgentPosition() -
            player.position;

        difference.y = 0f;

        return difference.sqrMagnitude;
    }

    public Vector3 GetAttackPosition()
    {
        if (attackPoint != null)
            return attackPoint.position;

        return GetAgentPosition();
    }

    // ==================================================
    // KNOCKBACK
    // ==================================================

    public void ApplyKnockback(
        Vector3 attackerPosition,
        float distance,
        float duration)
    {
        if (agent == null)
            return;

        EnemyHealth health =
            GetComponent<EnemyHealth>();

        if (health == null)
        {
            health =
                GetComponentInParent
                <EnemyHealth>();
        }

        if (health != null &&
            health.IsDead)
        {
            return;
        }

        // Knockback always wins over retreat.
        if (afterAttackRetreatCoroutine != null)
        {
            StopCoroutine(
                afterAttackRetreatCoroutine
            );

            afterAttackRetreatCoroutine = null;
            isRetreatingAfterAttack = false;
        }

        if (knockbackCoroutine != null)
        {
            StopCoroutine(
                knockbackCoroutine
            );
        }

        knockbackCoroutine =
            StartCoroutine(
                KnockbackRoutine(
                    attackerPosition,
                    distance,
                    duration
                )
            );
    }

    IEnumerator KnockbackRoutine(
        Vector3 attackerPosition,
        float distance,
        float duration)
    {
        isKnockedBack = true;

        if (IsAttacking)
            CancelAttack();

        if (enemyBlock != null)
            enemyBlock.ForceStopBlock();

        if (animator != null)
        {
            animator.SetBool(
                "Blocking",
                false
            );

            animator.ResetTrigger(
                "Attack"
            );

            animator.ResetTrigger(
                knockbackTrigger
            );

            animator.SetTrigger(
                knockbackTrigger
            );
        }

        if (!agent.isOnNavMesh)
        {
            isKnockedBack = false;
            knockbackCoroutine = null;

            yield break;
        }

        agent.isStopped = true;
        agent.ResetPath();

        Vector3 start =
            GetAgentPosition();

        Vector3 direction =
            start -
            attackerPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction =
                -agent.transform.forward;
        }

        direction.Normalize();

        Vector3 end =
            start +
            direction * distance;

        if (NavMesh.SamplePosition(
            end,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            end = hit.position;
        }

        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // SmoothStep
            t = t * t *
                (3f - 2f * t);

            Vector3 position =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );

            if (agent.isOnNavMesh)
            {
                agent.Warp(position);

                agent.isStopped = true;
                agent.ResetPath();
            }

            yield return null;
        }

        // IMPORTANT:
        // We intentionally remain isKnockedBack here.
        //
        // Your StandingUp animation event calls
        // EndKnockbackRecovery().
        knockbackCoroutine = null;
    }

    // ==================================================
    // STANDING UP EVENT
    // ==================================================

    public void EndKnockbackRecovery()
    {
        if (!isKnockedBack)
            return;

        isKnockedBack = false;

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();

            agent.isStopped = false;

            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        nextCombatDecisionTime = 0f;

        knockbackCoroutine = null;
    }

    // ==================================================
    // HELPERS
    // ==================================================

    Vector3 GetAgentPosition()
    {
        if (agent != null)
            return agent.transform.position;

        return transform.position;
    }

    float GetAttackDistance()
    {
        Vector3 from =
            attackPoint != null
                ? attackPoint.position
                : GetAgentPosition();

        return GetFlatDistance(
            from,
            player.position
        );
    }

    float GetFlatDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(
            a,
            b
        );
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    void OnDrawGizmosSelected()
    {
        Vector3 center =
            agent != null
                ? agent.transform.position
                : transform.position;

        Gizmos.DrawWireSphere(
            center,
            detectionRange
        );

        Gizmos.DrawWireSphere(
            center,
            combatRange
        );

        Gizmos.DrawWireSphere(
            center,
            preferredDistance
        );

        Gizmos.DrawWireSphere(
            center,
            dangerDistance
        );

        Gizmos.DrawWireSphere(
            center,
            enemySeparationDistance
        );

        Gizmos.DrawWireSphere(
            center,
            emergencySeparationDistance
        );

        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(
                attackPoint.position,
                damageRadius
            );
        }
    }
}