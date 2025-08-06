using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartPhysics : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float acceleration = 35f; // N/kg
    [SerializeField] public float maxSpeed = 25f; // m/s 
    [SerializeField] protected float breakForce = 50f; // N/kg
    [SerializeField] protected float steerPower = 4f; // rad/s

    [Header("Grip")]
    [SerializeField] protected float traction = 4f; // increase for snappy handling
    [SerializeField] protected float tractionDrift = 2f; // hold less when drift

    // runtime state
    [DoNotSerialize] public float steerInput; // -1 to 1, left to right
    protected float throttleInput; // -1 to 1, reverse to forward
    [DoNotSerialize] public bool DriftInput = false;

    protected Rigidbody rb;
    protected float curTraction;
    public bool isSpinningOut = false;
    protected float spinOutTimer = 0f;
    public float spinOutDuration;
    protected Quaternion originalRotation;


    // API
    public void SetSteer(float steer) => steerInput = Mathf.Clamp(steer, -1f, 1f);
    public void SetThrottle(float throttle) => throttleInput = Mathf.Clamp(throttle, -1f, 1f);
    public void Drift(bool on) => DriftInput = on;

    // life cycle 
    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        curTraction = traction;
    }


    // private void HandleThrottle()
    // {
    //     curSpeed = Vector3.Dot(rb.velocity, transform.forward); // m/s
    // }

    public virtual void Reset()
    {
        //in ballkart
    }

    public virtual void FixedUpdate()
    {

    }

    public virtual void SpinOut(float duration)
    {
        isSpinningOut = true;
        spinOutTimer = 0f;
        spinOutDuration = duration;
        
    }

    // public void ApplyBoost(float force)
    // {
    //     if (isSpinningOut) return;
    //     rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
    // }
    
}
