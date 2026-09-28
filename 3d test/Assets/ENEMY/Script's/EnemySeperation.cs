using UnityEngine;
using UnityEngine.AI;

public class EnemySeparation : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;

    [Header("Avoidance")]
    public int minimumPriority = 40;
    public int maximumPriority = 60;

    void Start()
    {
        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(
                "ENEMY SEPARATION: NavMeshAgent not found!"
            );

            return;
        }

        // Give each enemy a slightly different
        // avoidance priority.
        agent.avoidancePriority =
            Random.Range(
                minimumPriority,
                maximumPriority + 1
            );
    }
}