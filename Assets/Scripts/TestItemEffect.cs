using UnityEngine;

public class TestItemEffect : MonoBehaviour
{
    [Header("Test Items")]
    public GameObject tomato;
    public Tomato tomatoScriptableObject;

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.T))
        {
            SpawnTomato();
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
