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

    private void Awake()
    {
        input = new PlayerCart();
        cartPhysics = cart.GetComponent<CartPhysics>();
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
            // Left-stick X controls steering
            float steer = input.Player.Steer.ReadValue<float>();

            // east btn accelerates, south btn brakes/reverses
            float throttle = 0f;
            if (input.Player.Accelerate.IsPressed()) throttle += 1f;
            if (input.Player.Brake.IsPressed()) throttle -= 1f;

            cartPhysics.SetSteer(steer);
            cartPhysics.SetThrottle(throttle);
        }

        if (role == CartRole.Passenger && input.Player.UseItem.triggered)
        {
            cart.UseItem();
        }

        if (input.Player.StartGame.triggered && GameManager.Instance.GetCurrentRaceState() == GameManager.RaceState.WaitingToStart)
        {
            Debug.Log("Game started");
            GameManager.Instance.StartRace();
        }
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
