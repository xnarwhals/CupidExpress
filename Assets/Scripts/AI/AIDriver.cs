using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
public class AIDriver : MonoBehaviour
{
    [Header("Drive Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 10f;  
    public float turnSpeed = 5f;
    public float lookAheadDistance = 5f;

    [Header("Spline Path")]
    public SplineContainer spline;

    // Components
    private Rigidbody rb;
    
    // Spline tracking
    private float splineProgress = 0f;
    private float splineLength;
    private bool isInitialized = false;
    
    // Lane offset
    private Vector3 currentOffset = Vector3.zero;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // Lower center of mass for stability
    }

    private void Start()
    {
        InitializeSpline();
    }
    
    private void InitializeSpline()
    {
        if (spline == null || spline.Spline == null || spline.Spline.Count < 2) 
        {
            Debug.LogError("No valid spline assigned to AIDriver!");
            return;
        }

        splineLength = spline.CalculateLength();

        // t is the normalized progress along the spline (0 to 1)
        SplineUtility.GetNearestPoint(spline.Spline, transform.position, out float3 closestPoint, out float t);
        splineProgress = t / spline.Spline.Count;
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        MoveAlongSpline();
    }

    private void MoveAlongSpline()
    {
        // Simple constant progress along spline
        float progressSpeed = maxSpeed / splineLength; // progress per second
        splineProgress += progressSpeed * Time.fixedDeltaTime;
        
        // loop
        if (splineProgress > 1f)
        {
            splineProgress = spline.Spline.Closed ? 0f : 1f;
        }
        
        // current vector3 with offset applied
        Vector3 curSplinePosition = spline.EvaluatePosition(splineProgress);
        Vector3 curOffsetPosition = curSplinePosition + currentOffset;
        
        // Get target position slightly ahead
        float targetProgress = splineProgress + (lookAheadDistance / splineLength);
        if (targetProgress > 1f)
        {
            targetProgress = spline.Spline.Closed ? targetProgress - 1f : 1f;
        }

        Vector3 nextSplinePosition = spline.EvaluatePosition(targetProgress);
        Vector3 nextOffsetPosition = nextSplinePosition + currentOffset;
        
        // Point toward target
        Vector3 directionToTarget = (nextOffsetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        
        // Move toward current spline position at constant speed
        Vector3 directionToSpline = (curOffsetPosition - transform.position).normalized;
        rb.velocity = directionToSpline * maxSpeed;
    }



    #region Public Methods

    public float GetCurrentSplineProgress()
    {
        return splineProgress;
    }

    public void SetTargetOffset(Vector3 offset)
    {
        currentOffset = offset;
    }
    
    #endregion

    private void OnDrawGizmos()
    {
        if (!isInitialized || spline == null) return;

        // Current position on spline - center (blue)
        Vector3 currentSplinePos = spline.EvaluatePosition(splineProgress);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentSplinePos, 0.3f);
        
        // Current offset position - where AI goes (red)
        Vector3 currentOffsetPos = currentSplinePos + currentOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentOffsetPos, 0.5f);
        
        // Line showing offset from center to lane
        if (currentOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentSplinePos, currentOffsetPos);
        }

        // Target position ahead - center (cyan)
        float targetProgress = splineProgress + (lookAheadDistance / splineLength);
        if (targetProgress > 1f)
        {
            targetProgress = spline.Spline.Closed ? targetProgress - 1f : 1f;
        }
        Vector3 targetPos = spline.EvaluatePosition(targetProgress);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPos, 0.3f);
        
        // Target offset position - where AI aims (green)
        Vector3 targetOffsetPos = targetPos + currentOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetOffsetPos, 0.6f);
        Gizmos.DrawLine(transform.position, targetOffsetPos);

        // Forward direction
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
