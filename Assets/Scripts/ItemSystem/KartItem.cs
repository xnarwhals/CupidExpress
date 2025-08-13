using UnityEngine;

public abstract class KartItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject visualPrefab;

    // [Range(1, 100)] // ex 2 [tomato, boost] --> [60%, 40%]
    // public int rarity;
    
    public abstract void Use(Cart cartUsingItem, bool throwBackward);
}


