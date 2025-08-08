using UnityEngine;

public enum AIDriverState
{
    Normal,         // Following spline normally
    CornerSlowing,  // Slowing down for upcoming corner
    SpinningOut,    // Currently spinning out from item hit
    Recovering,     // Reorienting after spinout
    Boosting,       // Under boost effect
    Stunned         // Temporarily unable to move (future use)
}

// State transition data
[System.Serializable]
public class AIStateTransition
{
    public AIDriverState fromState;
    public AIDriverState toState;
    public float duration;
    public bool allowInterruption;
    
    public AIStateTransition(AIDriverState from, AIDriverState to, float duration, bool canInterrupt = true)
    {
        this.fromState = from;
        this.toState = to;
        this.duration = duration;
        this.allowInterruption = canInterrupt;
    }
}
