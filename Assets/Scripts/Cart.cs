using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private SimpleAIDriver aiDriver; // AI
    private CartPlayerInput[] playerInputs;
    private PlayerSplineProgress splineProgress;
    public string CartName => cartName;
    public int CartID => cartID;
    public CartPhysics CartPhysics => cartPhysics;
    public BallKart BallKart => ballKart;
    public SimpleAIDriver AIDriver => aiDriver;
    public Collider col;

    private void Awake()
    {
        cartPhysics = GetComponent<CartPhysics>();
        ballKart = GetComponent<BallKart>();
        splineProgress = GetComponent<PlayerSplineProgress>();
        aiDriver = GetComponent<SimpleAIDriver>();
        col = GetComponent<Collider>();


        playerInputs = GetComponentsInChildren<CartPlayerInput>();
        ketchupEffect = GetComponent<KetchupEffect>();
        // aiDriver = GetComponent<AIDriver>();
    }

    // Testing
    private void Start()
    {
        if (cartID == 0) cartName = PlayerData.PlayerName;
        if (CartID == 0 && splineProgress == null) Debug.LogWarning("PlayerSplineProgress component not found on Player!");
        if (cartID != 0 && aiDriver == null) Debug.LogWarning("SimpleAIDriver component not found on AI Cart!");
    }

    private void Update()
    {
        // if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
        // if (Joystick.current != null && Joystick.current.trigger.wasPressedThisFrame)
        // {
        //     if (cartID == 0) return;
        //     SpinOut(3f);
        //     ApplyBoost(5f, 1.5f);
        //     Shock(3f);
        // }
    }

    #region Cart Methods
    public float GetSplineProgress()
    {
        if (cartID == 0 && splineProgress != null) return splineProgress.splineProgress; // player
        else if (aiDriver != null) return aiDriver.GetSplineProgress(); // AI
        return 0f;

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
        else return aiDriver != null && aiDriver.CurrentState == AIDriverState.SpinningOut;

    }

    public void ApplyBoost(float duration, float speedMultiplier, float force)
    {
        if (cartID == 0) ballKart.ApplyInstantBoost(force);
        else aiDriver.StartBoost(duration, speedMultiplier);
    }

    public void Shock(float duration)
    {
        if (cartID == 0) ballKart.Shock(duration);
        else aiDriver.Shock(duration);
    }

    public Rigidbody GetRB()
    {
        if (cartPhysics != null) return cartPhysics.GetRB();
        if (aiDriver != null) return aiDriver.GetRB();
        return null;
    }
    

    #endregion
}
