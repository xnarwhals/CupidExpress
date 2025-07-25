using UnityEngine;
using UnityEngine.InputSystem;

public enum CartRole
{
    Driver,
    Passenger
}

public class CartPlayerInput : MonoBehaviour
{
    public CartRole role;
    public Cart cart;
    private CartPhysics cartPhysics; 
    private PlayerCart input; // bad name, "CartPlayerControls"
    [SerializeField] private int playerIndex = 0; // distinguish players using same script
    [SerializeField] bool printRawInput = false;

    [Header("Arduino")]
    [SerializeField] public float maxPress = 80.0f;
    [SerializeField] public float deadzone = 10.2f;
    [SerializeField] public float deadzoneScale = 1.0f;
    [SerializeField] public float stepThreshold = 200.0f;
    [SerializeField] public float stepBpm = 50.0f;

    float prevStepTime;
    bool prevStep; //false is left, true is right

    float currentThrottle = 0.0f;

    ArduinoMessageHandler messageHandler;

    private void Awake()
    {
        prevStepTime = Time.time;

        input = new PlayerCart();
        cartPhysics = FindAnyObjectByType<CartPhysics>();

        messageHandler = GameObject.FindAnyObjectByType<ArduinoMessageHandler>();
    }

    private void OnEnable()
    {
        input.Enable();

        if (Gamepad.all.Count > playerIndex)
        {
            input.devices = new InputDevice[] { Gamepad.all[playerIndex] }; // assign specific gamepad to this player
        }

        input.Player.SwapRoles.performed += ctx =>
        {
            CartRoleManager.Instance.TrySwapRoles(playerIndex);
        }; // callback context 

    }

    private void Start()
    {
        CartRoleManager.Instance.RegisterPlayer(this);
    }

    private void Update()
    {
        if (role == CartRole.Driver)
        {
            // Left-stick X or force sensors control steering
            float steer = 0.0f;

            if (messageHandler != null) //if arduino
            {
                float left = messageHandler.input0;
                float right = messageHandler.input1;

                if (left >  maxPress && right >  maxPress) steer = 0.0f; //if both on, go forward
                else
                {
                    steer = Mathf.Clamp((right - left) /  maxPress, -1.0f, 1.0f);
                    if (Mathf.Abs(steer) <  deadzone) steer *=  deadzoneScale;
                }

                float stepL = messageHandler.input2;
                float stepR = messageHandler.input3;

                if (stepL > stepThreshold && stepR < stepThreshold) //step left
                {
                    Step(false);
                }
                else if (stepR > stepThreshold && stepL < stepThreshold) //step right
                {
                    Step(true);
                }
                else if (currentThrottle > 0.0f && Time.time - prevStepTime <= (60.0f / stepBpm)) currentThrottle = 0.0f;

                if (currentThrottle <= 0.01f) print("idling");
                else print("running");

                if (printRawInput)
                    print("raw vals: " + left + ", " + right + " | Steer: " + steer);
                //print("steering: " + ((messageHandler.input1 - messageHandler.input0) / cartPhysics.maxPress).ToString());
            }

            if (steer == 0.0f) //if no arduino input, use controller
            {
                steer = input.Player.Steer.ReadValue<float>();
            }

            //drift
            cartPhysics.Drift(input.Player.Drift.ReadValue<float>() > 0.5f);

            // east btn accelerates, south btn brakes/reverses
            if (input.Player.Accelerate.IsPressed()) currentThrottle = 1f;
            if (input.Player.Brake.IsPressed()) currentThrottle = -1f;

            cartPhysics.SetSteer(steer);
            cartPhysics.SetThrottle(currentThrottle);
        }

        if (role == CartRole.Passenger && input.Player.UseItem.triggered)
        {
            // Same action (use item) two inputs
            InputControl itemTrigger = input.Player.UseItem.activeControl;
            bool itemCanBeUsedBehind = itemTrigger?.path.Contains("rightShoulder") ?? false;
            ItemManager.Instance.UseItem(cart, itemCanBeUsedBehind);
        }

        if (input.Player.StartGame.triggered && GameManager.Instance.GetCurrentRaceState() == GameManager.RaceState.WaitingToStart)
        {
            // Debug.Log("Game started");
            GameManager.Instance.StartRace();
        }
    }

    void Step (bool side)
    {
        if (prevStep == !side)
        {
            if (Time.time - prevStepTime <= (60.0f / stepBpm))
            {
                currentThrottle = 1f;
            }
        }

        prevStep = side;
        prevStepTime = Time.time;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    public void AssignRole(CartRole newRole)
    {
        role = newRole;

        if (role == CartRole.Driver)
        {
            // Debug.Log($"{gameObject.name} assigned role: Driver");
            MoveToSeat(cart.driverSeat);
        }
        else
        {
            // Debug.Log($"{gameObject.name} assigned role: Passenger");
            MoveToSeat(cart.passengerSeat);
        }
        // Debug.Log($"{gameObject.name} assigned role: {role}");
    }

    
    private void MoveToSeat(Transform seat)
    {
        transform.SetLocalPositionAndRotation(seat.localPosition, seat.localRotation);
    }
}
