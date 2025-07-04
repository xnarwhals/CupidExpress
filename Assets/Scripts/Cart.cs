using UnityEngine;

public class Cart : MonoBehaviour
{
    [Header("Positions")]
    public Transform driverSeat;
    public Transform passengerSeat;
    public Transform itemSlot;

    [Header("Cart Properties")]
    [SerializeField] private string cartName = "Player Cart";
    [SerializeField] private int cartID = 0; // AI later

    [Header("Ref Components")]
    private CartPhysics cartPhysics;
    private CartPlayerInput[] playerInputs;

    public string CartName => cartName;
    public int CartID => cartID;
    public CartPhysics CartPhysics => cartPhysics;

    private void Awake()
    {
        cartPhysics = GetComponent<CartPhysics>();
        playerInputs = GetComponentsInChildren<CartPlayerInput>();
    }

    public void OnItemAdded()
    {
        UpdateItemVisuals();
    }

    public void OnItemUsed()
    {
        UpdateItemVisuals();
    }

    private void UpdateItemVisuals()
    {
        var items = ItemManager.Instance.GetCartItems(this);
        ClearItemSlot();
        if (items.Count > 0)
        {
            KartItem curItem = items[0];
            GameObject itemVisual = Instantiate(curItem.visualPrefab, itemSlot);
            itemVisual.transform.localPosition = Vector3.zero; // Adjust as needed
            itemVisual.transform.localRotation = Quaternion.identity; // Reset rotation
        }
    }

    private void ClearItemSlot()
    {
        foreach (Transform child in itemSlot)
        {
            Destroy(child.gameObject);
        }
    }

    public void UseItem(bool itemCanBeUsedBehind)
    {
        ItemManager.Instance.UseItem(this, itemCanBeUsedBehind);
    }

    #region Cart Methods

    #endregion

    


}
