using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class PlayerSplineProgress : MonoBehaviour
{
    public SplineContainer spline; // Assign in inspector
    public float splineProgress;   // 0 to 1 progress value

    void Start()
    {
        spline = GameManager.Instance.raceTrack;
    }

    void Update()
    {
        // Use the player's position (not a Ray)
        Vector3 pos = transform.position;

        // Find nearest point along spline
        float t;
        float3 nearest;
        float dist;
        // pos is a Vector3; cast to float3
        SplineUtility.GetNearestPoint(spline.Spline, (Unity.Mathematics.float3)pos, out nearest, out t);

        splineProgress = t; // Normalized spline progress (0–1)

        // Debug.Log($"Progress: {splineProgress:F3}  Distance: {distanceAlongSpline:F2}m");
    }

    private void OnDrawGizmos()
    {
        if (spline == null) return;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
        transform.position + Vector3.up * 2.5f,
        $"Spline Progress: {splineProgress:P2}\n"
    );
#endif
    }
}