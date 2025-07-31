using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Watermelon")]
public class Watermelon : KartItem
{
    [Header("Watermelon Specific")]
    public GameObject rollingMelonPrefab;
    public GameObject aoeVisualPrefab;
    public float AOERadius = 5f; // Area of Effect radius
    public float melonTrackSpeed = 15f;

    public override void Use(Cart cartUsingItem, bool throwBackward)
    {
        if (cartUsingItem != null)
        {
            // 1. Find first place cart
            Cart leader = GameManager.Instance.GetLeaderCart();
            Debug.Log("Using Watermelon on leader: " + (leader != null ? leader.CartName : "None"));
            if (leader == null) return;

            // 2. spawn watermelon in front of user and have it snap to the spline and spline animate through track until close to leader
            Vector3 spawnPosition = cartUsingItem.itemSlot.position + cartUsingItem.transform.forward * 2f;
            GameObject rollingMelonObject = Instantiate(rollingMelonPrefab, spawnPosition, Quaternion.identity);
            RollingMelon rollingMelon = rollingMelonObject.GetComponent<RollingMelon>();

            if (rollingMelon != null)
            {
                rollingMelon.Initialize(AOERadius, melonTrackSpeed, leader, cartUsingItem, aoeVisualPrefab);
            }
            else
            {
                Debug.LogError("RollingMelon component not found on the prefab.");
            }
        }
    }

    
}
