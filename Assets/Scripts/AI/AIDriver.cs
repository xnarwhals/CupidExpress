using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Cart))]
public class AIDriver : MonoBehaviour
{
    [Header("Drive Settings")]
    public float maxSpeed = 21f;
    public float acceleration = 10f;  
    public float turnSpeed = 5f;
    public float lookAheadDistance = 5f;

    [Header("Spline Path")]
    public SplineContainer spline;
    public SplineCornerDetector cornerDetector; 
    
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
    public float SplineLength => splineLength;
    public float SplineProgress => splineProgress;
    public AIStateController StateController => stateController;

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
        Debug.Log($"Spline Length: {splineLength:F2}");

        SplineUtility.GetNearestPoint(spline.Spline, transform.position, out float3 closestPoint, out float t);
        splineProgress = t / spline.Spline.Count; // 
        isInitialized = true;
    }


    private void FixedUpdate()
    {
        if (!isInitialized || stateController == null) return;

        if (stateController.CanMove())
        {
            MoveAlongSpline();
        }
    }

    private void MoveAlongSpline()
    {
        // Calculate look-ahead progress along the spline
        float lookAheadProgress = splineProgress + (lookAheadDistance / splineLength);
        lookAheadProgress = WrapSplineProgress(lookAheadProgress);

        // Get direction and right vector at look-ahead point
        Vector3 splineDir = GetSplineDirection(lookAheadProgress);
        Vector3 splineRight = Vector3.Cross(Vector3.up, splineDir).normalized;

        // Calculate the look-ahead position and apply lane offset
        Vector3 lookAheadSplinePos = spline.EvaluatePosition(lookAheadProgress);
        Vector3 desiredOffsetPos = lookAheadSplinePos + currentOffset; // currentOffset set by AIBehaviorController

        // Move towards the offset target
        Vector3 toTarget = desiredOffsetPos - transform.position;
        float lateralError = Vector3.Dot(toTarget, splineRight);

        // Forward force along the spline
        rb.AddForce(splineDir * acceleration * GetStateAccelerationMultiplier(), ForceMode.Acceleration);

        // Lateral correction to stay in lane
        float lateralCorrectionStrength = 0.5f;
        rb.AddForce(splineRight * lateralError * lateralCorrectionStrength, ForceMode.Acceleration);

        // Rotate to face movement direction or spline direction if nearly stopped
        Vector3 flatVelocity = rb.velocity; flatVelocity.y = 0f;
        if (flatVelocity.magnitude > 0.5f)
        {
            // Face velocity
            Quaternion targetRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Face spline direction
            Quaternion targetRotation = Quaternion.LookRotation(splineDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }

        // Update progress and apply state effects
        UpdateSplineProgress();
        ClampVelocity(maxSpeed * GetStateSpeedMultiplier());
    }
    private float GetStateSpeedMultiplier()
    {
        if (stateController == null) return 1f;
        
        switch (stateController.currentState)
        {
            case AIDriverState.CornerSlowing: return 0.5f;
            case AIDriverState.SpinningOut: return 0.3f;
            case AIDriverState.Recovering: return 0.8f;
            case AIDriverState.Boosting: return 1.5f;
            case AIDriverState.Stunned: return 0f;
            default: return 1f;
        }
    }

    private float GetStateAccelerationMultiplier()
    {
        if (stateController == null) return 1f;
        
        switch (stateController.currentState)
        {
            case AIDriverState.CornerSlowing: return 0.6f;
            case AIDriverState.SpinningOut: return 0.1f;
            case AIDriverState.Recovering: return 0.6f;
            case AIDriverState.Boosting: return 1.2f;
            case AIDriverState.Stunned: return 0f;
            default: return 1f;
        }
    }
    public float WrapSplineProgress(float progress)
    {
        progress = progress % 1f;
        if (progress < 0f)
            progress += 1f;
        return progress;
    }

    public void ClampVelocity(float curMaxSpeed)
    {
        if (rb.velocity.magnitude > curMaxSpeed)
            rb.velocity = rb.velocity.normalized * curMaxSpeed;
    }

    private void UpdateSplineProgress()
    {
        Vector3 splineDirection = GetSplineDirection(splineProgress);
        float forwardSpeed = Vector3.Dot(rb.velocity, splineDirection);

        forwardSpeed = Mathf.Max(0f, forwardSpeed);

        splineProgress += forwardSpeed / splineLength * Time.fixedDeltaTime;
        splineProgress = WrapSplineProgress(splineProgress);
    }

    public Vector3 GetSplineDirection()
    {
        return GetSplineDirection(splineProgress);
    }

    public Vector3 GetSplineDirection(float progress)
    {
        float sampleDistance = 0.01f; // Sample distance for direction calculation
        // float nextProgress = Mathf.Min(1f, progress + sampleDistance);
        float nextProgress = WrapSplineProgress(progress + sampleDistance);

        Vector3 currentPos = spline.EvaluatePosition(progress);
        Vector3 nextPos = spline.EvaluatePosition(nextProgress);

        return (nextPos - currentPos).normalized;
    }


    #region Public Methods
    public void ApplyBoost(float boostDuration = 3f, float speedMultiplier = 1.5f)
    {
        if (!stateController.CanUseItems()) return; // Don't apply boost during certain states
        stateController.StartBoost(boostDuration, speedMultiplier);
    }
    
    public void SpinOut(float duration)
    {
        stateController.StartSpinOut(duration);
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

        // 1. Spline center (blue)
        Vector3 currentSplinePos = spline.EvaluatePosition(splineProgress);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentSplinePos, 0.3f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(currentSplinePos, $"Spline Center\nProgress: {splineProgress:F3}");
#endif

        // 2. AI Offset Position (red)
        Vector3 currentOffsetPos = currentSplinePos + currentOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentOffsetPos, 0.5f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(currentOffsetPos, "AI Offset Position");
#endif

        // 3. Line from spline center to offset
        if (currentOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentSplinePos, currentOffsetPos);
        }

        // 4. Look-ahead target (green)
        float lookAheadProgress = splineProgress + (lookAheadDistance / splineLength);
        lookAheadProgress = WrapSplineProgress(lookAheadProgress);
        Vector3 lookAheadSplinePos = spline.EvaluatePosition(lookAheadProgress);
        Vector3 lookAheadTarget = lookAheadSplinePos + currentOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lookAheadTarget, 0.6f);
        Gizmos.DrawLine(transform.position, lookAheadTarget);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(lookAheadTarget, $"LookAhead Target\nProgress: {lookAheadProgress:F3}");
#endif

        // 5. Draw AI's current position (magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // 6. Draw velocity vector (cyan)
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, rb.velocity);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Velocity: {rb.velocity.magnitude:F2}\nDir: {rb.velocity.normalized}\n");
#endif
        }

        // 7. Draw spline direction at current progress (white)
        Vector3 splineDir = GetSplineDirection(splineProgress);
        Gizmos.color = Color.white;
        Gizmos.DrawRay(currentSplinePos, splineDir * 2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(currentSplinePos + splineDir * 2f, $"Spline Dir\n{splineDir}");
#endif
    }
}
