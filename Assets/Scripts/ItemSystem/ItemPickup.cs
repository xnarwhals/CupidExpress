using Unity.VisualScripting;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private ItemSpawner spawner;
    private Transform spawnPoint;

    [Header("Visuals")]
    public float rotationSpeed = 50f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;

    private Vector3 startPosition;

    public void Initialize(ItemSpawner itemSpawner, Transform spawnLocation)
    {
        spawner = itemSpawner;
        spawnPoint = spawnLocation;
        startPosition = transform.position;
    }

    private void Update()
    {
        // RotatePickup();
        // BobPickup();

        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        Cart cart = other.gameObject.GetComponent<Cart>();
        
        if (cart != null)
        {
            CollectItem(cart);
            // if (!ItemManager.Instance.CartHasMaxItems(cart))
        }
    }

    private void CollectItem(Cart cart)
    {
        KartItem randomItem = spawner.GenerateRandomItem();
        // Debug.Log($"Collected item: {randomItem.itemName}");
        ItemManager.Instance.AssignItemToCart(cart, randomItem); // cart has item

        spawner.OnItemCollected(spawnPoint); // notify spawner that item was collected
        // effects here?
        Destroy(gameObject); // destroy pickup
    }
}
