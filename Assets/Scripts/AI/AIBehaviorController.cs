using System.Collections.Generic;
using UnityEngine;

public class AIBehaviorController : MonoBehaviour
{
    [SerializeField] private AIPersonality personality; // scriptable object

    [Header("Corner Detection")]
    public SplineCornerDetector cornerDetector;
    [Range(0.05f, 0.2f)]
    public float cornerLookAhead = 0.1f; // How far ahead to check for corners

    [Header("Corner Settings")]
    [Range(5f, 30f)]
    public float distanceBeforeSlowDown = 10f;
    [Range(0.3f, 0.8f)]
    public float slowDownFactor = 0.6f;

    [Header("Proximity")]
    [Range(0.5f, 5f)]
    public float proximityCheckFrequency = 1f; // Distance to check for nearby carts

    [Header("Speed Lerping")]
    [Range(1f, 10f)]
    public float speedLerpTime = 4f; 
    private float targetMaxSpeed;
    private bool isSlowingDown = false;

    private AIDriver aiDriver;

    // before personality modifications
    private float baseMaxSpeed;
    private float baseAcceleration;
    private float baseTurnSpeed;

    private bool cartInProximity = false;
    private Cart curCartInProximity = null; 
    private float lastProximityCheckTime = 0f; 

    private void Awake()
    {
        aiDriver = GetComponent<AIDriver>();
    }

    private void Start()
    {
        // Store original values before personality modifications
        baseMaxSpeed = aiDriver.maxSpeed;
        baseAcceleration = aiDriver.acceleration;
        baseTurnSpeed = aiDriver.turnSpeed;

        ApplyPersonalityValues();
        targetMaxSpeed = aiDriver.maxSpeed; 
    }

    // Change in needed will need testing for tunning
    private void ApplyPersonalityValues()
    {
        if (personality == null) return;

        // Apply personality modifiers to base values
        aiDriver.maxSpeed = baseMaxSpeed + (personality.aggressiveness * 5f);
        aiDriver.acceleration = baseAcceleration * Mathf.Lerp(0.6f, 1.3f, personality.aggressiveness);
        aiDriver.turnSpeed = baseTurnSpeed * Mathf.Lerp(0.8f, 1.5f, personality.aggressiveness);

        // Debug.Log($"AI Personality Applied: Speed={aiDriver.maxSpeed:F1}, Accel={aiDriver.acceleration:F1}, Turn={aiDriver.turnSpeed:F1}");
    }

    private void Update()
    {
        if (personality == null || aiDriver == null) return;

        // Apply lane offset based on personality
        ApplyLaneOffset();
        SlowDownOnCorner();
        InterpolateSpeed();

        if (Time.time - lastProximityCheckTime >= proximityCheckFrequency) // not every frame
        {
            CheckProximityState();
            lastProximityCheckTime = Time.time;
        }
    }

    private void ApplyLaneOffset()
    {
        if (personality == null) return;
        
        // Get current spline progress
        float currentProgress = aiDriver.GetCurrentSplineProgress();
        
        // Calculate lane offset vector
        Vector3 offsetVector = CalculateLaneOffsetVector(currentProgress);
        
        // Apply offset to AIDriver
        aiDriver.SetTargetOffset(offsetVector);
    }

    private void SlowDownOnCorner()
    {
        if (cornerDetector == null) return;

        float curProgress = aiDriver.GetCurrentSplineProgress();
        bool cornerAhead = cornerDetector.IsCornerAhead(curProgress, cornerLookAhead);

        if (!cornerAhead)
        {
            targetMaxSpeed = baseMaxSpeed + (personality.aggressiveness * 5f); // normal speed
            isSlowingDown = false;
            return;
        }
        else
        {
            float cornerSpeedModifier = slowDownFactor;

            if (personality != null)
            {
                float agroBonus = personality.aggressiveness * 0.2f; // 20% bonus
                cornerSpeedModifier = Mathf.Clamp(slowDownFactor + agroBonus, 0.3f, 1f);
            }

            float cornerSpeed = baseMaxSpeed * cornerSpeedModifier;
            targetMaxSpeed = cornerSpeed + (personality.aggressiveness * 2f);
            isSlowingDown = true;
        }
    }

    private void InterpolateSpeed()
    {
        aiDriver.maxSpeed = Mathf.Lerp(aiDriver.maxSpeed, targetMaxSpeed, speedLerpTime * Time.deltaTime);
        if (Mathf.Abs(aiDriver.maxSpeed - targetMaxSpeed) < 0.1f)
        {
            aiDriver.maxSpeed = targetMaxSpeed; // Snap to target if close enough

        }
    }

    private Vector3 CalculateLaneOffsetVector(float progress)
    {
        // No offset for center lane or zero offset
        if (personality.drivingLane == DrivingLane.Center || personality.laneOffset <= 0f)
            return Vector3.zero;

        // Calculate track direction at this progress point
        Vector3 trackDirection = GetSplineDirection(progress);

        // Get perpendicular direction (right side of track)
        Vector3 rightDirection = Vector3.Cross(trackDirection, Vector3.up).normalized;

        // Calculate offset vector based on lane preference
        Vector3 offsetVector = Vector3.zero;
        switch (personality.drivingLane)
        {
            case DrivingLane.Left:
                offsetVector = -rightDirection * personality.laneOffset; // Negative = left
                break;
            case DrivingLane.Right:
                offsetVector = rightDirection * personality.laneOffset; // Positive = right
                break;
        }

        // Apply lane commitment (how strictly to follow the offset)
        offsetVector *= personality.laneCommitment;

        return offsetVector;
    }

    private Vector3 GetSplineDirection(float progress)
    {
        // Sample two points close together to calculate direction
        float sampleDistance = 0.01f; // 1% of spline
        float nextProgress = Mathf.Min(1f, progress + sampleDistance);
        
        Vector3 currentPos = aiDriver.spline.EvaluatePosition(progress);
        Vector3 nextPos = aiDriver.spline.EvaluatePosition(nextProgress);
        
        return (nextPos - currentPos).normalized;
    }

    #region Item Usage

    // Based on personality, useItem check is done when close to other carts (proximity detection)

    public void UseItemCheck()
    {
        if (personality == null || aiDriver == null) return;

        bool hasNearbyCart = false;
        Cart nearbyCart = FindClosestCart(out float distance);

        if (nearbyCart != null) hasNearbyCart = true;

        float finalChance = ApplyAgroToBase(hasNearbyCart);
        bool shouldUseItem = Random.value < finalChance;

        if (shouldUseItem)
        {
            bool throwItBack = ThrowItBack(nearbyCart);
            ItemManager.Instance.UseItem(aiDriver.ThisCart, throwItBack);
        }
    }

    private Cart FindClosestCart(out float distance)
    {
        List<Cart> leaderboard = GameManager.Instance.GetCartLeaderboard();
        Cart closestCart = null;
        float closestDistance = float.MaxValue;

        foreach (Cart cart in leaderboard)
        {
            if (cart == aiDriver.ThisCart) continue; // skip self
            float dist = Vector3.Distance(transform.position, cart.transform.position);

            if (dist <= personality.proximityRadius && dist < closestDistance)
            {
                closestDistance = dist;
                closestCart = cart;
            }
        }

        distance = closestDistance;
        return closestCart;
    }

    private float ApplyAgroToBase(bool hasNearbyCart)
    {
        float baseChance = personality.chanceToUseItem;
        if (hasNearbyCart)
        {
            float agroBonus = personality.aggressiveness * 0.4f; // 50% bonus
            return Mathf.Clamp(baseChance + agroBonus, 0f, 1f);
        }  

        return baseChance;
    }

    private bool ThrowItBack(Cart nearbyCart)
    {
        if (nearbyCart == null) return false;

        Vector3 toCart = (nearbyCart.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toCart);

        return dot < -0.3f; 
    }
    
    // item roll check once per proximity check
    // if cart is nearby useItem is rolled once, only roll again if the cart leaves and re-enters proximity
    private void CheckProximityState()
    {
        Cart nearbyCart = FindClosestCart(out float proximityDistance);
        // Debug.Log($"Proximity check: {nearbyCart?.CartName ?? "None"} at {proximityDistance:F1}m");

        if (nearbyCart != null)
        {
            if (!cartInProximity)
            {
                if (nearbyCart != curCartInProximity)
                {
                    cartInProximity = true;
                    curCartInProximity = nearbyCart;
                    UseItemCheck();
                }
            }
        }
        else
        {
            if (cartInProximity)
            {
                cartInProximity = false;
                curCartInProximity = null;
            }
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (aiDriver == null || cornerDetector == null) return;

        float currentProgress = aiDriver.GetCurrentSplineProgress();
        bool cornerAhead = cornerDetector.IsCornerAhead(currentProgress, cornerLookAhead);

        if (cornerDetector != null)
        {
            // Main corner detection sphere
            Gizmos.color = cornerAhead ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 3f);

            // Proximity Sphere
            Gizmos.color = cartInProximity ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(transform.position, personality.proximityRadius);

            // ✅ CURRENT SPEED BAR (Blue) - What speed AI is actually using
            float currentSpeedRatio = aiDriver.maxSpeed / baseMaxSpeed;
            Vector3 barStart = transform.position + Vector3.up * 5f;
            Vector3 currentBarEnd = barStart + Vector3.up * (currentSpeedRatio * 3f);

            // Color current speed bar based on level
            if (currentSpeedRatio >= 0.9f)
                Gizmos.color = Color.blue;
            else if (currentSpeedRatio >= 0.7f)
                Gizmos.color = Color.cyan;
            else
                Gizmos.color = Color.magenta;

            Gizmos.DrawLine(barStart, currentBarEnd);
            Gizmos.DrawWireSphere(currentBarEnd, 0.3f);

            // ✅ TARGET SPEED BAR (Yellow) - What speed AI is transitioning towards
            float targetSpeedRatio = targetMaxSpeed / baseMaxSpeed;
            Vector3 targetBarEnd = barStart + Vector3.up * (targetSpeedRatio * 3f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(barStart + Vector3.right * 0.5f, targetBarEnd + Vector3.right * 0.5f);
            Gizmos.DrawWireSphere(targetBarEnd + Vector3.right * 0.5f, 0.2f);

            // ✅ TRANSITION INDICATOR - Line between current and target
            if (Mathf.Abs(aiDriver.maxSpeed - targetMaxSpeed) > 0.1f)
            {
                Gizmos.color = isSlowingDown ? Color.red : Color.green;
                Gizmos.DrawLine(currentBarEnd, targetBarEnd + Vector3.right * 0.5f);

                // Arrow to show direction of transition
                Vector3 arrowDir = (targetBarEnd - currentBarEnd).normalized;
                Vector3 arrowPos = Vector3.Lerp(currentBarEnd, targetBarEnd, 0.5f);
                Gizmos.DrawRay(arrowPos, arrowDir * 0.5f);
            }

            // ✅ TRANSITION PROGRESS BAR
            float transitionProgress = 0f;
            if (Mathf.Abs(targetMaxSpeed - baseMaxSpeed) > 0.1f) // Avoid division by zero
            {
                float maxSpeedDiff = Mathf.Abs(targetMaxSpeed - baseMaxSpeed);
                float currentSpeedDiff = Mathf.Abs(aiDriver.maxSpeed - baseMaxSpeed);
                transitionProgress = 1f - (currentSpeedDiff / maxSpeedDiff);
            }

            Vector3 progressStart = transform.position + Vector3.up * 8f;
            Vector3 progressEnd = progressStart + Vector3.right * (transitionProgress * 2f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(progressStart, progressEnd);
            Gizmos.DrawWireCube(progressEnd, Vector3.one * 0.1f);

            // ✅ LOOK-AHEAD VISUALIZATION (existing code)
            Vector3 lookAheadPos = Vector3.zero;
            if (aiDriver.spline != null)
            {
                float lookAheadProgress = currentProgress + cornerLookAhead;
                if (lookAheadProgress > 1f && aiDriver.spline.Spline.Closed)
                    lookAheadProgress -= 1f;
                else if (lookAheadProgress > 1f)
                    lookAheadProgress = 1f;

                lookAheadPos = aiDriver.spline.EvaluatePosition(lookAheadProgress);

                // Draw look-ahead point
                Gizmos.color = cornerAhead ? Color.red : Color.cyan;
                Gizmos.DrawWireSphere(lookAheadPos, 1f);

                // Draw line from car to look-ahead point
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, lookAheadPos);
            }
        }

        // ✅ ENHANCED LANE INDICATOR WITH SPEED INFO
        if (personality != null)
        {
            Vector3 cubePos = transform.position + Vector3.up * 3f;
            Gizmos.color = personality.drivingLane == DrivingLane.Left ? Color.red :
                        personality.drivingLane == DrivingLane.Right ? Color.blue : Color.white;
            Gizmos.DrawWireCube(cubePos, Vector3.one * 0.5f);

#if UNITY_EDITOR
            string laneInfo = personality.drivingLane == DrivingLane.Center ? "Center" :
                            personality.drivingLane == DrivingLane.Left ? $"Left ({personality.laneOffset:F1}m)" :
                            $"Right ({personality.laneOffset:F1}m)";

            // ✅ Enhanced debug info with transition data
            string speedStatus = isSlowingDown ? "SLOWING" : "ACCELERATING";
            float speedDifference = targetMaxSpeed - aiDriver.maxSpeed;

            UnityEditor.Handles.Label(transform.position + Vector3.up * 9f,
                $"Personality: {personality.name}\n" +
                $"Aggressiveness: {personality.aggressiveness:F2}\n" +
                $"Lane: {laneInfo}\n" +
                $"Current Speed: {aiDriver.maxSpeed:F1}\n" +
                $"Target Speed: {targetMaxSpeed:F1}\n" +
                $"Speed Diff: {speedDifference:F1}\n" +
                $"Transition Rate: {speedLerpTime:F1}\n" +
                $"Status: {speedStatus}\n" +
                $"Corner Ahead: {cornerAhead}");
#endif
        }
    }
}
