using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartPhysics : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float acceleration = 35f; // N/kg
    [SerializeField] protected float maxSpeed = 25f; // m/s 
    [SerializeField] protected float breakForce = 50f; // N/kg
    [SerializeField] protected float steerPower = 4f; // rad/s

    [Header("Arduino")]
    [SerializeField] public float maxPress = 45.0f;
    [SerializeField] public float deadzone = 0.2f;
    [SerializeField] public float deadzoneScale = 0.2f;

    [Header("Grip")]
    [SerializeField] protected float traction = 4f; // increase for snappy handling
    [SerializeField] protected float tractionDrift = 2f; // hold less when drift
    [SerializeField] AnimationCurve driftSteerCurve = AnimationCurve.Linear(0,1,1,0.2f);

    // spin out 
    private bool isSpinningOut = false;
    private float spinOutTimer = 0f;
    private float spinOutDuration = 2f;
    private Quaternion originalRotation;
    private bool isRecoveringRotation = false;
    private float recoverTimer = 0f;
    private float recoverTime = 0.5f;
    

    Rigidbody rb;

    // runtime state
    [DoNotSerialize] public float steerInput; // -1 to 1, left to right
    protected float throttleInput; // -1 to 1, reverse to forward
    [DoNotSerialize] public bool isDrifting = false;

    // spin out 
    private bool isSpinningOut = false;
    private float spinOutTimer = 0f;
    private float spinOutDuration = 2f;
    private Quaternion originalRotation;
    private bool isRecoveringRotation = false;
    private float recoverTimer = 0f;
    private float recoverTime = 0.5f;
    
    protected Rigidbody rb;
    protected float curTraction;

    // API
    public void SetSteer(float steer) => steerInput = Mathf.Clamp(steer, -1f, 1f);
    public void SetThrottle(float throttle) => throttleInput = Mathf.Clamp(throttle, -1f, 1f);
    public void Drift(bool on) => isDrifting = on;

    // life cycle 
    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        curTraction = traction;

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
    }


    public virtual void FixedUpdate()
    {
        /*if (isSpinningOut)
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
        }*/
    }

    /*private void HandleThrottle()
    {
        curSpeed = Vector3.Dot(rb.velocity, transform.forward); // m/s

        Vector3 forward = transform.forward * accellMagnitude * rb.mass;
        rb.AddForce(forward, ForceMode.Acceleration);

        // cap speed
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            if (curSpeed < maxSpeed)
            {
                rb.AddForce(transform.forward * acceleration * throttleInput, ForceMode.Acceleration);
            }
        }

        // apply braking force
        if (throttleInput < 0)
        {
            // rb.AddForce(-flatVelocity.normalized * breakForce * rb.mass, ForceMode.Acceleration);
        }

        // steering
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, flatVelocity.magnitude); // 0 to 1 based on speed
        float steerStrength = steerPower * speedFactor; // harder to steer at high speeds

        // maybe
        if (isDrifting) steerStrength *= driftSteerCurve.Evaluate(speedFactor); // reduce steer strength when drifting

        rb.AddTorque(Vector3.up * steerInput * steerStrength * rb.mass, ForceMode.Acceleration);

        curTraction = Mathf.Lerp(curTraction, isDrifting ? tractionDrift : traction, dt * 3); // lerp traction based on speed

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x *= Mathf.Pow(1f - curTraction * dt, 10f);
        rb.velocity = transform.TransformDirection(localVelocity);
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

    public void ApplyBoost(float force)
    {
        if (isSpinningOut) return; 
        rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
    }

    #region Getters
    public float GetCurrentSpeed() => curSpeed;
    public float GetMaxSpeed() => maxSpeed;
    public bool IsSpinningOut() => isSpinningOut;

    #endregion*/
}
