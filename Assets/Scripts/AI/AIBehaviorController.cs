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

    private AIDriver aiDriver;
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
    }

    private void ApplyPersonalityValues()
    {
        if (personality == null) return;

        // Apply personality modifiers to base values
        aiDriver.maxSpeed = baseMaxSpeed + (personality.aggressiveness * 5f);
        aiDriver.acceleration = baseAcceleration * Mathf.Lerp(0.6f, 1.3f, personality.aggressiveness);
        aiDriver.turnSpeed = baseTurnSpeed * Mathf.Lerp(0.8f, 1.5f, personality.aggressiveness);
        
        Debug.Log($"AI Personality Applied: Speed={aiDriver.maxSpeed:F1}, Accel={aiDriver.acceleration:F1}, Turn={aiDriver.turnSpeed:F1}");
    }

    private void Update()
    {
        if (personality == null || aiDriver == null) return;

        // Apply lane offset based on personality
        ApplyLaneOffset();

        if (Time.time - lastProximityCheckTime >= proximityCheckFrequency)
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

    // private void SlowDownOnCorner()
    // {
    //     Vector3 curTarget = aiDriver.CurTarget;

    //     if (aiDriver.NextWaypointIsCorner() == false) return;

    //     float distanceToCorner = Vector3.Distance(transform.position, curTarget);

    //     float speedModifier = CalculateSpeedModifer(distanceToCorner); // without personality range (0.6 - 1)
    //     speedModifier = ApplyPersonalityToCornerSpeed(speedModifier); // with personality :D range (0.3 - 1.2)

    //     aiDriver.SetSpeedModifier(speedModifier);
    // }

    // private float CalculateSpeedModifer(float distanceToCorner)
    // {
    //     if (distanceToCorner > distanceBeforeSlowDown)
    //         return 1f; // No slowdown when far enough

    //     // interpolate full speed to slow down
    //     float distanceRatio = distanceToCorner / distanceBeforeSlowDown;
    //     return Mathf.Lerp(slowDownFactor, 1f, distanceRatio); 
    // }

    // private float ApplyPersonalityToCornerSpeed(float baseSpeedModifier)
    // {
    //     if (personality == null) return baseSpeedModifier; // (0.6 - 1)

    //     float agroBonous = personality.aggressiveness * 0.22f; // agro levels .9+ hit the clamp
    //     return Mathf.Clamp(baseSpeedModifier + agroBonous, 0.3f, 1.2f); // 30% to 120% of base speed modifier
    // }


    #region Item Usage

    // Based on personality, useItem check is done in two cases
    // 1. When close to other carts (proximity detection)

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


    #endregion

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
                if (!cartInProximity || nearbyCart != curCartInProximity)
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

    private void OnDrawGizmos()
    {
        if (aiDriver == null || cornerDetector == null) return;

        float currentProgress = aiDriver.GetCurrentSplineProgress();
        bool cornerAhead = cornerDetector.IsCornerAhead(currentProgress, cornerLookAhead);
        
        // Draw corner detection range
        Gizmos.color = cornerAhead ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 3f);
        
        // Draw personality info
        if (personality != null)
        {
            // Lane indicator cube above car
            Vector3 cubePos = transform.position + Vector3.up * 3f;
            Gizmos.color = personality.drivingLane == DrivingLane.Left ? Color.red : 
                          personality.drivingLane == DrivingLane.Right ? Color.blue : Color.white;
            Gizmos.DrawWireCube(cubePos, Vector3.one * 0.5f);
            
#if UNITY_EDITOR
            string laneInfo = personality.drivingLane == DrivingLane.Center ? "Center" :
                             personality.drivingLane == DrivingLane.Left ? $"Left ({personality.laneOffset:F1}m)" :
                             $"Right ({personality.laneOffset:F1}m)";
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 4f,
                $"Personality: {personality.name}\n" +
                $"Aggressiveness: {personality.aggressiveness:F2}\n" +
                $"Lane: {laneInfo}\n" +
                $"Commitment: {personality.laneCommitment:F2}\n" +
                $"Speed: {aiDriver.maxSpeed:F1} / {baseMaxSpeed:F1}\n" +
                $"Corner Ahead: {cornerAhead}");
#endif
        }
    }
}
