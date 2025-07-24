using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

public class BallKart : CartPhysics
{
    [SerializeField] Transform kartTransform; //the parent of the kart (used for movement)
    [SerializeField] Transform kartNormal; //the kart child of transform, parent of model
    [SerializeField] Transform kartModel; //the actual model

    [Header("Movement Settings")]
    [SerializeField] float floorGravity = 25f;
    [SerializeField] float airGravity = 25f;
    [SerializeField] float steerAccelleration = 4f;
    [SerializeField] float steerAcceleration2 = 5f;
    [SerializeField] float idleDecelleration = 1.0f;
    [SerializeField] float reverseAcceleration = 2.0f;
    [SerializeField] float reverseMaxSpeed = 15.0f;
    [SerializeField] float maxAngularVelocity = 7.0f;
    [SerializeField] float driftMaxAngularVelocity = 7.0f;

    [Header("Visual Stuff")]
    [SerializeField] float modelSteerOffset = 15f;
    [SerializeField] float modelSteerOffsetSmoothing = 0.2f;
    [SerializeField] float kartOrientationRayLength = 1.0f;
    [SerializeField] float rampSmoothing = 8.0f;
    [SerializeField] float airSmoothing = 0.2f;


    [Header("Controls")]
    [SerializeField] bool invertSteering = false;

    [Header("Other")]
    [SerializeField] LayerMask floorLayerMask;

    float currentSpeed = 0.0f;
    float currentRotate = 0.0f;
    float inputSpeed; //in case we want a more dynamic throttle system
    float inputRotation; //jik ^
    float currentAcceleration;

    Vector3 kartOffset;

    public override void Awake()
    {
        rb = GetComponent<Rigidbody>();

        kartOffset = kartTransform.position - transform.position;
    }

    private void Update()
    {
        //Update()
        float dt = Time.deltaTime;
        if (invertSteering) steerInput = -steerInput;

        //setting inputs to be used in fixed
        inputSpeed = maxSpeed * throttleInput; //in case we want a more dynamic throttle system
        inputRotation = steerInput * steerPower; //jik ^
        currentAcceleration = acceleration;

        //throttle forward vs backward acceleration & speed
        if (Mathf.Abs(throttleInput) <= 0.01f) currentAcceleration = idleDecelleration; //if no input, idle, uses rb drag in combination
        else if (throttleInput < 0) { currentAcceleration = reverseAcceleration; inputSpeed = reverseMaxSpeed * throttleInput; }

        currentSpeed = Mathf.SmoothStep(currentSpeed, inputSpeed, dt * currentAcceleration);
        currentRotate = Mathf.Lerp(currentRotate, inputRotation, dt * steerAccelleration);

        //Drift
        

        //tie the kart to the sphere
        kartTransform.position = transform.position + kartOffset;

        //model steering exaggeration/offset
        float steerDir = steerInput * modelSteerOffset;
        kartModel.localRotation = Quaternion.Euler(Vector3.Lerp(kartModel.localEulerAngles, new Vector3(0, (steerDir), kartModel.localEulerAngles.z), modelSteerOffsetSmoothing)); //model steering
    }

    public override void FixedUpdate()
    {
        float dt = Time.deltaTime;
        
        rb.AddForce(kartTransform.forward * currentSpeed, ForceMode.Acceleration);

        RaycastHit hitGravCheck;
        Physics.Raycast(kartTransform.position + (kartTransform.up * .1f), Vector3.down, out hitGravCheck, 2.0f, floorLayerMask); //find floor

        bool grounded = hitGravCheck.collider;

        float gravity = airGravity;
        if (grounded) gravity = floorGravity;

        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration); //also rb gravity exists

        kartTransform.eulerAngles = Vector3.Lerp(kartTransform.eulerAngles, new Vector3(0, kartTransform.eulerAngles.y + currentRotate, 0), dt * steerAcceleration2);

        //Drift
        if (DriftInput) { rb.maxAngularVelocity = driftMaxAngularVelocity; }
        else { rb.maxAngularVelocity = maxAngularVelocity; }

            //AIR CONTROL!!!!!!


            //kart puppeting
            RaycastHit hitNear;
        Physics.Raycast(kartTransform.position + (kartTransform.up * .1f), Vector3.down, out hitNear, kartOrientationRayLength, floorLayerMask); //find floor
        if (hitNear.collider) //if hit ground
        {
            kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, dt * rampSmoothing); //correct rotation
        }
        else
        {
            kartNormal.up = Vector3.Lerp(kartNormal.up, new Vector3(0, 1, 0), dt * airSmoothing); //correct rotation
        }
        kartNormal.Rotate(0, kartTransform.eulerAngles.y, 0);
    }
}
