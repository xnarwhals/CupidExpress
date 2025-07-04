using UnityEngine;

public class AIBehaviorController : MonoBehaviour
{
    [SerializeField] private AIPersonality personality; // scriptable object

    [Header("Corner Settings")]
    [Range(5f, 30f)]
    public float distanceBeforeSlowDown = 10f; // 10 meters 
    [Range(0.3f, 0.8f)]
    public float slowDownFactor = 0.6f; // 60% slower when approaching corners

    private AIDriver aiDriver;
    private int lastWayPointIndex = -1;

    private void Awake()
    {
        aiDriver = GetComponent<AIDriver>();
    }

    private void Start()
    {
        ApplyPersonalityValues();
    }

    private void ApplyPersonalityValues()
    {
        if (personality == null) return;

        aiDriver.maxSpeed += personality.aggressiveness * 5f; // 5 m/s increase based on aggressiveness
        aiDriver.acceleration *= Mathf.Lerp(0.6f, 1.3f, personality.aggressiveness); // 60% to 130% of base acceleration
        aiDriver.turnSpeed *= Mathf.Lerp(0.8f, 1.5f, personality.aggressiveness); // 80% to 120% of base turn speed
    }

    private void UpdateTargetWithOffset()
    {
        Vector3 baseTarget = aiDriver.GetCurrentWaypointPosition();
        Vector3 offsetTarget = CalculateOffsetPosition(baseTarget);
        aiDriver.SetTarget(offsetTarget);

    }

    private Vector3 CalculateOffsetPosition(Vector3 baseWaypoint)
    {
        if (personality == null) return baseWaypoint;

        Vector3 trackDirection = GetTrackDirection();
        Vector3 LRDirection = Vector3.Cross(trackDirection, Vector3.up).normalized;

        Vector3 offset;

        switch (personality.drivingLane)
        {
            case DrivingLane.Left:
                offset = -LRDirection * personality.laneOffset;
                break;
            case DrivingLane.Right:
                offset = LRDirection * personality.laneOffset;
                break;
            case DrivingLane.Center:
            default:
                offset = Vector3.zero;
                break;
        }

        return baseWaypoint + offset * personality.laneCommitment;
    }

    private Vector3 GetTrackDirection()
    {
        if (aiDriver.waypoints.Length <= 1) return transform.forward;

        Vector3 currentPos = aiDriver.GetCurrentWaypointPosition();
        Vector3 nextPos = aiDriver.GetNextWaypointPosition();
        return (nextPos - currentPos).normalized;
    }

    private void SlowDownOnCorner()
    {
        Vector3 curTarget = aiDriver.CurTarget;
        float distanceToCorner = Vector3.Distance(transform.position, curTarget);
        float speedModifier = CalculateSpeedModifer(distanceToCorner); // without personality

        speedModifier = ApplyPersonalityToCornerSpeed(speedModifier); // with personality :D
        aiDriver.SetSpeedModifier(speedModifier);
    }

    private float CalculateSpeedModifer(float distanceToCorner)
    {
        if (distanceToCorner > distanceBeforeSlowDown)
            return 1f; // No slowdown 

        // interpolate full speed to slow down
        float distanceRatio = distanceToCorner / distanceBeforeSlowDown;
        return Mathf.Lerp(slowDownFactor, 1f, distanceRatio);
    }

    private float ApplyPersonalityToCornerSpeed(float baseSpeedModifier)
    {
        if (personality == null) return baseSpeedModifier;

        float agroBonous = personality.aggressiveness * 0.3f; // 30% bonus
        return Mathf.Clamp(baseSpeedModifier + agroBonous, 0.3f, 1f);
    }
    private void Update()
    {
        if (personality == null || aiDriver == null) return;

        if (aiDriver.CurrentWaypointIndex != lastWayPointIndex)
        {
            UpdateTargetWithOffset();
            lastWayPointIndex = aiDriver.CurrentWaypointIndex;
        }
        SlowDownOnCorner();
    }

    private void OnDrawGizmos()
    {
        if (aiDriver == null) return;

        Vector3 curTarget = aiDriver.CurTarget;
        float distanceToCorner = Vector3.Distance(transform.position, curTarget);
        Vector3 directionToTarget = (curTarget - transform.position).normalized;

        Gizmos.color = distanceToCorner <= distanceBeforeSlowDown ? Color.red : Color.green;
        Gizmos.DrawRay(transform.position, directionToTarget * distanceToCorner);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, directionToTarget * distanceBeforeSlowDown); // slowdown threshold
        
        // Slowdown threshold sphere
        Gizmos.color = Color.blue;
        Vector3 slowdownPoint = transform.position + (directionToTarget * distanceBeforeSlowDown);
        Gizmos.DrawWireSphere(slowdownPoint, 0.8f);

        // Speed modifier visualization
        float speedMod = CalculateSpeedModifer(distanceToCorner);
        Gizmos.color = Color.Lerp(Color.red, Color.green, speedMod);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * speedMod);
        
#if UNITY_EDITOR
        // Draw text labels in editor
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
        $"Distance: {distanceToCorner:F1}m\nSpeed: {speedMod:F2}");
    
    UnityEditor.Handles.Label(slowdownPoint + Vector3.up, 
        $"Slowdown Threshold\n{distanceBeforeSlowDown:F1}m");
#endif
    }


}
