using UnityEngine;

[System.Serializable]
public class AIStateController : MonoBehaviour
{
    [Header("State Management")]
    public AIDriverState currentState = AIDriverState.Normal;
    
    [Header("State Durations")]
    public float spinOutDuration = 2f;
    public float recoveryDuration = 1.5f;
    public float boostDuration = 3f;
    
    [Header("State Settings")]
    [Range(0.1f, 1f)]
    public float spinOutBrakeIntensity = 0.8f;
    [Range(0.1f, 1f)]
    public float recoverySpeedMultiplier = 0.3f;
    [Range(2f, 10f)]
    public float recoveryRotationSpeed = 3f;

    [Header("Spin Out Settings")]
    [Range(1f, 5f)]
    public float spinOutRotationSpeed = 2f; // rotations per second
    private float spinOutRotationVelocity = 0f; 
    
    // State tracking
    private float stateTimer = 0f;
    private Quaternion targetRecoveryRotation;
    private float originalMaxSpeed;
    
    // Components
    private AIDriver aiDriver;
    private Rigidbody rb;
    private Transform transform;
    
    // Events
    public System.Action<AIDriverState, AIDriverState> OnStateChanged;
    
    public void Initialize(AIDriver driver)
    {
        aiDriver = driver;
        rb = aiDriver.GetComponent<Rigidbody>();
        transform = aiDriver.transform;
        originalMaxSpeed = aiDriver.maxSpeed;
    }
    
    private void Update()
    {
        if (aiDriver == null) return;
        
        UpdateCurrentState();
    }
    
    private void UpdateCurrentState()
    {
        stateTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case AIDriverState.Normal:
                // Normal state - no special handling needed
                break;
                
            case AIDriverState.SpinningOut:
                HandleSpinOutState();
                break;
                
            case AIDriverState.Recovering:
                HandleRecoveryState();
                break;
                
            case AIDriverState.Boosting:
                HandleBoostState();
                break;
                
            case AIDriverState.Stunned:
                HandleStunnedState();
                break;
        }
    }
    
    #region State Transitions
    
    public bool TryChangeState(AIDriverState newState, float duration = 0f)
    {
        if (!CanTransitionTo(newState))
        {
            Debug.LogWarning($"Cannot transition from {currentState} to {newState}");
            return false;
        }
        
        AIDriverState oldState = currentState;
        ExitCurrentState();
        
        currentState = newState;
        stateTimer = 0f;
        
        if (duration > 0f)
        {
            SetStateDuration(newState, duration);
        }
        
        EnterNewState(newState);
        OnStateChanged?.Invoke(oldState, newState);
        
        Debug.Log($"AI State: {oldState} -> {newState}");
        return true;
    }
    
    private bool CanTransitionTo(AIDriverState newState)
    {
        // Define transition rules
        switch (currentState)
        {
            case AIDriverState.Normal:
                return true; // Can transition to any state from normal
                
            case AIDriverState.SpinningOut:
                return newState == AIDriverState.Recovering; // Spinout must go to recovery
                
            case AIDriverState.Recovering:
                return newState == AIDriverState.Normal || newState == AIDriverState.SpinningOut;
                
            case AIDriverState.Boosting:
                return newState != AIDriverState.Recovering; // Can't recover while boosting
                
            case AIDriverState.Stunned:
                return newState == AIDriverState.Normal;
                
            default:
                return false;
        }
    }
    
    private void SetStateDuration(AIDriverState state, float duration)
    {
        switch (state)
        {
            case AIDriverState.SpinningOut:
                spinOutDuration = duration;
                break;
            case AIDriverState.Recovering:
                recoveryDuration = duration;
                break;
            case AIDriverState.Boosting:
                boostDuration = duration;
                break;
        }
    }
    
    #endregion
    
    #region State Handlers
    
    private void HandleSpinOutState()
    {
        float progressRatio = stateTimer / spinOutDuration;

        float decelerateFactor = Mathf.Lerp(0.92f, 0.85f, progressRatio);
        rb.velocity *= decelerateFactor; // Gradual slowdown

        float spinThisFrame = spinOutRotationSpeed * 360f * Time.deltaTime;
        transform.Rotate(0, spinThisFrame, 0, Space.Self);

        if (progressRatio > 0.7f)
        {
            float spinReduction = Mathf.Lerp(1f, 0.1f, (progressRatio - 0.7f) / 0.3f);
            spinOutRotationSpeed *= spinReduction;
        } 
        
        // Transition to recovery when time is up
        if (stateTimer >= spinOutDuration)
        {
            TryChangeState(AIDriverState.Recovering);
        }
    }
    
    private void HandleRecoveryState()
    {
        float progressRatio = stateTimer / recoveryDuration;

        Vector3 splineDirection = aiDriver.GetSplineDirection();
        targetRecoveryRotation = Quaternion.LookRotation(splineDirection);
        
        // Smooth rotation towards spline direction
        if (targetRecoveryRotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRecoveryRotation,
                recoveryRotationSpeed * Time.deltaTime);
        }
        
        // Gradual speed increase
        float speedMultiplier = Mathf.Lerp(recoverySpeedMultiplier, 1f, progressRatio);
        float targetSpeed = originalMaxSpeed * speedMultiplier;
        
        rb.AddForce(splineDirection * aiDriver.acceleration * speedMultiplier, ForceMode.Acceleration);
        
        // Speed limiting
        if (rb.velocity.magnitude > targetSpeed)
        {
            rb.velocity = rb.velocity.normalized * targetSpeed;
        }
        
        // Check if recovery is complete
        float rotationDiff = Quaternion.Angle(transform.rotation, targetRecoveryRotation);
        if (stateTimer >= recoveryDuration || rotationDiff < 5f)
        {
            TryChangeState(AIDriverState.Normal);
        }
    }
    
    private void HandleBoostState()
    {
        // Boost effect gradually wears off
        float progressRatio = stateTimer / boostDuration;
        float boostMultiplier = Mathf.Lerp(1.5f, 1f, progressRatio);
        
        // Apply boost to max speed
        aiDriver.maxSpeed = originalMaxSpeed * boostMultiplier;
        
        if (stateTimer >= boostDuration)
        {
            TryChangeState(AIDriverState.Normal);
        }
    }
    
    private void HandleStunnedState()
    {
        // Completely stop movement
        rb.velocity *= 0.95f; // Gradual slowdown
        rb.angularVelocity *= 0.9f;
        
        // Auto-recover after duration (can be set externally)
        if (stateTimer >= 1f) // Default 1 second stun
        {
            TryChangeState(AIDriverState.Normal);
        }
    }
    
    #endregion
    
    #region State Entry/Exit
    
    private void EnterNewState(AIDriverState state)
    {
        switch (state)
        {
            case AIDriverState.SpinningOut:
                // Apply initial spinning force
                spinOutRotationVelocity = spinOutRotationSpeed;

                Vector3 randomSpin = Vector3.up * Random.Range(-0.3f, 0.3f);
                rb.AddTorque(randomSpin * 150f, ForceMode.VelocityChange);

                Vector3 sidewayForce = transform.right * Random.Range(-30f, 30f);
                rb.AddForce(sidewayForce, ForceMode.VelocityChange);

                break;
                
            case AIDriverState.Recovering:
                // Stop all spinning and calculate target rotation
                rb.velocity *= recoverySpeedMultiplier;
                
                Vector3 splineDirection = aiDriver.GetSplineDirection();
                targetRecoveryRotation = Quaternion.LookRotation(splineDirection);
                break;
                
            case AIDriverState.Normal:
                // Restore original max speed
                aiDriver.maxSpeed = originalMaxSpeed;
                spinOutRotationSpeed = 3f; 
                break;
        }
    }
    
    private void ExitCurrentState()
    {
        // Cleanup when leaving a state
        switch (currentState)
        {
            case AIDriverState.Recovering:
                // Ensure final rotation is set
                rb.angularVelocity = Vector3.zero;
                break;

            case AIDriverState.SpinningOut:
                rb.angularVelocity = Vector3.zero;
                break;
        }
    }
    
    #endregion
    
    #region Public Interface
    
    public void StartSpinOut(float duration)
    {
        TryChangeState(AIDriverState.SpinningOut, duration);
    }
    
    public void StartBoost(float duration, float speedMultiplier = 1.5f)
    {
        if (TryChangeState(AIDriverState.Boosting, duration))
        {
            aiDriver.maxSpeed = originalMaxSpeed * speedMultiplier;
        }
    }
    
    public void StartStun(float duration)
    {
        TryChangeState(AIDriverState.Stunned, duration);
    }
    
    public bool IsInState(AIDriverState state) => currentState == state;
    public bool CanMove() => currentState != AIDriverState.Stunned;
    public bool CanUseItems() => currentState == AIDriverState.Normal || currentState == AIDriverState.Boosting;
    
    public float GetStateProgress()
    {
        float duration = GetCurrentStateDuration();
        return duration > 0 ? stateTimer / duration : 0f;
    }
    
    private float GetCurrentStateDuration()
    {
        switch (currentState)
        {
            case AIDriverState.SpinningOut: return spinOutDuration;
            case AIDriverState.Recovering: return recoveryDuration;
            case AIDriverState.Boosting: return boostDuration;
            default: return 0f;
        }
    }
    
    #endregion
}
