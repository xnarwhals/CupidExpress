using System.Collections.Generic;
using UnityEngine;

public class AIBehaviorController : MonoBehaviour
{
    [SerializeField] private AIPersonality personality; // scriptable object

    [Header("Proximity")]
    [Range(0.5f, 5f)]
    public float proximityCheckFrequency = 1f; // Distance to check for nearby carts
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

        // ApplyPersonalityValues();
    }

    // Change in needed will need testing for tunning
    private void ApplyPersonalityValues()
    {
        if (personality == null) return;

        // Apply personality modifiers to base values
        aiDriver.maxSpeed = baseMaxSpeed + (personality.aggressiveness * 5f);
        aiDriver.acceleration = baseAcceleration * Mathf.Lerp(0.6f, 1.3f, personality.aggressiveness);
        aiDriver.turnSpeed = baseTurnSpeed * Mathf.Lerp(0.8f, 1.1f, personality.aggressiveness);

        // Debug.Log($"AI Personality Applied: Speed={aiDriver.maxSpeed:F1}, Accel={aiDriver.acceleration:F1}, Turn={aiDriver.turnSpeed:F1}");
    }

    private void Update()
    {
        if (personality == null || aiDriver == null) return;

        ApplyLaneOffset();

        if (Time.time - lastProximityCheckTime >= proximityCheckFrequency) // not every frame
        {
            CheckProximityState();
            lastProximityCheckTime = Time.time;
        }
    }

    private void ApplyLaneOffset()
    {
        if (personality == null || aiDriver == null) return;

        switch (personality.drivingLane)
        {
            case DrivingLane.Center:
                break;
            case DrivingLane.Left:
                aiDriver.SetLaneOffset(-personality.laneOffset);
                break;
            case DrivingLane.Right:
                aiDriver.SetLaneOffset(personality.laneOffset);
                break;
            default:
                break;
        } 
        

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
        if (aiDriver == null || personality == null) return;
        // proximity sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, personality.proximityRadius);
    }
}