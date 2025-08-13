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
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemPickup += HandleItemsChanged;
            ItemManager.Instance.OnItemUse += HandleItemsChanged;
        }
        UpdateItemUI();
  
    }

    private void OnDisable()
    {
        ItemManager.Instance.OnItemPickup -= HandleItemsChanged;
        ItemManager.Instance.OnItemUse -= HandleItemsChanged;
    }

    private void HandleItemsChanged(Cart cart)
    {
        if (cart == playerCart) // Only update UI for player cart
        {
            UpdateItemUI();
            // AudioManager.Instance.PlaySFX(AudioManager.Instance.itemPickupSound, 0.8f);
        }
    }

    private void UpdateItemUI()
    {
        var items = ItemManager.Instance.GetCartItems(playerCart);

        // First item slot
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

        // Second item slot
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