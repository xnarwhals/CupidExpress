using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class SplineCornerDetector : MonoBehaviour
{
    [Header("Corner Detection")]
    public SplineContainer splineContainer;
    
    [Header("Auto-Detection Settings")]
    [Range(10f, 60f)]
    public float curvatureThreshold = 20f; // Degrees of curvature to consider a corner
    
    [Range(0.01f, 0.1f)]
    public float sampleDistance = 0.02f; // How far to sample for curvature calculation
    
    [Header("Visualization")]
    [Tooltip("Show curvature analysis for all points, even before auto-detection")]
    public bool showCurvatureAnalysis = true;
    
    [Header("Manual Corner Setup")]
    public List<SplineCornerData> cornerData = new List<SplineCornerData>();
    
    private void Start()
    {
        if (splineContainer != null && cornerData.Count == 0)
        {
            AutoDetectCorners();
        }
    }
    
    [ContextMenu("Auto-Detect Corners")]
    public void AutoDetectCorners()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;
        
        cornerData.Clear();
        var spline = splineContainer.Spline;
        int knotCount = spline.Count;
        
        for (int i = 0; i < knotCount; i++)
        {
            float t = (float)i / knotCount;
            float curvature = CalculateCurvatureAtProgress(t);
            
            SplineCornerData data = new SplineCornerData();
            
            if (curvature > curvatureThreshold)
            {
                data.isCorner = true;
                data.cornerIntensity = Mathf.Clamp01(curvature / 60f); // Normalize to 0-1
                data.recommendedSpeed = Mathf.Lerp(0.4f, 0.8f, 1f - data.cornerIntensity);
                data.gizmoColor = Color.Lerp(Color.yellow, Color.red, data.cornerIntensity);
            }
            
            cornerData.Add(data);
        }
        
        Debug.Log($"Auto-detected {cornerData.FindAll(c => c.isCorner).Count} corners out of {knotCount} knots");
    }
    
    public SplineCornerData GetCornerDataAtProgress(float progress)
    {
        if (cornerData.Count == 0) return null;
        
        int knotIndex = Mathf.FloorToInt(progress * cornerData.Count);
        knotIndex = Mathf.Clamp(knotIndex, 0, cornerData.Count - 1);
        
        return cornerData[knotIndex];
    }
    
    public bool IsCornerAhead(float currentProgress, float lookAheadDistance = 0.1f)
    {
        float endProgress = Mathf.Min(1f, currentProgress + lookAheadDistance);
        
        for (float t = currentProgress; t <= endProgress; t += 0.02f)
        {
            var data = GetCornerDataAtProgress(t);
            if (data != null && data.isCorner) return true;
        }
        
        return false;
    }
    
    private float CalculateCurvatureAtProgress(float progress)
    {
        if (splineContainer == null) return 0f;
        
        float prevT = Mathf.Max(0f, progress - sampleDistance);
        float nextT = Mathf.Min(1f, progress + sampleDistance);
        
        Vector3 currentPos = splineContainer.EvaluatePosition(progress);
        Vector3 prevPos = splineContainer.EvaluatePosition(prevT);
        Vector3 nextPos = splineContainer.EvaluatePosition(nextT);
        
        Vector3 forward = (nextPos - currentPos).normalized;
        Vector3 backward = (currentPos - prevPos).normalized;
        
        return Vector3.Angle(forward, backward);
    }
    
    private void OnDrawGizmos()
    {
        if (splineContainer == null) return;
        
        var spline = splineContainer.Spline;
        if (spline == null) return;
        
        // If no corner data, show curvature analysis
        if (cornerData.Count == 0 || showCurvatureAnalysis)
        {
            DrawCurvatureAnalysis();
            if (cornerData.Count == 0) return;
        }
        
        // Draw corner data
        for (int i = 0; i < cornerData.Count && i < spline.Count; i++)
        {
            var data = cornerData[i];
            float t = (float)i / spline.Count;
            Vector3 position = splineContainer.EvaluatePosition(t);
            
            if (data.isCorner)
            {
                // Corner indicators (colored spheres)
                Gizmos.color = data.gizmoColor;
                Gizmos.DrawWireSphere(position, data.gizmoSize);
                
                // Intensity line
                Gizmos.DrawLine(position, position + Vector3.up * (data.cornerIntensity * 3f));
                
#if UNITY_EDITOR
                // Corner info text
                UnityEditor.Handles.Label(position + Vector3.up * 2f, 
                    $"Corner {i}\nIntensity: {data.cornerIntensity:F2}\nSpeed: {data.recommendedSpeed:F2}");
#endif
            }
            else
            {
                // Non-corner indicators (small green dots)
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(position, 0.2f);
            }
        }
    }
    
    private void DrawCurvatureAnalysis()
    {
        var spline = splineContainer.Spline;
        int sampleCount = 50; // Number of points to sample along spline
        
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 position = splineContainer.EvaluatePosition(t);
            float curvature = CalculateCurvatureAtProgress(t);
            
            // Color based on curvature
            if (curvature > curvatureThreshold)
            {
                // Potential corner (red)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(position, 0.5f);
            }
            else if (curvature > curvatureThreshold * 0.5f)
            {
                // Moderate curve (yellow)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(position, 0.3f);
            }
            else
            {
                // Straight section (green)
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(position, 0.2f);
            }
            
#if UNITY_EDITOR
            // Show curvature value
            if (curvature > 5f) // Only show for curves
            {
                UnityEditor.Handles.Label(position + Vector3.up, $"{curvature:F1}°");
            }
#endif
        }
        
#if UNITY_EDITOR
        // Legend
        Vector3 legendPos = splineContainer.transform.position + Vector3.up * 5f;
        UnityEditor.Handles.Label(legendPos, 
            $"Curvature Analysis:\n" +
            $"🔴 Red: Corner (>{curvatureThreshold:F0}°)\n" +
            $"🟡 Yellow: Curve (>{curvatureThreshold * 0.5f:F0}°)\n" +
            $"🟢 Green: Straight (<{curvatureThreshold * 0.5f:F0}°)");
#endif
    }
}
