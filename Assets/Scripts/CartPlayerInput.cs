using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    public TestItemEffect testItemEffect;
    [SerializeField] private int playerIndex = 0; // distinguish players using same script
    [SerializeField] bool printRawInput = false;

    [Header("Arduino")] //organize later?
    public float maxPressL = 80.0f;
    public float maxPressR = 80.0f;
    public float steerDeadzoneL = 10f;
    public float steerDeadzoneR = 10f;
    public float deadzoneScale = 1.0f;
    public float stepThreshold = 200.0f;
    public float itemLThreshold = 200.0f;
    public float stepBpm = 50.0f;
    public float reverseStepCount = 1.0f;

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
            if (playerIndex == 0)
            {
                input.devices = new InputDevice[] { Gamepad.all[0], Keyboard.current };
            }
            else
            {
                input.devices = new InputDevice[] { Gamepad.all[playerIndex] };
            }
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
        if (input.Player.Reset.triggered)
        {
            cartPhysics.Reset();
        }

        if (role == CartRole.Driver)
        {
            // Left-stick X or force sensors control steering
            float steer = 0.0f;

            if (currentThrottle != 0.0f && Time.time - prevStepTime > (60.0f / stepBpm)) currentThrottle = 0.0f; //stop the cart from going if no step has happened yet
            if (messageHandler != null) //if arduino
            {
                //steer
                float left = messageHandler.input0;
                float right = messageHandler.input1;


                if (left >  maxPressL && right >  maxPressR) steer = 0.0f; //if both on, go forward
                else
                {
                    //deadzones
                    if (left < steerDeadzoneL) left *= deadzoneScale;
                    if (right < steerDeadzoneR) right *= deadzoneScale;

                    steer = Mathf.Clamp(right / maxPressR - left / maxPressL, -1.0f, 1.0f);
                }

                //stepping
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
                else if (stepL > stepThreshold && stepR > stepThreshold && Time.time - prevStepTime > (60.0f / (stepBpm / reverseStepCount)))
                {
                    currentThrottle = -1.0f;
                }

                //items
                float itemL = messageHandler.input4;
                if (itemL > itemLThreshold)
                {
                    ItemManager.Instance.UseItem(cart, false);
                }

                //printing
                if (printRawInput)
                    print("raw vals: " + left + ", " + right + " | Steer: " + steer);
                //print("steering: " + ((messageHandler.input1 - messageHandler.input0) / cartPhysics.maxPress).ToString());
            }

            if (steer == 0.0f) //if no arduino input, use controller
            {
                steer = input.Player.Steer.ReadValue<float>();

            }

            //drift


                //cartPhysics.Drift(input.Player.Drift.IsPressed());
                cartPhysics.Drift(input.Player.Drift.ReadValue<float>() > 0.5f);

            // east btn accelerates, south btn brakes/reverses
            if (input.Player.Accelerate.IsPressed()) currentThrottle = 1f;
            if (input.Player.Brake.IsPressed()) currentThrottle = -1f;

            cartPhysics.SetSteer(steer);
            cartPhysics.SetThrottle(currentThrottle);
        }


        // ITEM USE CODE
        if (role == CartRole.Driver && input.Player.UseItem.triggered)
        {
            bool throwItBack = false;

            // Check if '2' key was pressed this frame
            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
                throwItBack = true;

            // Also check for left trigger (gamepad)
            InputControl itemTrigger = input.Player.UseItem.activeControl;
            if (itemTrigger != null && itemTrigger.path.Contains("leftTrigger"))
                throwItBack = true;

            ItemManager.Instance.UseItem(cart, throwItBack);
        }

        if (input.Player.StartGame.triggered && GameManager.Instance.GetCurrentRaceState() == GameManager.RaceState.WaitingToStart)
        {
            // Debug.Log("Game started");
            GameManager.Instance.StartRace();
        }

        if (testItemEffect != null && input.Player.DebugBtn.WasPressedThisFrame())
        {
            testItemEffect.Test();
        }

    }

    void Step (bool side)
    {
        if (prevStep == !side)
        {
            /*if (Time.time - prevStepTime <= (60.0f / stepBpm))
            {
                currentThrottle = 1f;
            }*/

            currentThrottle = Mathf.Clamp((60.0f / stepBpm) / (Time.time - prevStepTime), 0.0f, 1.0f);
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
