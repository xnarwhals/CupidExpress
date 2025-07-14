using UnityEngine;

public class Cart : MonoBehaviour
{
    [Header("Seat Positions")]
    public Transform driverSeat;
    public Transform passengerSeat;

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
        if (cartPhysics == null ) print((CartPhysics)GetComponent<BallKart>());
        playerInputs = GetComponentsInChildren<CartPlayerInput>();
    }
    public void UseItem()
    {
        ItemManager.Instance.UseItem(this);
        // Debug.Log("Using item");
    }

}
