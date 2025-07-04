using UnityEngine;

[CreateAssetMenu(fileName = "New AI Personality", menuName = "AI/Personality")]
public class AIPersonality : ScriptableObject
{
    [Header("Basic Behavior")]
    [Tooltip("higher impacts maxSpeed, acceleration, turnSpeed, cornering behavior")]
    [Range(0f, 1f)]
    public float aggressiveness = 0.5f;

    [Header("Driving Style")]
    [Tooltip("0 = Center, 1 = Left, 2 = Right")]
    public DrivingLane drivingLane = DrivingLane.Center; 

    [Tooltip("Offset from center path in meters")]
    [Range(0f, 8f)]
    public float laneOffset = 2f;

    [Tooltip("How strictly the AI follows the lane (0 = flex, 1 = strict)")]
    [Range(0f, 1f)]
    public float laneCommitment = 0.7f;

    [Header("Proximity Behavior")]
    [Range(1f, 10f)]
    public float proximityDetectionRange = 5f; // 5 meters
    [Range(0f, 1f)]
    public float proximityAggression = 0.5f; // Behavior change when near other carts

}

public enum DrivingLane
{
    Center,
    Left,
    Right,
}
