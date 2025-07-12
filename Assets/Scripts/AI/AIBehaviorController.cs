using System.Collections.Generic;
using UnityEngine;

public class AIBehaviorController : MonoBehaviour
{
    [SerializeField] private AIPersonality personality; // scriptable object

    [Header("Corner Settings")]
    [Range(5f, 30f)]
    public float distanceBeforeSlowDown = 10f; // 10 meters 
    [Range(0.3f, 0.8f)]
    public float slowDownFactor = 0.6f; // 60% slower when approaching corners 

    [Header("Proximity Check Frequency")]
    [Tooltip("How often to check for nearby carts, rolls useItemCheck() if cart found (in seconds)")]
    [Range(1f, 20f)]
    public float proximityCheckFrequency = 6f; 

    private AIDriver aiDriver;
    private int lastWayPointIndex = -1;
    private float lastProximityCheckTime = 0f;

    private bool cartInProximity = false;
    private Cart curCartInProximity = null;

    private void Awake()
    {
        aiDriver = GetComponent<AIDriver>();
    }

    private void Start()
    {
        ApplyPersonalityValues();
    }

    private void ApplyPersonalityValues()
    {
        if (personality == null) return;

        aiDriver.maxSpeed += personality.aggressiveness * 5f; // 5 m/s increase based on aggressiveness
        aiDriver.acceleration *= Mathf.Lerp(0.6f, 1.3f, personality.aggressiveness); // 60% to 130% of base acceleration
        aiDriver.turnSpeed *= Mathf.Lerp(0.8f, 1.5f, personality.aggressiveness); // 80% to 120% of base turn speed
    }

    #region Driving

    private void UpdateTargetWithOffset()
    {
        Vector3 baseTarget = aiDriver.GetCurrentWaypointPosition();
        Vector3 offsetTarget = CalculateOffsetPosition(baseTarget);
        aiDriver.SetTarget(offsetTarget);

    }

    private Vector3 CalculateOffsetPosition(Vector3 baseWaypoint)
    {
        if (personality == null) return baseWaypoint;

        Vector3 trackDirection = GetTrackDirection();
        Vector3 LRDirection = Vector3.Cross(trackDirection, Vector3.up).normalized;

        Vector3 offset;

        switch (personality.drivingLane)
        {
            case DrivingLane.Left:
                offset = -LRDirection * personality.laneOffset;
                break;
            case DrivingLane.Right:
                offset = LRDirection * personality.laneOffset;
                break;
            case DrivingLane.Center:
            default:
                offset = Vector3.zero;
                break;
        }

        return baseWaypoint + offset * personality.laneCommitment;
    }

    private Vector3 GetTrackDirection()
    {
        if (aiDriver.waypoints.Length <= 1) return transform.forward;

        Vector3 currentPos = aiDriver.GetCurrentWaypointPosition();
        Vector3 nextPos = aiDriver.GetNextWaypointPosition();
        return (nextPos - currentPos).normalized;
    }

    private void SlowDownOnCorner()
    {
        Vector3 curTarget = aiDriver.CurTarget;

        if (aiDriver.NextWaypointIsCorner() == false) return;

        float distanceToCorner = Vector3.Distance(transform.position, curTarget);

        float speedModifier = CalculateSpeedModifer(distanceToCorner); // without personality range (0.6 - 1)
        speedModifier = ApplyPersonalityToCornerSpeed(speedModifier); // with personality :D range (0.3 - 1.2)

        aiDriver.SetSpeedModifier(speedModifier);
    }

    private float CalculateSpeedModifer(float distanceToCorner)
    {
        if (distanceToCorner > distanceBeforeSlowDown)
            return 1f; // No slowdown when far enough

        // interpolate full speed to slow down
        float distanceRatio = distanceToCorner / distanceBeforeSlowDown;
        return Mathf.Lerp(slowDownFactor, 1f, distanceRatio); 
    }

    private float ApplyPersonalityToCornerSpeed(float baseSpeedModifier)
    {
        if (personality == null) return baseSpeedModifier; // (0.6 - 1)

        float agroBonous = personality.aggressiveness * 0.22f; // agro levels .9+ hit the clamp
        return Mathf.Clamp(baseSpeedModifier + agroBonous, 0.3f, 1.2f); // 30% to 120% of base speed modifier
    }

    #endregion

    #region Item Usage

    // Based on personality, useItem check is done in two cases
    // 1. When close to other carts (proximity detection)
    // 2. When reached a waypoint

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


    private void Update()
    {
        if (personality == null || aiDriver == null) return;

        if (aiDriver.CurrentWaypointIndex != lastWayPointIndex)
        {
            UpdateTargetWithOffset();
            lastWayPointIndex = aiDriver.CurrentWaypointIndex;
            UseItemCheck();
        }

        if (Time.time - lastProximityCheckTime > proximityCheckFrequency) // Check every x seconds
        {
            CheckProximityState(); // note it does not care if there is more than one cart in proximity 
            lastProximityCheckTime = Time.time;
        }


        SlowDownOnCorner();
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
        if (aiDriver == null) return;

        Vector3 curTarget = aiDriver.CurTarget;
        float distanceToCorner = Vector3.Distance(transform.position, curTarget);
        Vector3 directionToTarget = (curTarget - transform.position).normalized;

        Gizmos.color = distanceToCorner <= distanceBeforeSlowDown ? Color.red : Color.green;
        Gizmos.DrawRay(transform.position, directionToTarget * distanceToCorner);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, directionToTarget * distanceBeforeSlowDown); // slowdown threshold

        // Slowdown threshold sphere
        Gizmos.color = Color.blue;
        Vector3 slowdownPoint = transform.position + (directionToTarget * distanceBeforeSlowDown);
        Gizmos.DrawWireSphere(slowdownPoint, 0.8f);

        // Speed modifier visualization
        float speedMod = CalculateSpeedModifer(distanceToCorner);
        Gizmos.color = Color.Lerp(Color.red, Color.green, speedMod);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * speedMod);

        // proximity check visualization
        Cart nearbyCart = FindClosestCart(out float proximityDistance);
        Gizmos.color = nearbyCart != null ? Color.magenta : Color.gray; // Magenta if nearby cart exists
        Gizmos.DrawWireSphere(transform.position, personality.proximityRadius);


#if UNITY_EDITOR
        // Draw text labels in editor
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
        $"Distance: {distanceToCorner:F1}m\nSpeed: {speedMod:F2}");

        UnityEditor.Handles.Label(slowdownPoint + Vector3.up,
            $"Slowdown Threshold\n{distanceBeforeSlowDown:F1}m");
#endif
    }


}
