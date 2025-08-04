using UnityEngine;

public class Cart : MonoBehaviour
{
    [Header("Positions")]
    public Transform driverSeat;
    public Transform passengerSeat;
    public Transform itemSlot;
    public Transform forwardRef; // where items are thrown from

    [Header("Cart Properties")]
    [SerializeField] private string cartName = "Player Cart";
    [SerializeField] private int cartID = 0; // AI later

    [Header("Ref Components")]
    private CartPhysics cartPhysics;
    private KetchupEffect ketchupEffect; // player
    private AIDriver aiDriver; // AI
    private CartPlayerInput[] playerInputs;

    public bool isLeader = false;

    public string CartName => cartName;
    public int CartID => cartID;
    public CartPhysics CartPhysics => cartPhysics;

    private void Awake()
    {
        cartPhysics = GetComponent<CartPhysics>();

        if (cartPhysics == null) print((CartPhysics)GetComponent<BallKart>());

        playerInputs = GetComponentsInChildren<CartPlayerInput>();
        ketchupEffect = GetComponent<KetchupEffect>();
        aiDriver = GetComponent<AIDriver>();
    }

    private void Start()
    {
        if (isLeader)
        {
            GameManager.Instance.SetCartLap(this, 2);
        }
    }

    #region Cart Methods
    public float GetSplineProgress()
    {
        if (aiDriver == null) return 0f;
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
        if (cartID == 0) Debug.Log("Player Cart Boost");
        else aiDriver.ApplyBoost(duration, speedMultiplier);
    }

    #endregion




}
