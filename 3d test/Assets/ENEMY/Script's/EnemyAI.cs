using UnityEngine;
using UnityEngine.AI;
using System.Collections;






public class EnemyAI : MonoBehaviour



{



    [Header("References")]



    public Transform player;



    public Transform attackPoint;



    public Animator animator;



    public NavMeshAgent agent;



    public EnemyBlock enemyBlock;







    [Tooltip("Drag the visible enemy model here.")]



    public Transform visualModel;







    [Header("Detection")]



    public float detectionRange = 20f;


[Header("Knockback")]
[Tooltip("Animator trigger played when this enemy is knocked backward.")]
public string knockbackTrigger = "Knockback";

private bool isKnockedBack;
private Coroutine knockbackCoroutine;




    [Header("Combat Distance")]



    public float combatRange = 7f;







    [Tooltip("Distance enemy tries to maintain from player.")]



    public float preferredDistance = 4f;







    [Tooltip("Small dead zone around preferred distance.")]



    public float distanceTolerance = 0.3f;







    [Tooltip("If player gets this close, enemy retreats aggressively.")]



    public float dangerDistance = 2.2f;







    [Header("Combat Movement")]



    public float combatMoveSpeed = 4f;







    [Tooltip("Speed used when player gets too close.")]



    public float retreatSpeed = 6f;







    [Tooltip("How far sideways the enemy tries to circle.")]



    public float circleDistance = 1.5f;







    public float decisionInterval = 1.5f;







    [Range(0f, 1f)]



    public float circleChance = 0.7f;



    [Header("Enemy Separation")]

    [Tooltip("How close another enemy can get before this enemy moves away from it.")]

    public float enemySeparationDistance = 2.5f;



    [Tooltip("How strongly nearby enemies push this enemy's destination away.")]

    public float enemySeparationStrength = 2f;



    [Tooltip("Smooths separation changes so enemies do not jerk or slide when avoiding each other.")]

    public float separationSmoothTime = 0.2f;



    [Tooltip("Ignore very tiny combat destination changes that can cause foot sliding.")]

    public float minimumCombatMoveDistance = 0.2f;







    [Header("Facing")]



    [Tooltip("0 normally. Use 180 if the model faces backward.")]



    public float facingOffset = 0f;







    [Header("Attack")]



    public float attackRange = 2.5f;



    public float attackCooldown = 1.5f;

    [Tooltip("Delay after one enemy finishes attacking before another enemy can attack.")]
    public float attackHandoffDelay = 0.5f;







    [Header("Attack Damage")]



    public float attackDamage = 10f;



    public float damageRadius = 1.5f;



    public LayerMask playerLayer;







    [Header("Blocking")]



    public float blockCheckDelay = 0.3f;







    private PlayerHealth playerHealth;



    private PlayerLockOn playerLockOn;







    private float originalSpeed;







    private float nextAttackTime;



    private float nextBlockCheckTime;



    private float nextCombatDecisionTime;







    private bool inCombatMode;







    public bool IsAttacking { get; private set; }







    // -1 = left



    //  0 = hold



    //  1 = right



    private int combatDirection;







    private Quaternion originalVisualLocalRotation;



    private Vector3 smoothedSeparationOffset;

    private Vector3 separationSmoothVelocity;











    // ==================================================



    // START



    // ==================================================







    void Start()



    {



        // =========================================



        // FIND PLAYER



        // =========================================







        if (player == null)



        {



            GameObject playerObject =



                GameObject.FindGameObjectWithTag("Player");







            if (playerObject != null)



            {



                player = playerObject.transform;



            }



        }







        // =========================================



        // PLAYER COMPONENTS



        // =========================================







        if (player != null)



        {



            playerHealth =



                player.GetComponent<PlayerHealth>();







            if (playerHealth == null)



            {



                playerHealth =



                    player.GetComponentInParent<PlayerHealth>();



            }







            playerLockOn =



                player.GetComponent<PlayerLockOn>();







            if (playerLockOn == null)



            {



                playerLockOn =



                    player.GetComponentInParent<PlayerLockOn>();



            }



        }







        // =========================================



        // FIND ENEMY COMPONENTS



        // =========================================







        if (animator == null)



        {



            animator =



                GetComponentInChildren<Animator>();



        }







        if (agent == null)



        {



            agent =



                GetComponentInChildren<NavMeshAgent>();



        }







        if (enemyBlock == null)



        {



            enemyBlock =



                GetComponent<EnemyBlock>();



        }







        if (visualModel == null &&



            animator != null)



        {



            visualModel = animator.transform;



        }







        // =========================================



        // ERROR CHECKS



        // =========================================







        if (player == null)



        {



            Debug.LogError(



                "ENEMY: Player not assigned!"



            );



        }







        if (playerHealth == null)



        {



            Debug.LogError(



                "ENEMY: PlayerHealth not found!"



            );



        }







        if (playerLockOn == null)



        {



            Debug.LogError(



                "ENEMY: PlayerLockOn not found!"



            );



        }







        if (animator == null)



        {



            Debug.LogError(



                "ENEMY: Animator not assigned!"



            );



        }







        if (agent == null)



        {



            Debug.LogError(



                "ENEMY: NavMeshAgent not assigned!"



            );



        }







        if (attackPoint == null)



        {



            Debug.LogError(



                "ENEMY: AttackPoint not assigned!"



            );



        }







        if (visualModel == null)



        {



            Debug.LogError(



                "ENEMY: Visual Model not assigned!"



            );



        }







        // =========================================



        // NAVMESH SETUP



        // =========================================







        if (agent != null)



        {



            originalSpeed = agent.speed;







            agent.updatePosition = true;



            agent.updateRotation = true;



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
{
    return;
}






        // =========================================



        // PLAYER DEAD



        // =========================================







        if (playerHealth != null &&



            playerHealth.IsDead)



        {



            ExitCombatMode();







            StopMoving();







            animator.ResetTrigger("Attack");







            if (enemyBlock != null)



            {



                enemyBlock.ForceStopBlock();



            }







            return;



        }







        float distanceToPlayer =



            GetFlatDistance(



                agent.transform.position,



                player.position



            );







        // =========================================



        // OUTSIDE DETECTION RANGE



        // =========================================







        if (distanceToPlayer > detectionRange)



        {



            ExitCombatMode();







            StopMoving();







            if (enemyBlock != null)



            {



                enemyBlock.ForceStopBlock();



            }







            return;



        }







        // =========================================



        // COMBAT MODE



        // =========================================







        if (distanceToPlayer <= combatRange)



        {



            EnterCombatMode();







            UpdateCombat(distanceToPlayer);







            return;



        }







        // =========================================



        // NORMAL CHASE



        // =========================================







        ExitCombatMode();







        ChasePlayer();



    }











    // ==================================================



    // ENTER COMBAT MODE



    // ==================================================







    void EnterCombatMode()



    {



        if (inCombatMode)



        {



            return;



        }







        inCombatMode = true;







        agent.updateRotation = true;



        agent.speed = combatMoveSpeed;







        nextCombatDecisionTime = 0f;







        ChooseCombatMovement();



    }











    // ==================================================



    // EXIT COMBAT MODE



    // ==================================================







    void ExitCombatMode()



    {



        if (!inCombatMode)



        {



            return;



        }







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



        {



            return;



        }







        FacePlayer();







        // =========================================



        // CURRENTLY ATTACKING



        // =========================================







        if (IsAttacking)



        {



            StopMoving();



            FacePlayer();



            return;



        }







        float attackDistance =



            GetAttackDistance();







        // =========================================



        // LOCKED-ON ENEMY ATTACKS



        // =========================================







        if (attackDistance <= attackRange &&
            EnemyAttackCoordinator.CanThisEnemyAttack(
                this,
                player,
                attackHandoffDelay))
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







        // =========================================



        // DANGER - PLAYER TOO CLOSE



        // =========================================







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







        // =========================================



        // NON-TARGET ENEMY IN ATTACK RANGE



        //



        // It is NOT allowed to attack.



        // It holds/circles instead.



        // =========================================







        if (attackDistance <= attackRange)



        {



            StopMoving();



            FacePlayer();







            TryBlock();







            return;



        }







        // =========================================



        // STOP BLOCKING WHILE MOVING



        // =========================================







        if (enemyBlock != null &&



            enemyBlock.IsBlocking)



        {



            enemyBlock.StopBlocking();



        }







        // =========================================



        // TOO CLOSE



        // =========================================







        if (distanceToPlayer <



            preferredDistance -



            distanceTolerance)



        {



            RetreatFromPlayer();



            return;



        }







        // =========================================



        // TOO FAR



        // =========================================







        if (distanceToPlayer >



            preferredDistance +



            distanceTolerance)



        {



            MoveTowardPlayer();



            return;



        }







        // =========================================



        // IDEAL COMBAT DISTANCE



        // =========================================







        if (Time.time >=



            nextCombatDecisionTime)



        {



            ChooseCombatMovement();







            nextCombatDecisionTime =



                Time.time +



                decisionInterval;



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



    // IS PLAYER LOCKED ONTO THIS ENEMY?



    // ==================================================







    bool IsPlayerLockedOntoMe()



    {



        if (playerLockOn == null)



        {



            return false;



        }







        if (!playerLockOn.IsLockedOn)



        {



            return false;



        }







        Transform target =



            playerLockOn.CurrentTarget;







        if (target == null)



        {



            return false;



        }







        // Get this enemy's EnemyHealth.



        EnemyHealth myHealth =



            GetComponent<EnemyHealth>();







        if (myHealth == null)



        {



            myHealth =



                GetComponentInParent<EnemyHealth>();



        }







        // Get locked target's EnemyHealth.



        EnemyHealth targetHealth =



            target.GetComponent<EnemyHealth>();







        if (targetHealth == null)



        {



            targetHealth =



                target.GetComponentInParent<EnemyHealth>();



        }







        if (myHealth == null ||



            targetHealth == null)



        {



            return false;



        }







        return myHealth == targetHealth;



    }











    // ==================================================



    // CHOOSE COMBAT MOVEMENT



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



        {



            return;



        }







        Vector3 desiredPosition;







        // Locked target approaches the player normally.



        if (IsPlayerLockedOntoMe())



        {



            Vector3 direction =



                agent.transform.position -



                player.position;







            direction.y = 0f;







            if (direction.sqrMagnitude <



                0.001f)



            {



                direction = -player.forward;



            }







            direction.Normalize();







            desiredPosition =



                player.position +



                direction *



                preferredDistance;



        }



        else



        {



            // Other enemies naturally use their



            // current side of the player.



            Vector3 direction =



                agent.transform.position -



                player.position;







            direction.y = 0f;







            if (direction.sqrMagnitude <



                0.001f)



            {



                direction = player.forward;



            }







            direction.Normalize();







            desiredPosition =



                player.position +



                direction *



                preferredDistance;



        }







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



        {



            return;



        }







        Vector3 enemyPosition =



            agent.transform.position;







        Vector3 awayDirection =



            enemyPosition -



            player.position;







        awayDirection.y = 0f;







        if (awayDirection.sqrMagnitude <



            0.001f)



        {



            awayDirection =



                -player.forward;



        }







        awayDirection.Normalize();







        Vector3 desiredPosition =



            enemyPosition +



            awayDirection *



            preferredDistance;







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



        {



            return;



        }







        Vector3 fromPlayer =



            agent.transform.position -



            player.position;







        fromPlayer.y = 0f;







        if (fromPlayer.sqrMagnitude <



            0.001f)



        {



            fromPlayer = player.forward;



        }







        fromPlayer.Normalize();







        Vector3 tangent =



            Vector3.Cross(



                Vector3.up,



                fromPlayer



            );







        tangent *= direction;







        Vector3 radialPosition =



            player.position +



            fromPlayer *



            preferredDistance;







        Vector3 desiredPosition =



            radialPosition +



            tangent *



            circleDistance;







        MoveToCombatPosition(



            desiredPosition,



            combatMoveSpeed



        );



    }











    // ==================================================



    // MOVE TO COMBAT POSITION



    // ==================================================







    void MoveToCombatPosition(



        Vector3 desiredPosition,



        float moveSpeed)



    {



        if (!agent.isOnNavMesh)



        {



            return;



        }



        // Smooth enemy separation so the NavMesh destination does not

        // suddenly jump sideways and create visible foot sliding.

        Vector3 targetSeparationOffset =

            GetEnemySeparationOffset();



        smoothedSeparationOffset =

            Vector3.SmoothDamp(

                smoothedSeparationOffset,

                targetSeparationOffset,

                ref separationSmoothVelocity,

                separationSmoothTime

            );



        desiredPosition += smoothedSeparationOffset;



        // Ignore tiny destination corrections. These tiny NavMesh movements

        // are a common source of visible sliding while the animation is idle.

        Vector3 flatDifference =

            desiredPosition - agent.transform.position;



        flatDifference.y = 0f;



        if (flatDifference.magnitude < minimumCombatMoveDistance)

        {

            HoldPosition();

            return;

        }



        NavMeshHit hit;







        if (NavMesh.SamplePosition(



            desiredPosition,



            out hit,



            2f,



            NavMesh.AllAreas))



        {



            agent.speed = moveSpeed;



            agent.isStopped = false;







            agent.SetDestination(



                hit.position



            );







            animator.SetFloat(



                "Speed",



                agent.velocity.magnitude,



                0.1f,



                Time.deltaTime



            );



        }



        else



        {



            HoldPosition();



        }







        FacePlayer();



    }



    // ==================================================

    // ENEMY SEPARATION

    // ==================================================



    Vector3 GetEnemySeparationOffset()

    {

        if (enemySeparationDistance <= 0f ||

            enemySeparationStrength <= 0f)

        {

            return Vector3.zero;

        }



        EnemyAI[] allEnemies =

            FindObjectsByType<EnemyAI>(

                FindObjectsSortMode.None

            );



        Vector3 separation = Vector3.zero;

        int nearbyCount = 0;



        Vector3 myPosition =

            agent != null

                ? agent.transform.position

                : transform.position;



        foreach (EnemyAI other in allEnemies)

        {

            if (other == null || other == this)

                continue;



            Vector3 otherPosition =

                other.agent != null

                    ? other.agent.transform.position

                    : other.transform.position;



            Vector3 away = myPosition - otherPosition;

            away.y = 0f;



            float distance = away.magnitude;



            if (distance <= 0.001f ||

                distance >= enemySeparationDistance)

                continue;



            float strength =

                1f - (distance / enemySeparationDistance);



            separation += away.normalized * strength;

            nearbyCount++;

        }



        if (nearbyCount == 0)

            return Vector3.zero;



        separation /= nearbyCount;



        return separation * enemySeparationStrength;

    }















    // ==================================================



    // HOLD POSITION



    // ==================================================







    void HoldPosition()



    {



        if (agent != null &&



            agent.isOnNavMesh)



        {



            agent.isStopped = true;



            agent.ResetPath();



        }







        animator.SetFloat(



            "Speed",



            0f,



            0.1f,



            Time.deltaTime



        );







        FacePlayer();



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







        if (direction.sqrMagnitude <



            0.001f)



        {



            return;



        }







        Quaternion targetRotation =



            Quaternion.LookRotation(



                direction.normalized,



                Vector3.up



            );







        targetRotation *=



            Quaternion.Euler(



                0f,



                facingOffset,



                0f



            );







        visualModel.rotation =



            targetRotation;



    }











    // ==================================================



    // RESTORE VISUAL MODEL



    // ==================================================







    void RestoreVisualRotation()



    {



        if (visualModel == null)



        {



            return;



        }







        visualModel.localRotation =



            originalVisualLocalRotation;



    }











    // ==================================================



    // NORMAL CHASE



    // ==================================================







    void ChasePlayer()



    {



        if (!agent.isOnNavMesh)



        {



            return;



        }







        agent.updateRotation = true;



        agent.speed = originalSpeed;



        agent.isStopped = false;







        agent.SetDestination(



            player.position



        );







        RestoreVisualRotation();







        animator.SetFloat(



            "Speed",



            agent.velocity.magnitude,



            0.1f,



            Time.deltaTime



        );



    }











    // ==================================================



    // STOP MOVEMENT



    // ==================================================







    void StopMoving()



    {



        if (agent != null &&



            agent.isOnNavMesh)



        {



            agent.isStopped = true;



            agent.ResetPath();



        }







        if (animator != null)



        {



            animator.SetFloat(



                "Speed",



                0f,



                0.1f,



                Time.deltaTime



            );



        }



    }











    // ==================================================



    // ATTACK



    // ==================================================







    void TryAttack()



    {



        if (playerHealth != null &&



            playerHealth.IsDead)



        {



            return;



        }







        // =========================================
        // ONLY THE CLOSEST ENEMY CAN ATTACK
        // =========================================

        if (!EnemyAttackCoordinator.TryClaimAttack(
            this,
            player,
            attackHandoffDelay))
        {
            return;
        }







        if (Time.time <



            nextAttackTime)



        {



            return;



        }







        StopMoving();



        FacePlayer();







        if (enemyBlock != null)



        {



            enemyBlock.StopBlocking();



        }







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



    // BLOCK



    // ==================================================







    void TryBlock()



    {



        if (enemyBlock == null)



        {



            return;



        }







        if (enemyBlock.IsBlocking)



        {



            return;



        }







        if (Time.time <



            nextBlockCheckTime)



        {



            return;



        }







        enemyBlock.TryBlock();







        nextBlockCheckTime =



            Time.time +



            blockCheckDelay;



    }











    // ==================================================



    // ATTACK DISTANCE



    // ==================================================







    float GetAttackDistance()



    {



        if (attackPoint == null)



        {



            return GetFlatDistance(



                agent.transform.position,



                player.position



            );



        }







        return GetFlatDistance(



            attackPoint.position,



            player.position



        );



    }











    // ==================================================



    // FLAT DISTANCE



    // ==================================================







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



    // ANIMATION EVENT DAMAGE



    // ==================================================







    public void DealAttackDamage()



    {



        if (playerHealth != null &&



            playerHealth.IsDead)



        {



            return;



        }







        // Extra safety:
        // only the enemy that owns the shared attack turn
        // can deal damage.
        if (!EnemyAttackCoordinator.IsActiveAttacker(this))
        {
            return;
        }







        if (attackPoint == null)



        {



            return;



        }







        Collider[] hits =



            Physics.OverlapSphere(



                attackPoint.position,



                damageRadius,



                playerLayer



            );







        foreach (Collider hit in hits)



        {



            PlayerHealth health =



                hit.GetComponentInParent<PlayerHealth>();







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



    // END ATTACK



    // ==================================================







    public void EndAttack()



    {



        IsAttacking = false;

        EnemyAttackCoordinator.ReleaseAttack(
            this,
            attackHandoffDelay
        );



    }











    // ==================================================



    // CANCEL ATTACK



    // ==================================================







    public void CancelAttack()



    {



        IsAttacking = false;

        EnemyAttackCoordinator.ReleaseAttack(
            this,
            attackHandoffDelay
        );







        if (animator != null)



        {



            animator.ResetTrigger(



                "Attack"



            );



        }



    }

    // ==================================================
    // ATTACK COORDINATOR HELPERS
    // ==================================================

    public bool CanParticipateInAttackSelection()
    {
        if (player == null || agent == null)
            return false;

        if (playerHealth != null && playerHealth.IsDead)
            return false;

        EnemyHealth myHealth = GetComponent<EnemyHealth>();

        if (myHealth == null)
            myHealth = GetComponentInParent<EnemyHealth>();

        if (myHealth != null && myHealth.IsDead)
            return false;

        float distance =
            GetFlatDistance(
                agent.transform.position,
                player.position
            );

        return distance <= detectionRange;
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

    EnemyHealth myHealth =
        GetComponent<EnemyHealth>();

    if (myHealth == null)
    {
        myHealth =
            GetComponentInParent<EnemyHealth>();
    }

    if (myHealth != null &&
        myHealth.IsDead)
    {
        return;
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
// ==================================================
// END KNOCKBACK / STAND UP RECOVERY
// Animation Event at the end of StandingUp.
// ==================================================

public void EndKnockbackRecovery()
{
    if (!isKnockedBack)
        return;

    // Recovery is finished.
    isKnockedBack = false;

    if (agent != null &&
        agent.isOnNavMesh)
    {
        // Clear anything left from knockback.
        agent.ResetPath();

        // Allow NavMesh movement again.
        agent.isStopped = false;

        // Make sure the agent can update normally.
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    // Force the combat AI to make a fresh
    // movement decision immediately.
    nextCombatDecisionTime = 0f;

// IMPORTANT:
// Do NOT set isKnockedBack = false here.
// StandingUp's Animation Event does that.
knockbackCoroutine = null;
}

IEnumerator KnockbackRoutine(
    Vector3 attackerPosition,
    float distance,
    float duration)
{
    isKnockedBack = true;

    // Cancel any attack currently happening.
    if (IsAttacking)
    {
        CancelAttack();
    }

    // Stop blocking.
    if (enemyBlock != null)
    {
        enemyBlock.ForceStopBlock();
    }

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

    // Stop normal NavMesh movement.
    agent.isStopped = true;
    agent.ResetPath();

    Vector3 startPosition =
        agent.transform.position;

    // Push enemy directly away from player.
    Vector3 direction =
        startPosition -
        attackerPosition;

    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f)
    {
        direction =
            -agent.transform.forward;
    }

    direction.Normalize();

    Vector3 wantedEndPosition =
        startPosition +
        direction * distance;

    Vector3 endPosition =
        wantedEndPosition;

    // Try to keep the final position on NavMesh.
    NavMeshHit hit;

    if (NavMesh.SamplePosition(
        wantedEndPosition,
        out hit,
        2f,
        NavMesh.AllAreas))
    {
        endPosition =
            hit.position;
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

        // Smooth movement instead of
        // instantly teleporting.
        float smoothT =
            t * t * (3f - 2f * t);

        Vector3 nextPosition =
            Vector3.Lerp(
                startPosition,
                endPosition,
                smoothT
            );

        // Warp is used because the NavMeshAgent
        // still owns the enemy's position.
        if (agent.isOnNavMesh)
{
    agent.Warp(
        nextPosition
    );

    // IMPORTANT:
    // Keep the NavMeshAgent stopped while
    // Knockback -> StandingUp is playing.
    agent.isStopped = true;
    agent.ResetPath();
}

        }

// Do NOT set isKnockedBack to false here.
// The StandingUp animation will release the enemy.
knockbackCoroutine = null;
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







        if (attackPoint != null)



        {



            Gizmos.DrawWireSphere(



                attackPoint.position,



                attackRange



            );



        }



    }



}
