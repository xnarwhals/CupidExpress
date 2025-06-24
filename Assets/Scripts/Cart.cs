using UnityEngine;

public class Cart : MonoBehaviour
{
    private Vector2 driveInput;
    public Rigidbody rb;
    public float speed = 1000f;
    public float turnSpeed = 150f;
    public Transform driverSeat;
    public Transform passengerSeat;

    public void SetDriveInput(Vector2 input)
    {
        driveInput = input;
    }

    public void UseItem()
    {
        Debug.Log("Using item");
    }

    void FixedUpdate()
    {
        Vector3 forward = transform.forward * driveInput.y * speed * Time.fixedDeltaTime;
        rb.AddForce(forward);

        if (Mathf.Abs(driveInput.y) > 0.1f)
        {
            float turn = driveInput.x * turnSpeed * Time.fixedDeltaTime;
            rb.AddTorque(Vector3.up * turn);
        }  
    }
}
