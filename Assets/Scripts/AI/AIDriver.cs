using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Cart))]
public class AIDriver : MonoBehaviour
{
    [Header("Drive Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float turnSpeed = 5f;
    public float lookAheadDistance = 1f;
    public float cornerSlowdown = 0.7f;
    public float baseSpeedMultiplier = 1;

    [Header("Spline Path")]
    public SplineContainer spline;
    // public SplineCornerDetector cornerDetector;

    [Header("Physics")]
    [SerializeField] private LayerMask ground;
    [SerializeField] private float groundCheckDistance = 1f; 

    // Private 
    private float curSpeed;
    private float targetSpeed = 0f;
    private float3 targetPos;
    private float3 lookAheadPos;
    private float3 splineForward;
    private float splinePos = 0f; // spline position in normalized form (0 to 1)
    private float splineLength;
    private bool isInitialized = false;

    public bool showGizmos = true; 

    // Lane offset
    private Vector3 currentOffset = Vector3.zero;

    // Refs
    private Rigidbody rb;
    private Cart thisCart;

    // State Management
    private AIStateController stateController;


    // getters 
    public Cart ThisCart => thisCart;
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
        FindClosestSplinePos();
        UpdateTargetPos();
    }

    private void InitializeSpline()
    {
        if (spline == null || spline.Spline == null || spline.Spline.Count < 2)
        {
            Debug.LogError("No valid spline assigned to AIDriver!");
            return;
        }


        splineLength = spline[0].GetLength();
        FindClosestSplinePos();
        UpdateTargetPos(); // initial target

        isInitialized = true;
    }


    private void FixedUpdate()
    {
        if (!isInitialized || stateController == null) return;

        if (stateController.CanMove())
        {
            UpdateSplinePos();
            UpdateTargetPos();
            CalculateTargetSpeed();
            HandleMovement();
        }
    }

    private void UpdateSplinePos()
    {

        float3 carPos = transform.position;
        float closestDistance = float.MaxValue;
        float closestT = splinePos; // Start search near current progress

        int samples = 50;
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            float3 splinePoint = spline.EvaluatePosition(t);
            float distance = math.distance(carPos, splinePoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }

        splinePos = closestT;
    }

    private void UpdateTargetPos()
    {
        float dynamicLookAhead = lookAheadDistance * (1f + (curSpeed / maxSpeed) * 0.5f);

        float lookAheadT = dynamicLookAhead / splineLength;
        float targetT = splinePos + lookAheadT;
        float lookAheadT2 = (dynamicLookAhead * 2f) / splineLength;
        float lookAheadTargetT = splinePos + lookAheadT2;

        // Wrap around if necessary
        if (targetT > 1f) targetT -= 1f;
        if (lookAheadTargetT > 1f) lookAheadTargetT -= 1f;

        // Get positions and tangent from spline
        targetPos = spline.EvaluatePosition(targetT);
        lookAheadPos = spline.EvaluatePosition(lookAheadTargetT);
        splineForward = math.normalize(spline.EvaluateTangent(targetT));


        // Apply currentOffset (set by SetLaneOffset)
        targetPos += (float3)currentOffset;
        lookAheadPos += (float3)currentOffset;
    }

    private void CalculateTargetSpeed()
    {
       targetSpeed = maxSpeed;
        
        // Apply state speed multiplier
        targetSpeed *= GetStateSpeedMultiplier();
        
        // Slow down for corners using spline curvature
        float curvature = GetSplineCurvature(splinePos);
        if (curvature > 0.1f)
        {
            float curvatureFactor = Mathf.Clamp01(curvature * 5f); // Adjust multiplier as needed
            targetSpeed *= Mathf.Lerp(1f, cornerSlowdown, curvatureFactor);
        }
        
        // Random variation 
        targetSpeed *= (1f + Mathf.Sin(Time.time * 0.5f + GetInstanceID()) * 0.05f);
        
        // Apply base speed multiplier
        targetSpeed *= baseSpeedMultiplier;
    }

    float GetSplineCurvature(float t)
    {
        float delta = 0.01f;
        float t1 = Mathf.Clamp01(t - delta);
        float t2 = Mathf.Clamp01(t + delta);
        
        float3 tangent1 = math.normalize(spline.EvaluateTangent(t1));
        float3 tangent2 = math.normalize(spline.EvaluateTangent(t2));
        
        // Calculate angle between tangents as approximation of curvature
        float angle = math.acos(math.clamp(math.dot(tangent1, tangent2), -1f, 1f));
        return angle / (2f * delta); // Normalize by delta
    }

    private void HandleMovement()
    {
        if (rb == null) return;

        // Steer calc
        Vector3 directionToTarget = ((Vector3)targetPos - transform.position).normalized;
        float steerAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
        
        // Apply steering with smooth input
        float steerInput = Mathf.Clamp(steerAngle / 30f, -1f, 1f);
        Vector3 steerForce = transform.right * steerInput * turnSpeed;
        
        // Acceleration/Braking
        float speedDifference = targetSpeed - curSpeed;
        
        if (speedDifference > 0)
        {
            float motorInput = Mathf.Clamp01(speedDifference / 5f);
            curSpeed += acceleration * motorInput * Time.deltaTime;
        }
        else
        {
            curSpeed -= 10f * Time.deltaTime;
        }
        
        curSpeed = Mathf.Clamp(curSpeed, 0f, maxSpeed * 1.2f); // Allow slight overspeed
        
        // Apply forces only when grounded
        if (IsGrounded())
        {
            Vector3 forwardForce = transform.forward * curSpeed;
            rb.AddForce(forwardForce + steerForce, ForceMode.Acceleration);
            
            // Apply downforce for better handling at high speeds
            float downforceAmount = (curSpeed / maxSpeed) * 2f;
            rb.AddForce(-transform.up * downforceAmount, ForceMode.Acceleration);
        }
        
        // Smooth rotation towards spline direction
        Vector3 targetDirection = (Vector3)splineForward;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    public void ClampVelocity(float curMaxSpeed)
    {
        if (rb.velocity.magnitude > curMaxSpeed)
            rb.velocity = rb.velocity.normalized * curMaxSpeed;
    }

    private void FindClosestSplinePos()
    {
        float3 carPos = transform.position;
        float closestDistance = float.MaxValue;
        float closestT = 0f;
        
        // Sample spline at regular intervals to find closest point
        int samples = 100;
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            float3 splinePoint = spline.EvaluatePosition(t);
            float distance = math.distance(carPos, splinePoint);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }
        
        splinePos = closestT;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -transform.up, groundCheckDistance, ground);
    }

    #region State 
     private float GetStateSpeedMultiplier()
    {
        if (stateController == null) return 1f;

        switch (stateController.currentState)
        {
            case AIDriverState.CornerSlowing: return 0.5f;
            case AIDriverState.SpinningOut: return 0.3f;
            case AIDriverState.Recovering: return 0.8f;
            case AIDriverState.Boosting: return stateController.GetBoostMultiplier();
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

    #endregion

    #region Public Methods
    public void ApplyBoost(float boostDuration, float speedMultiplier)
    {
        if (!stateController.CanUseItems()) return; // Don't apply boost during certain states
        stateController.StartBoost(boostDuration, speedMultiplier); // enter state with modifier
    }

    public void SpinOut(float duration)
    {
        stateController.StartSpinOut(duration);
    }

    public void SetTargetOffset(Vector3 offset)
    {
        currentOffset = offset;
    }
    public void SetMaxSpeed(float speed)
    {
        maxSpeed = speed * baseSpeedMultiplier;
    }
    
    public float GetCurrentSpeed()
    {
        return curSpeed;
    }
    
    public float GetSplineProgress()
    {
        return splinePos;
    }
    
    public Vector3 GetSplinePosition()
    {  
        return spline.EvaluatePosition(splinePos);
    }

    public void SetLaneOffset(float laneOffset)
    {
        // Get the right vector at the current spline position
        Vector3 splineTangent = spline.EvaluateTangent(splinePos);
        Vector3 splineUp = Vector3.up; // Or use the spline's up if available
        Vector3 splineRight = Vector3.Cross(splineUp, splineTangent).normalized;

        currentOffset = splineRight * laneOffset;
    }

    // Methods for AIStateController
    public Rigidbody GetRigidbody() => rb;
    public float GetAcceleration() => acceleration;

    #endregion

    private void OnDrawGizmos()
    {
        if (spline == null || !showGizmos) return;

        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPos, 0.5f);
        
        // Draw look-ahead position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(lookAheadPos, 0.3f);
        
        // Draw line to target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPos);
        
        // Draw forward direction from spline
        Gizmos.color = Color.green;
        Gizmos.DrawRay(targetPos, (Vector3)splineForward * 2f);

        // Draw current position on spline
        Gizmos.color = Color.cyan;
        Vector3 currentSplinePos = spline.EvaluatePosition(splinePos);
        Gizmos.DrawWireSphere(currentSplinePos, 0.4f);
        
        // Draw spline path
        Gizmos.color = Color.white;
        int segments = 100;
        Vector3 previousPoint = spline.EvaluatePosition(0f);
        
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 currentPoint = spline.EvaluatePosition(t);
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        

        // Ground check
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, -transform.up * groundCheckDistance);
        
        // speed indicator
        Gizmos.color = Color.Lerp(Color.green, Color.red, curSpeed / maxSpeed);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"Speed: {curSpeed:F2}"
            );
        #endif
    }
}
