using UnityEngine;

public class TestItemEffect : MonoBehaviour
{
    [Header("Test Items")]
    private Cart testCart;
    public GameObject tomato;
    public Tomato tomatoScriptableObject;
    public BoostItem boostItemScriptableObject;

    private void Awake()
    {
        testCart = GetComponent<Cart>();  
    }

    private void Update()
    {

        if (testCart != null && Input.GetKeyDown(KeyCode.T))
        {
            // SpawnTomato();
            testCart.ApplyBoost(1000f);

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
