using UnityEngine;

[System.Serializable]
public class SplineCornerData
{
    public bool isCorner = false;
    public float cornerIntensity = 1f; // 0.1 = gentle, 1.0 = sharp
    public float recommendedSpeed = 0.6f; // Speed multiplier for this corner
    
    [Header("Corner Visualization")]
    public Color gizmoColor = Color.red;
    public float gizmoSize = 1f;
}
