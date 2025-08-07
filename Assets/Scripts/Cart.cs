using UnityEngine;

public class Cart : MonoBehaviour
{
    [Header("Positions")]
    public Transform driverSeat;
    public Transform passengerSeat;
    public Transform itemSlot;
    public Transform forwardRef; // where items are thrown from

    [Header("Cart Properties")]
    public string cartName = "Player Cart";
    public int cartID = 0; // AI later

    [Header("Ref Components")]
    private CartPhysics cartPhysics;
    private BallKart ballKart;
    private KetchupEffect ketchupEffect; // player
    private AIDriver aiDriver; // AI
    private CartPlayerInput[] playerInputs;

    public bool isLeader = false; // ignore

    public string CartName => cartName;
    public int CartID => cartID;
    public CartPhysics CartPhysics => cartPhysics;
    public BallKart BallKart => ballKart;

    private void Awake()
    {
        cartPhysics = GetComponent<CartPhysics>();
        ballKart = GetComponent<BallKart>();

        //if (cartPhysics == null) print((CartPhysics)GetComponent<BallKart>());

        // if (cartPhysics == null || ballKart == null) print((CartPhysics)GetComponent<BallKart>());


        playerInputs = GetComponentsInChildren<CartPlayerInput>();
        ketchupEffect = GetComponent<KetchupEffect>();
        aiDriver = GetComponent<AIDriver>();
    }

    // Testing
    private void Start()
    {
        if (isLeader) GameManager.Instance.SetCartLap(this, 2);
    }

    #region Cart Methods
    public float GetSplineProgress()
    {
        if (aiDriver == null) return 0f; // human no spline
        return aiDriver.GetSplineProgress();

    }

    public void SpinOut(float duration)
    {
        if (cartID == 0) cartPhysics.SpinOut(duration);
        else if (aiDriver != null) aiDriver.SpinOut(duration);
    }

    // Player only
    public void StartKetchupEffect()
    {
        if (ketchupEffect != null) ketchupEffect.StartKetchupEffect();
    }

    public bool IsSpinningOut()
    {
        if (cartID == 0) return cartPhysics.isSpinningOut;
        else return aiDriver != null && aiDriver.StateController.currentState == AIDriverState.SpinningOut;

    }

    public void ApplyBoost(float duration, float speedMultiplier)
    {
        if (cartID == 0) ballKart.ApplyInstantBoost(20f);
        else aiDriver.ApplyBoost(duration, speedMultiplier);
    }

    public void Shock(float duration)
    {
        if (cartID == 0) ballKart.Shock(duration);
        else aiDriver.Shock(duration);
    }

    #endregion
}
