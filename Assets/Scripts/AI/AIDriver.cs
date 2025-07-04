using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIDriver : MonoBehaviour
{
    [Header("Drive Settings")]
    public float maxSpeed = 15f; // 15 m/s
    public float acceleration = 10f; // 10 m/s^2
    public float turnSpeed = 2f;
    public float distanceThreshold = 1f; // 1 meter threshold for waypoint switching

    [Header("Waypoint Path")]
    [Tooltip("Waypoints that all CPU will follow in order")]
    public Transform[] waypoints; // all CPUs follow this center path + behavioral variation 
    private int curWaypointIndex = 0;

    // Stuff
    private Rigidbody rb;
    private Vector3 curTarget;
    private float curSpeedModifer = 1f;

    // Getters
    public int CurrentWaypointIndex => curWaypointIndex;
    public Vector3 CurTarget => curTarget;
    public Transform[] Waypoints => waypoints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.2f, 0); // Lower center of mass for stability
    }

    private void Start()
    {
        if (waypoints.Length > 0) curTarget = waypoints[0].position;
        else Debug.LogWarning("No waypoints assigned to AIDriver!");
    }

    private void FixedUpdate()
    {
        if (waypoints.Length == 0 || rb == null) return;
        if (GameManager.Instance.GetCurrentRaceState() != GameManager.RaceState.Racing) return;

        // Move Logic
        Vector3 direction = (curTarget - transform.position).normalized;

        // "Steer"
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        // Speed control
        float altMaxSpeed = maxSpeed * curSpeedModifer;
        if (rb.velocity.magnitude < altMaxSpeed)
        {
            rb.AddForce(direction * acceleration, ForceMode.Acceleration);
        }

        // Waypoint progression
        float distanceToTarget = Vector3.Distance(transform.position, curTarget);
        if (distanceToTarget < distanceThreshold) // 1 meter threshold
        {
            curWaypointIndex = (curWaypointIndex + 1) % waypoints.Length; // Loop through waypoints
        }
    }

    #region Public Methods
    public Vector3 GetCurrentWaypointPosition()
    {
        if (curWaypointIndex < 0 || curWaypointIndex >= waypoints.Length)
            return Vector3.zero;

        return waypoints[curWaypointIndex].position;
    }

    public Vector3 GetNextWaypointPosition()
    {
        int nextIndex = (curWaypointIndex + 1) % waypoints.Length; // loop
        return waypoints[nextIndex].position;
    }

    public void SetTarget(Vector3 newTarget)
    {
        curTarget = newTarget;
    }

    public void SetSpeedModifier(float modifier)
    {
        curSpeedModifer = Mathf.Clamp(modifier, 0.1f, 2f); // Clamp between 0.1 and 2
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (curTarget != Vector3.zero)
        {
            // Cyan sphere at offset position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(curTarget, 0.8f); 
            
            // Follow Path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, curTarget);
        }

        // Show base waypoint for comparison
        if (waypoints.Length > 0 && curWaypointIndex < waypoints.Length)
        {
            // Red sphere at non offset waypoint
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(waypoints[curWaypointIndex].position, 0.5f); 
        }
        
    }
}
