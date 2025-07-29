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
        return aiDriver.SplineProgress;

    }

    public void SpinOut(float duration)
    {
        Debug.Log("spin out");
        if (cartID == 0) Debug.Log("Player Cart Spin Out");
        else if (aiDriver != null) aiDriver.SpinOut(duration);
    }

    // Player only
    public void StartKetchupEffect()
    {
        if (ketchupEffect != null) ketchupEffect.StartKetchupEffect();
    }

    public bool IsSpinningOut()
    {
        // if (cartID == 0) return cartPhysics != null && cartPhysics.IsSpinningOut();
        // else return aiDriver != null && aiDriver.IsSpinningOut();
        return false;
    }

    public void ApplyBoost(float force)
    {   
        Debug.Log("apply boost");
        if (cartID == 0) Debug.Log("Player Cart Boost");
        else aiDriver.ApplyBoost(force);
    }

    #endregion




}
