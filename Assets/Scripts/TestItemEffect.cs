using UnityEngine;

public class TestItemEffect : MonoBehaviour
{
    [Header("Test Items")]
    private Cart cartUsingItem;
    public Cart cartTarget;

    public GameObject tomato;
    public Tomato tomatoScriptableObject;
    public BoostItem boostItemScriptableObject;
    public Watermelon watermelonScriptableObject;

    private void Awake()
    {
        cartUsingItem = GetComponent<Cart>();
    }

    private void Update()
    {

        if (cartUsingItem != null && Input.GetKeyDown(KeyCode.T))
        {
            // SpawnTomato();
            // testCart.ApplyBoost(1000f);
            watermelonScriptableObject.Use(cartUsingItem, false);

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
