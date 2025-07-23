using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Cart))]
public class AIDriver2 : MonoBehaviour
{
    [Header("Drive Settings")]
    public float maxSpeed = 27f; // 27 m/s = ~60 mph
    public float acceleration = 9f; // 9 m/s^2 = 3 seconds to reach max speed
    public float turnSpeed = 2f;
    public float swapToNextWaypointDistance = 1f; // 1 meter threshold for waypoint switching

    [Header("Waypoint Path")]
    [Tooltip("Waypoints that all CPU will follow in order")]
    public Waypoint[] waypoints; // all CPUs follow this center path + behavioral variation 
    private int curWaypointIndex = 0;

    // Refs
    private Rigidbody rb;
    private Vector3 curTarget;
    private Cart thisCart;

    // other
    private float curSpeedModifer = 1f; 
    private bool isSpinningOut = false;
    private float spinOutTimer = 0f;
    private float spinOutDuration = 2f; // placeholder 2s
    private Quaternion originalRotation;

    // Getters
    public int CurrentWaypointIndex => curWaypointIndex;
    public Vector3 CurTarget => curTarget;
    public Waypoint[] Waypoints => waypoints;
    public Cart ThisCart => thisCart;

    private void Awake()
    {
        thisCart = GetComponent<Cart>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.2f, 0); // Lower center of mass for stability
    }

    private void Start()
    {
        if (waypoints.Length > 0) curTarget = waypoints[0].transform.position;
        else Debug.LogWarning("No waypoints assigned to AIDriver!");
    }

    private void FixedUpdate()
    {
        if (waypoints.Length == 0 || rb == null) return;
        if (GameManager.Instance.GetCurrentRaceState() != GameManager.RaceState.Racing) return;

        if (isSpinningOut)
        {
            spinOutTimer += Time.fixedDeltaTime;
            if (spinOutTimer >= spinOutDuration)
            {
                isSpinningOut = false;
                spinOutTimer = 0f;
                transform.rotation = originalRotation;
                rb.angularVelocity = Vector3.zero;
            }

            rb.AddForce(-rb.velocity.normalized * acceleration, ForceMode.Acceleration); // hold your horses
            return;
        }


        // Move Logic
        Vector3 direction = (curTarget - transform.position).normalized;

        // "Steer"
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        // Speed control
        float altMaxSpeed = maxSpeed * curSpeedModifer; // 27 m/s * 1.3f = 35.1 m/s on the most agro

        if (rb.velocity.magnitude < altMaxSpeed)
        {
            rb.AddForce(direction * acceleration, ForceMode.Acceleration);
        }

        // Waypoint progression
        float distanceToTarget = Vector3.Distance(transform.position, curTarget);
        
        if (distanceToTarget < swapToNextWaypointDistance) // 1 meter threshold
        {
            curWaypointIndex = (curWaypointIndex + 1) % waypoints.Length; // Loop through waypoints
        }
    }

    public void SpinOut(float duration)
    {
        if (isSpinningOut) return;

        isSpinningOut = true;
        spinOutTimer = 0f;
        spinOutDuration = duration;

        originalRotation = transform.rotation;
        Vector3 ySpin = Vector3.up * Random.Range(-1f, 1f); 
        rb.AddTorque(ySpin * 1000f, ForceMode.VelocityChange); // Random spin force
    }

    public void ApplyBoost(float force)
    {
        if (isSpinningOut) return;
        rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
        
    }

    #region Public Methods
    public Vector3 GetCurrentWaypointPosition()
    {
        if (curWaypointIndex < 0 || curWaypointIndex >= waypoints.Length)
            return Vector3.zero;

        return waypoints[curWaypointIndex].transform.position;
    }

    public Vector3 GetNextWaypointPosition()
    {
        int nextIndex = (curWaypointIndex + 1) % waypoints.Length; // loop
        return waypoints[nextIndex].transform.position;
    }

    public bool NextWaypointIsCorner()
    {
        int nextIndex = (curWaypointIndex + 1) % waypoints.Length;
        return waypoints[nextIndex].isCorner;
    }

    public void SetTarget(Vector3 newTarget)
    {
        curTarget = newTarget;
    }

    public void SetSpeedModifier(float modifier)
    {
        curSpeedModifer = modifier;
    }

    // getter

    public bool IsSpinningOut() => isSpinningOut;

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
            Gizmos.DrawWireSphere(waypoints[curWaypointIndex].transform.position, 0.5f);
        }

    }
}
