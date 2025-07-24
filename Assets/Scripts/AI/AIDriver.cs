using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Cart))]
public class AIDriver : MonoBehaviour
{
    [Header("Drive Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 10f;  
    public float turnSpeed = 5f;
    public float lookAheadDistance = 5f;

    [Header("Spline Path")]
    public SplineContainer spline;
    
    // Spline tracking
    private float splineProgress = 0f;
    private float splineLength;
    private bool isInitialized = false;
    
    // Lane offset
    private Vector3 currentOffset = Vector3.zero;
    
    // Refs
    private Rigidbody rb;
    private Cart thisCart;

    // State Management
    private AIStateController stateController;


    // getters 
    public Cart ThisCart => thisCart;

    private void Awake()
    {
        thisCart = GetComponent<Cart>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // Lower center of mass for stability
        
        // Initialize state controller
        stateController = gameObject.AddComponent<AIStateController>();
        stateController.Initialize(this);
    }

    private void Start()
    {
        if (spline == null)
        {
            Debug.LogError("AIDriver requires a SplineContainer to be assigned!");
            return;
        }
        
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
        if (!isInitialized || stateController == null) return;
        
        // Only move normally if state allows it StateController.CanMove() 
        if (stateController.IsInState(AIDriverState.Normal))
        {
            MoveAlongSpline();
        }
    }

    private void MoveAlongSpline()
    {
        // Simple constant progress along spline
        float speedMultiplier = stateController.IsInState(AIDriverState.Boosting) ? 1.5f : 1f;

        float progressSpeed = maxSpeed * speedMultiplier / splineLength; // progress per second
        // float progressSpeed = Mathf.Lerp(rb.velocity.magnitude, maxSpeed, 0.5f) / splineLength;
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
        rb.AddForce(directionToSpline * acceleration, ForceMode.Acceleration);

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed; 
        }
    }

    public Vector3 GetSplineDirection()
    {
        return GetSplineDirection(splineProgress);
    }


    private Vector3 GetSplineDirection(float progress)
    {
        float sampleDistance = 0.01f; // Sample distance for direction calculation
        float nextProgress = Mathf.Min(1f, progress + sampleDistance);

        Vector3 currentPos = spline.EvaluatePosition(progress);
        Vector3 nextPos = spline.EvaluatePosition(nextProgress);

        return (nextPos - currentPos).normalized;
    }


    #region Public Methods

    // temp override maxSpeed for boost
    public void ApplyBoost(float force)
    {
        if (!stateController.CanUseItems()) return; // Don't apply boost during certain states


        float boostDuration = 3f; // Default boost duration
        float speedMultiplier = 1.5f; // Default speed multiplier
        stateController.StartBoost(boostDuration, speedMultiplier);
    }
    
    public void SpinOut(float duration)
    {
        stateController.StartSpinOut(duration);
    }

    public float GetCurrentSplineProgress()
    {
        return splineProgress;
    }

    public void SetTargetOffset(Vector3 offset)
    {
        currentOffset = offset;
    }
    
    // Methods for AIStateController
    public Rigidbody GetRigidbody() => rb;
    public float GetAcceleration() => acceleration;
    
    #endregion

    private void OnDrawGizmos()
    {
        if (!isInitialized || spline == null) return;

        // Current position on spline - center (blue)
        Vector3 currentSplinePos = spline.EvaluatePosition(splineProgress);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentSplinePos, 0.3f);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(currentSplinePos, "Spline Center");
        #endif
        
        // Current offset position - where AI goes (red)
        Vector3 currentOffsetPos = currentSplinePos + currentOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentOffsetPos, 0.5f);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(currentOffsetPos, "AI Offset Position");
        #endif
        
        // Line showing offset from center to lane
        if (currentOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentSplinePos, currentOffsetPos);
        }

        // Dynamically calculate lookAheadDistance
        float dynamicLookAheadDistance = Mathf.Lerp(2f, 10f, rb.velocity.magnitude / maxSpeed);

        // Target position ahead - center (cyan)
        float targetProgress = splineProgress + (dynamicLookAheadDistance / splineLength);
        if (targetProgress > 1f)
        {
            targetProgress = spline.Spline.Closed ? targetProgress - 1f : 1f;
        }
        Vector3 targetPos = spline.EvaluatePosition(targetProgress);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPos, 0.3f);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(targetPos, "Spline Target");
        #endif
        
        // Target offset position - where AI aims (green)
        Vector3 targetOffsetPos = targetPos + currentOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetOffsetPos, 0.6f);
        Gizmos.DrawLine(transform.position, targetOffsetPos);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(targetOffsetPos, "AI Target Position");
        #endif

        // Forward direction
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
