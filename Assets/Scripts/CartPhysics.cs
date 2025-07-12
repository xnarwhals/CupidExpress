using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartPhysics : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float breakForce = 200f; // N/kg
    [SerializeField] private float maxSpeed = 15f; // m/s 
    [SerializeField] private float reverseSpeed = 8f;
    [SerializeField] private float steerSpeed = 150f;
    [SerializeField] private float acceleration = 800f; // N/kg

    // runtime state
    private float steerInput; // -1 to 1, left to right
    private float throttleInput; // -1 to 1, reverse to forward
    private float curSpeed;

    // spin out 
    private bool isSpinningOut = false;
    private float spinOutTimer = 0f;
    private float spinOutDuration = 2f;
    private Quaternion originalRotation;
    private bool isRecoveringRotation = false;
    private float recoverTimer = 0f;
    private float recoverTime = 0.5f;
    

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


    void FixedUpdate()
    {
        if (isSpinningOut)
        {
            spinOutTimer += Time.fixedDeltaTime;
            if (spinOutTimer >= spinOutDuration)
            {
                isSpinningOut = false; // Reset after duration
                spinOutTimer = 0f;

                // restore rotation
                isRecoveringRotation = true;
                recoverTimer = 0f;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            HandleThrottle();
            HandleSteering();
        }

        if (isRecoveringRotation)
        {
            recoverTimer += Time.fixedDeltaTime;
            float t = recoverTimer / recoverTime;
            if (t >= 1f)
            {
                t = 1f;
                isRecoveringRotation = false;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, t);
        }
    }

    private void HandleThrottle()
    {
        curSpeed = Vector3.Dot(rb.velocity, transform.forward); // m/s

        if (throttleInput > 0)
        {
            if (curSpeed < maxSpeed)
            {
                rb.AddForce(transform.forward * acceleration * throttleInput, ForceMode.Acceleration);
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

    private void HandleSteering()
    {
        if (Mathf.Abs(curSpeed) > 0.5f)
        {
            float steerAmount = steerInput * steerSpeed * Time.fixedDeltaTime;
            float speedFactor = Mathf.Clamp01(Mathf.Abs(curSpeed) / maxSpeed); // 0 to 1 based on speed
            steerAmount *= Mathf.Lerp(1f, 0.4f, speedFactor); // reduce steering at high speeds

            if (curSpeed < 0) steerAmount = -steerAmount; // reverse steer 

            transform.Rotate(0, steerAmount, 0);
        }
    }

    public void SpinOut(float duration = 2f)
    {
        if (isSpinningOut) return; // I have mercy --> no stacking
        isSpinningOut = true;
        spinOutTimer = 0f;
        spinOutDuration = duration;

        originalRotation = transform.rotation; 

        Vector3 ySpin = transform.up * Random.Range(-1f, 1f);
        rb.AddTorque(ySpin * 1000f, ForceMode.VelocityChange); // Apply a strong torque to spin out

    }

    #region Getters
    public float GetCurrentSpeed() => curSpeed;
    public float GetMaxSpeed() => maxSpeed;
    public bool IsSpinningOut() => isSpinningOut;

    #endregion
}


