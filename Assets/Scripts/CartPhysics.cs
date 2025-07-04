using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartPhysics : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float breakForce = 200f; // N/kg
    [SerializeField] private float maxSpeed = 15f; // m/s 
    [SerializeField] private float reverseSpeed = 8f;
    [SerializeField] private float steerSpeed = 150f;
    [SerializeField] private float acceleration = 800f;
    [SerializeField] private float drag = 0.95f;

    public float downforce = 2f; // Downforce multiplier

    // runtime state
    private float steerInput; // -1 to 1, left to right
    private float throttleInput; // -1 to 1, reverse to forward
    private float curSpeed;

    Rigidbody rb;

    // Called by driver
    public void SetSteer(float steer) => steerInput = Mathf.Clamp(steer, -1f, 1f);
    public void SetThrottle(float throttle) => throttleInput = Mathf.Clamp(throttle, -1f, 1f);

    public void SetInputs(float steer, float throttle)
    {
        SetSteer(steer);
        SetThrottle(throttle);
    }

    // life cycle 
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0f);
    }

    // physics
    void FixedUpdate()
    {
        HandleThrottle();
        HandleSteering();
        ApplyDownforce();
        ApplyDrag();
    }

    void HandleThrottle()
    {
        curSpeed = Vector3.Dot(rb.velocity, transform.forward); // m/s

        if (throttleInput > 0)
        {
            if (curSpeed < maxSpeed)
            {
                float speedRatio = curSpeed / maxSpeed;
                float accelerationCurve = 1f - (speedRatio * speedRatio); // Quadratic reduction

                float finalAcceleration = acceleration * accelerationCurve * throttleInput; // Reduce acceleration at high speeds
                rb.AddForce(transform.forward * finalAcceleration, ForceMode.Acceleration);
            }
        }
        else if (throttleInput < 0)
        {
            if (curSpeed > 0.1f) // break
            {
                rb.AddForce(-transform.forward * breakForce * Mathf.Abs(throttleInput), ForceMode.Acceleration);
            }
            else if (curSpeed > -reverseSpeed) // reverse
            {
                rb.AddForce(transform.forward * throttleInput * acceleration * 0.5f, ForceMode.Acceleration);
            }
        }
    }

    void HandleSteering()
    {
        if (Mathf.Abs(curSpeed) > 0.5f)
        {
            float steerAmount = steerInput * steerSpeed * Time.fixedDeltaTime;
            float speedFactor = Mathf.Clamp01(Mathf.Abs(curSpeed) / maxSpeed); // 0 to 1 based on speed
            steerAmount *= Mathf.Lerp(1f, 0.4f, speedFactor); // reduce steering at high speeds

            if (curSpeed < 0) steerAmount = -steerAmount;

            transform.Rotate(0, steerAmount, 0);
        }
    }

    void ApplyDownforce()
    {
        float speedRatio = Mathf.Clamp01(curSpeed / maxSpeed);
        rb.AddForce(-transform.up * downforce * speedRatio, ForceMode.Acceleration); // Downforce proportional to speed
    }

    void ApplyDrag()
    {
        rb.velocity *= drag; // Apply drag to slow down over time
    }

    #region Getters
    public float GetCurrentSpeed() => curSpeed;
    public float GetMaxSpeed() => maxSpeed;

    #endregion
}


