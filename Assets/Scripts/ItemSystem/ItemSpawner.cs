using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Item Spawner Settings")]
    [Tooltip("Time between item spawns")]
    public float spawnInterval = 5f;

    [Tooltip("Possible spawn items")]
    public List<KartItem> availableItems = new List<KartItem>();

    [Tooltip("Visual prefab for item pickuo")]
    public GameObject itemPickupPrefab;

    [Tooltip("Item rarity weights")]
    public List<float> itemRarityWeights = new List<float>();

    [Header("Spawn Points")]
    [Tooltip("Where can items spawn")]
    public Transform spawnPointParent;
    [HideInInspector]
    public Transform[] spawnPoints;

    [Header("Current State")]
    [SerializeField] private Dictionary<Transform, GameObject> activeItems = new Dictionary<Transform, GameObject>();
    private float nextSpawnTime = 0f;

    private void Start()
    {
        if (spawnPointParent == null)
        {
            Debug.LogError("No spawn points parent given for ItemSpawner.");
            return;
        }

        spawnPoints = new Transform[spawnPointParent.childCount];
        for (int i = 0; i < spawnPointParent.childCount; i++)
        {
            spawnPoints[i] = spawnPointParent.GetChild(i);
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points found in ItemSpawner.");
            return;
        }



        TrySpawnAtEmptyPoints(); // initial spawn
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            TrySpawnAtEmptyPoints();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void TrySpawnAtEmptyPoints()
    {
        if (availableItems.Count == 0)
        {
            Debug.Log("Add items to the ItemSpawner!");
            return;
        }

        List<Transform> emptySpawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!activeItems.ContainsKey(spawnPoint) || activeItems[spawnPoint] == null)
            {
                emptySpawnPoints.Add(spawnPoint);
            }
        }

        foreach (Transform point in emptySpawnPoints)
        {
            SpawnItemAt(point);
        }

    }

    private void SpawnItemAt(Transform spawnPoint)
    {

        GameObject pickup = Instantiate(itemPickupPrefab, spawnPoint.position, spawnPoint.rotation);

        // track it
        activeItems[spawnPoint] = pickup;

        // set up pickup
        var itemPickup = pickup.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.Initialize(this, spawnPoint);
        }
        // Debug.Log($"Spawned item: {selectedItem.itemName} at {spawnPoint.name}");

    }

    public KartItem GenerateRandomItem()
    {
        return SelectItemByRarity();
    }

    public void OnItemCollected(Transform spawnPoint)
    {
        if (activeItems.ContainsKey(spawnPoint))
        {
            activeItems.Remove(spawnPoint);
        }
    }

    private KartItem SelectItemByRarity()
    {
        // no weights = same chance for all items
        if (itemRarityWeights.Count != availableItems.Count)
        {
            Debug.LogWarning("item weights != available items, using equal chance for all items.");
            return availableItems[Random.Range(0, availableItems.Count)];
        }

        float totalWeight = 0f;
        for (int i = 0; i < itemRarityWeights.Count; i++)
        {
            totalWeight += itemRarityWeights[i];
        }

        float randomValue = Random.Range(0f, totalWeight);
        float curWeight = 0f;

        for (int i = 0; i < itemRarityWeights.Count; i++)
        {
            curWeight += itemRarityWeights[i];
            if (randomValue <= curWeight)
            {
                return availableItems[i];
            }
        }
        return availableItems[0]; // fallback 
    }
}
