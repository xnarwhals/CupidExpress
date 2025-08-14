using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    private Dictionary<Cart, Queue<KartItem>> heldItems = new Dictionary<Cart, Queue<KartItem>>();
    public event System.Action<Cart> OnItemPickup;
    public event System.Action<Cart> OnItemUse;
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
            itemQueue.Enqueue(item); // add item 
            UpdateItemVisuals(cart, cart.itemSlot);
            OnItemPickup?.Invoke(cart);

            AudioManager.Instance.PlayUISFX(AudioManager.Instance.itemPickupSound, 1.0f);
        }
        else
        {
            // Debug.Log("Max Items reached");
        }

    }

    public void UseItem(Cart cart, bool throwBackward)
    {
        if (heldItems.ContainsKey(cart) && heldItems[cart].Count > 0)
        {
            var item = heldItems[cart].Dequeue();
            item.Use(cart, throwBackward);

            UpdateItemVisuals(cart, cart.itemSlot);
            OnItemUse?.Invoke(cart);
        }
    }

    public bool CartHasMaxItems(Cart cart)
    {
        if (heldItems.ContainsKey(cart))
        {
            return heldItems[cart].Count >= 2; // 2 item max
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

    #region Item Display (In-Game)

    public void UpdateItemVisuals(Cart cart, Transform itemSlot)
    {
        var items = GetCartItems(cart);
        ClearItemSlot(itemSlot);
        if (items.Count > 0)
        {
            KartItem curItem = items[0];
            Instantiate(curItem.visualPrefab, itemSlot);
            // itemVisual.transform.localPosition = Vector3.zero;
            // itemVisual.transform.localRotation = Quaternion.identity;
        }
    }

    private void ClearItemSlot(Transform itemSlot)
    {
        foreach (Transform child in itemSlot)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void ClearCartItems(Cart cart)
    {
        if (heldItems.ContainsKey(cart))
        {
            heldItems[cart].Clear();
            UpdateItemVisuals(cart, cart.itemSlot);
        }
    }
    
    #endregion
}
