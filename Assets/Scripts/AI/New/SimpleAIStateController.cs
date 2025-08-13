using UnityEngine;

[System.Serializable]
public class SimpleAIStateController : MonoBehaviour
{
    [Header("State Management")]
    private float stateTimer = 0f;
    public AIDriverState currentState = AIDriverState.Normal;
    private float spinOutDuration = 2f;
    private float recoveryDuration = 1.5f;
    private float boostDuration = 3f;

    [Header("State Settings")]
    // Ref
    private SimpleAIDriver SimpleAIDriver;

    // Events
    public System.Action<AIDriverState, AIDriverState> OnStateChanged;

    public void Initialize(SimpleAIDriver driver)
    {
        SimpleAIDriver = driver;
    }

    private void Update()
    {
        if (SimpleAIDriver == null) return;

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

    private void HandleSpinOutState()
    {
        SimpleAIDriver.modelTransform.Rotate(Vector3.up * 450f * Time.deltaTime, Space.World);


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
                break;

            case AIDriverState.SpinningOut:
                break;
        }
    }

    #endregion

    #region Public Interface

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

//     private void OnDrawGizmos()
//     {
//         if (aiDriver == null) return;

//         float currentProgress = aiDriver.GetSplineProgress();
//         // bool cornerAhead = cornerDetector.IsCornerAhead(currentProgress, cornerLookAhead);

//         int leaderboardPos = -1;
//         if (GameManager.Instance != null && aiDriver.ThisCart != null)
//         {
//             leaderboardPos = GameManager.Instance.GetCartPosition(aiDriver.ThisCart);
//         }


// #if UNITY_EDITOR
//         // State information label
//         UnityEditor.Handles.Label(transform.position + Vector3.up * 6f,
//             $"State: {currentState}\n" +
//             $"Timer: {stateTimer:F1}s\n" +
//             $"Spline Progress: {aiDriver.GetSplineProgress():F2}\n" +
//             $"Leaderboard Pos: {(leaderboardPos > 0 ? leaderboardPos.ToString() : "N/A")}"
//         );
// #endif
//     }
}