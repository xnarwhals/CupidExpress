using UnityEngine;
using UnityEngine.InputSystem;

public class TestItemEffect : MonoBehaviour
{
    [Header("Test Items")]
    public Cart cartUsingItem;
    public Cart cartTarget;

    public GameObject tomato;
    public Tomato tomatoScriptableObject;
    public BoostItem boostItemScriptableObject;
    public Watermelon watermelonScriptableObject;

    private void Awake()
    {

        cartUsingItem = GetComponent<Cart>();
    }

    public void Test()
    {

        if (cartUsingItem != null)
        {
            
            // cartUsingItem.SpinOut(4f);
            // SpawnTomato();
            // cartUsingItem.ApplyBoost(boostItemScriptableObject.boostDuration, boostItemScriptableObject.speedMultiplier);
            // watermelonScriptableObject.Use(cartUsingItem, false);

        }
    }

    private void SpawnTomato()
    {
        if (tomato == null || tomatoScriptableObject == null) return;

        GameObject tomatoInstance = Instantiate(tomato, transform.position, Quaternion.identity);
        TomatoProjectile tomatoProjectile = tomatoInstance.GetComponent<TomatoProjectile>();

        if (tomatoProjectile != null)
        {
            tomatoProjectile.Initialize(tomatoScriptableObject, null, transform.position);
        }
    }




}
