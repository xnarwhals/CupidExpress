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
    private KetchupEffect ketchupEffect; // player
    private AIDriver aiDriver; // AI
    private CartPlayerInput[] playerInputs;

    public string CartName => cartName;
    public int CartID => cartID;
    public CartPhysics CartPhysics => cartPhysics;

    private void Awake()
    {
        cartPhysics = GetComponent<CartPhysics>();
        if (cartPhysics == null ) print((CartPhysics)GetComponent<BallKart>());
        playerInputs = GetComponentsInChildren<CartPlayerInput>();
        ketchupEffect = GetComponent<KetchupEffect>();
        aiDriver = GetComponent<AIDriver>();
    }

    #region Cart Methods

    public void SpinOut(float duration)
    {
        if (cartID == 0) if (cartPhysics != null) cartPhysics.SpinOut(duration);
        // else if (aiDriver != null) aiDriver.SpinOut(duration);
    }

    public void StartKetchupEffect()
    {
        if (ketchupEffect != null) ketchupEffect.StartKetchupEffect();
    }

    public bool IsSpinningOut()
    {
        if (cartID == 0) return cartPhysics != null && cartPhysics.IsSpinningOut();
        // else return aiDriver != null && aiDriver.IsSpinningOut();
        return false;
    }

    public void ApplyBoost(float force)
    {
        if (cartID == 0) cartPhysics.ApplyBoost(force);
        // else aiDriver.ApplyBoost(force);
    }

    #endregion




}
