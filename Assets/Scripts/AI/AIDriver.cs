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

    // other
    private bool isSpinningOut = false;
    private float spinOutTimer = 0f;
    private float spinOutDuration = 2f; // placeholder 2s
    private Quaternion originalRotation;
    private bool isRecovering = false;
    private float recoverTimer = 0f;
    private float recoverTime = 1.5f; // Time to recover from spin out
    private Quaternion targetRotation;


    // getters 
    public Cart ThisCart => thisCart;

    private void Awake()
    {
        thisCart = GetComponent<Cart>();
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
        
        if (isSpinningOut)
        {
            HandleSpinOut();
            return;
        }

        if (isRecovering)
        {
            HandleRecovery();
            return;
        }

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

    private void HandleSpinOut()
    {
        spinOutTimer += Time.fixedDeltaTime;

        float breakIntensity = Mathf.Lerp(0.3f, 0.8f, spinOutTimer / spinOutDuration);
        float breakForce = acceleration * breakIntensity;
        rb.AddForce(-rb.velocity.normalized * breakForce, ForceMode.VelocityChange); 

        float angleDamp = Mathf.Lerp(0.1f, 0.9f, spinOutTimer / spinOutDuration);
        rb.angularVelocity *= (1f - angleDamp * Time.fixedDeltaTime);

        if (spinOutTimer >= spinOutDuration)
        {
            EndSpinOut();
        }
        
        UpdateSplineProgress();
    }

    private void EndSpinOut()
    {
        isSpinningOut = false;
        spinOutTimer = 0f;

        // Reset rotation to original
        rb.angularVelocity = Vector3.zero;
        rb.velocity *= 0.3f;

        // Start recovery
        StartRecovery();
    }

    private void StartRecovery()
    {
        isRecovering = true;
        recoverTimer = 0f;

        Vector3 splineDirection = GetSplineDirection(splineProgress);
        targetRotation = Quaternion.LookRotation(splineDirection);
    }

    private void HandleRecovery()
    {
        recoverTimer += Time.fixedDeltaTime;

        float recoveryProgress = recoverTimer / recoverTime;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 3f * recoveryProgress);
        Vector3 splineDirection = GetSplineDirection(splineProgress);
        float speedMultiplier = Mathf.Lerp(0.3f, 1f, recoveryProgress);
        float targetSpeed = maxSpeed * speedMultiplier;

        rb.AddForce(splineDirection * acceleration * speedMultiplier, ForceMode.Acceleration);

        if (rb.velocity.magnitude > targetSpeed)
        {
            rb.velocity = rb.velocity.normalized * targetSpeed; // Clamp speed
        }

        float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);
        
        if (recoverTimer >= recoverTime || rotationDifference < 5f)
        {
            isRecovering = false; // Reset after recovery
            recoverTimer = 0f;
            transform.rotation = targetRotation;
        }
        
        UpdateSplineProgress();
    }

    private void UpdateSplineProgress()
    {
        Vector3 splineDirection = GetSplineDirection(splineProgress);
        float fowardMovement = Vector3.Dot(rb.velocity, splineDirection);

        if (fowardMovement > 0f)
        {
            float progressIncrement = fowardMovement / splineLength * Time.fixedDeltaTime;
            splineProgress += progressIncrement;
            if (splineProgress > 1f)
            {
                splineProgress = spline.Spline.Closed ? 0f : 1f;
            }
        }
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
        if (isSpinningOut) return; // Don't apply boost while spinning out
        Debug.Log("Boosting");

        float boostedSpeed = Mathf.Clamp(maxSpeed + force, 0f, maxSpeed * 2f);
        rb.velocity = transform.forward * boostedSpeed;
    }
    
    public void SpinOut(float duration)
    {
        if (isSpinningOut) return; // no perma spin out

        isSpinningOut = true;
        spinOutTimer = 0f;
        spinOutDuration = duration;

        originalRotation = transform.rotation;
        Vector3 ySpin = Vector3.up * UnityEngine.Random.Range(-1f, 1f);
        rb.AddTorque(ySpin * 1000f, ForceMode.VelocityChange); // Random spin force
    }

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
