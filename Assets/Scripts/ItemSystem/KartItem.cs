using UnityEngine;

public abstract class KartItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject visualPrefab;

    public abstract void Use(Cart cartUsingItem);

    public virtual bool ShouldAIUse(Cart cartUsingItem) => true;
}


