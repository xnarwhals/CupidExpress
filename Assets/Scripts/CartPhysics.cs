using System;
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



    // runtime state
    protected float steerInput; // -1 to 1, left to right
    protected float throttleInput; // -1 to 1, reverse to forward
    protected bool isDrifting = false;

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

        rb.centerOfMass += Vector3.down * 0.3f; // Set center of mass to the center of the cart
    }

    // physics
    public virtual void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime; // we love delta time

        // thrust forward / backward
        float accellMagnitude = acceleration; //if forward, use forward accell
        if (throttleInput < 0) accellMagnitude = -breakForce; //if breaking, use breakforce
        else if (throttleInput == 0) accellMagnitude = 0; //else don't accellerate

        Vector3 forward = transform.forward * accellMagnitude * rb.mass;
        rb.AddForce(forward, ForceMode.Acceleration);

        // cap speed
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            rb.velocity = flatVelocity.normalized * maxSpeed + Vector3.up * rb.velocity.y; // maintain y velocity   
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
}
