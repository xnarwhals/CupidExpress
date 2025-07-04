using UnityEngine.UI;
using UnityEngine;

public class ItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image firstItemSlot;
    public Image secondItemSlot;

    public Cart playerCart;

    private void Start()
    {
        ItemManager.Instance.OnItemAdded += onItemChanged;
        ItemManager.Instance.OnItemUsed += onItemChanged;
        UpdateItemUI();
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemAdded -= onItemChanged;
            ItemManager.Instance.OnItemUsed -= onItemChanged;
        }
    }

    private void onItemChanged(Cart cart)
    {
        if (cart == playerCart) // AI carts don't have a UI
        {
            UpdateItemUI();
        }
    }

    private void UpdateItemUI()
    {
        var items = ItemManager.Instance.GetCartItems(playerCart);

        if (items.Count > 0)
        {
            firstItemSlot.sprite = items[0].icon;
            firstItemSlot.enabled = true;
        }
        else
        {
            firstItemSlot.sprite = null;
            firstItemSlot.enabled = false;
        }

        if (items.Count > 1)
        {
            secondItemSlot.sprite = items[1].icon;
            secondItemSlot.enabled = true;
        }
        else
        {
            secondItemSlot.sprite = null;
            secondItemSlot.enabled = false;
        }
    }

    
}
