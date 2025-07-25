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

    [Header("Corner Stuff")]
    public SplineCornerDetector cornerDetector;
    [Range(0.05f, 0.1f)]
    public float cornerLookAhead = 0.05f; 

    [Header("Corner Settings")]
    [Range(1f, 10f)]
    public float distanceBeforeSlowDown = 1f;
    [Range(0.3f, 0.8f)]
    public float slowDownFactor = 0.3f;

    [Header("State Settings")]
    [Range(0.1f, 1f)]
    public float spinOutBrakeIntensity = 0.8f;
    [Range(0.1f, 1f)]
    public float recoverySpeedMultiplier = 0.3f;
    [Range(2f, 10f)]
    public float recoveryRotationSpeed = 3f;

    [Header("Spin Out Settings")]
    [Range(1f, 5f)]
    public float spinOutRotationSpeed = 1f; // rotations per second

    // State tracking
    private float stateTimer = 0f;
    private float originalMaxSpeed;

    // Components
    private AIDriver aiDriver;
    private Rigidbody rb;

    // Events
    public System.Action<AIDriverState, AIDriverState> OnStateChanged;

    public void Initialize(AIDriver driver)
    {
        aiDriver = driver;
        rb = aiDriver.GetComponent<Rigidbody>();
        cornerDetector = aiDriver.cornerDetector; // Get it from AIDriver, not this component
    }

    private void Update()
    {
        if (aiDriver == null) return;

        UpdateCurrentState();
    }

    private void UpdateCurrentState()
    {
        stateTimer += Time.deltaTime;

        if (currentState == AIDriverState.Normal)
        {
            CheckForCorners();
        }

        switch (currentState)
        {
            case AIDriverState.Normal:
                // Normal state - no special handling needed
                break;

            case AIDriverState.CornerSlowing:
                // HandleCornerSlowingState();
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

            case AIDriverState.CornerSlowing:
                return newState != AIDriverState.Recovering;

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

    // Note: State effects (movement, physics) are now handled in AIDriver.ApplyStateEffects()
    // These handlers only manage state transitions and timers

    private void HandleSpinOutState()
    {
        if (stateTimer >= spinOutDuration)
        {
            TryChangeState(AIDriverState.Recovering);
        }
    }

    private void HandleRecoveryState()
    {
        if (stateTimer >= recoveryDuration)
        {
            TryChangeState(AIDriverState.Normal);
        }
    }

    private void HandleBoostState()
    {
        if (stateTimer >= boostDuration)
        {
            TryChangeState(AIDriverState.Normal);
        }
    }

    private void HandleStunnedState()
    {
        // Auto-recover after duration
        if (stateTimer >= 1f) // Default 1 second stun
        {
            TryChangeState(AIDriverState.Normal);
        }
    }

    #endregion

    #region Corner Detection

    private void CheckForCorners()
    {
        if (cornerDetector == null) return;

        float curProgress = aiDriver.GetCurrentSplineProgress();
        bool cornerAhead = cornerDetector.IsCornerAhead(curProgress, cornerLookAhead);
        
        if (cornerAhead && currentState == AIDriverState.Normal)
        {
            TryChangeState(AIDriverState.CornerSlowing);
        }
        else if (!cornerAhead && currentState == AIDriverState.CornerSlowing)
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
                // Debug.Log("Enterd Spin Out State");
                break;

            case AIDriverState.Recovering:
                // Debug.Log("Enterd Recovery State");
                break;

            case AIDriverState.Normal:
                break;
        }
    }

    private void ExitCurrentState()
    {
        // Cleanup when leaving a state
        switch (currentState)
        {
            case AIDriverState.Recovering:
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
        TryChangeState(AIDriverState.Boosting, duration);
    }

    public void StartStun(float duration)
    {
        TryChangeState(AIDriverState.Stunned, duration);
    }

    public bool IsInState(AIDriverState state) => currentState == state;
    public bool CanMove() => currentState != AIDriverState.Stunned && currentState != AIDriverState.SpinningOut; 
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

    private void OnDrawGizmos()
    {
        if (aiDriver == null || cornerDetector == null) return;

        float currentProgress = aiDriver.GetCurrentSplineProgress();
        bool cornerAhead = cornerDetector.IsCornerAhead(currentProgress, cornerLookAhead);
        
        // State indicator sphere
        Vector3 spherePos = transform.position + Vector3.up * 2f;
        switch (currentState)
        {
            case AIDriverState.Normal:
                Gizmos.color = Color.green;
                break;
            case AIDriverState.CornerSlowing:
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                break;
            case AIDriverState.SpinningOut:
                Gizmos.color = Color.red;
                break;
            case AIDriverState.Recovering:
                Gizmos.color = Color.yellow;
                break;
            case AIDriverState.Boosting:
                Gizmos.color = Color.cyan;
                break;
            case AIDriverState.Stunned:
                Gizmos.color = Color.magenta;
                break;
        }
        Gizmos.DrawWireSphere(spherePos, 1.5f);
        
        // Corner detection visualization
        if (currentState == AIDriverState.Normal || currentState == AIDriverState.CornerSlowing)
        {
            // Gizmos.color = cornerAhead ? Color.red : Color.green;
            // Gizmos.DrawWireSphere(transform.position, 2f);
            
            // Look-ahead visualization
            if (aiDriver.spline != null)
            {
                float lookAheadProgress = currentProgress + cornerLookAhead;
                if (lookAheadProgress > 1f && aiDriver.spline.Spline.Closed)
                    lookAheadProgress -= 1f;
                else if (lookAheadProgress > 1f)
                    lookAheadProgress = 1f;

                Vector3 lookAheadPos = aiDriver.spline.EvaluatePosition(lookAheadProgress);
                
                // Gizmos.color = cornerAhead ? Color.red : Color.cyan;
                // Gizmos.DrawWireSphere(lookAheadPos, 1f);
                
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, lookAheadPos);
            }
        }

#if UNITY_EDITOR
        // State information label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 6f,
            $"State: {currentState}\n" +
            $"Timer: {stateTimer:F1}s\n" +
            $"Corner Ahead: {cornerAhead}");
#endif
    }
}
