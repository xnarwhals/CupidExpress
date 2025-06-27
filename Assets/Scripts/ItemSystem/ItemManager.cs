using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    private Dictionary<Cart, Queue<KartItem>> heldItems = new Dictionary<Cart, Queue<KartItem>>();
    public System.Action<Cart> OnItemAdded;
    public System.Action<Cart> OnItemUsed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AssignItemToCart(Cart cart, KartItem item)
    {
        if (!heldItems.ContainsKey(cart)) heldItems[cart] = new Queue<KartItem>();
        var itemQueue = heldItems[cart];

        if (itemQueue.Count < 2)
        {
            // UI animation or sound for pickup here?
            itemQueue.Enqueue(item); // add item 
            OnItemAdded?.Invoke(cart); // notify listeners that item was added
            // Debug.Log($"{cart.name} received item: {item.itemName}");
        }
        else
        {
            // Debug.Log("Max Items reached");
        }

    }

    public void UseItem(Cart cart)
    {
        if (heldItems.ContainsKey(cart) && heldItems[cart].Count > 0)
        {
            var item = heldItems[cart].Dequeue();
            item.Use(cart);
            OnItemUsed?.Invoke(cart); // notify listeners that item was used
        }
    }

    public bool CartHasMaxItems(Cart cart)
    {
        if (heldItems.ContainsKey(cart))
        {
            return heldItems[cart].Count >= 2; // Assuming max items is 2
        }
        return false;
    }

    public int GetItemCount(Cart cart)
    {
        if (heldItems.ContainsKey(cart))
        {
            return heldItems[cart].Count;
        }
        return 0;
    }

    public List<KartItem> GetCartItems(Cart cart)
    {
        if (heldItems.ContainsKey(cart))
        {
            return heldItems[cart].ToList(); 
        }
        return new List<KartItem>();
    }


}
