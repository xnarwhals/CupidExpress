using UnityEngine;

[CreateAssetMenu(fileName = "New AI Personality", menuName = "AI/Personality")]
public class AIPersonality : ScriptableObject
{
    [Header("Basic Behavior")]
    [Tooltip("higher impacts maxSpeed, acceleration, turnSpeed, cornering behavior")]
    [Range(0f, 1f)]
    public float aggressiveness = 0.5f; // 0 = passive, 1 = beefer mode

    [Header("Driving Style")]
    public DrivingLane drivingLane = DrivingLane.Center;

    [Tooltip("Offset from center path in meters")]
    [Range(0f, 8f)]
    public float laneOffset = 2f;

    [Tooltip("How strictly the AI follows the lane (0 = flex, 1 = strict)")]
    [Range(0f, 1f)]
    public float laneCommitment = 0.7f;

    [Header("Item Behavior")]
    [Range(0f, 1f)]
    [Tooltip("Chance to use an item when available 0% vs 100%")]
    public float chanceToUseItem = 0.5f;
    public float proximityRadius = 5f; // what counts as another cart "close enough" to do an item check roll
}

public enum DrivingLane
{
    Center,
    Left,
    Right,
}
