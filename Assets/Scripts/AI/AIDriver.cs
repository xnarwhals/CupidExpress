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
        SplineUtility.GetNearestPoint(spline.Spline, transform.position, out float3 closestPoint, out float t);
        splineProgress = t / spline.Spline.Count; 
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
        // Get current position on spline
        Vector3 currentSplinePos = spline.EvaluatePosition(splineProgress);
        Vector3 targetPosition = currentSplinePos + currentOffset;
        
        // look ahead for forward direction 
        Vector3 toTarget = targetPosition - transform.position;
        rb.AddForce(toTarget.normalized * acceleration, ForceMode.Acceleration);
        
        // Simple rotation toward target
        if (toTarget.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                turnSpeed * Time.fixedDeltaTime);
        }
        
        // Update spline progress based on movement
        UpdateSplineProgress();
        
        ClampVelocity();
        
        ApplyStateEffects();
    }
    private float WrapSplineProgress(float progress)
    {
        progress = progress % 1f; 
        if (progress < 0f)
            progress += 1f; 
        return progress;
    }

    private void ClampVelocity()
    {
        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    private void UpdateSplineProgress()
    {
        Vector3 splineDirection = GetSplineDirection(splineProgress);
        float forwardSpeed = Vector3.Dot(rb.velocity, splineDirection);

        forwardSpeed = Mathf.Max(0f, forwardSpeed);

        splineProgress += forwardSpeed / splineLength * Time.fixedDeltaTime;
        splineProgress = WrapSplineProgress(splineProgress);
    }

    private void ApplyStateEffects()
    {
        if (stateController == null) return;
        switch (stateController.currentState)
        {
            case AIDriverState.CornerSlowing:
                rb.velocity *= 0.7f;
                break;

            case AIDriverState.SpinningOut:
                rb.velocity *= 0.95f;
                rb.AddTorque(Vector3.up * 30f, ForceMode.Acceleration);
                break;

            case AIDriverState.Recovering:
                rb.velocity *= 0.8f;
                break;

            case AIDriverState.Boosting:
                float boostSpeed = maxSpeed * 1.5f;
                if (rb.velocity.magnitude < boostSpeed)
                    rb.AddForce(GetSplineDirection(splineProgress) * acceleration * 0.5f, ForceMode.Acceleration);
                break;

            case AIDriverState.Stunned:
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                break;

            default:
                break;

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
    public void ApplyBoost(float boostDuration = 3f, float speedMultiplier = 1.5f)
    {
        if (!stateController.CanUseItems()) return; // Don't apply boost during certain states
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

        float targetProgress = splineProgress + (lookAheadDistance / splineLength);
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

        // Velocity 
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Velocity: {rb.velocity.magnitude:F1}\nProgress: {splineProgress:F3}\n");
#endif
            
        }
    }
}
