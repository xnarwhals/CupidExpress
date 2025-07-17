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
    [SerializeField] float gravity = 25f;
    [SerializeField] float steerAccelleration = 4f;
    [SerializeField] float steerAcceleration2 = 5f;
    [SerializeField] float idleDecelleration = 1.0f;
    [SerializeField] float reverseAcceleration = 2.0f;
    [SerializeField] float reverseMaxSpeed = 15.0f;

    [Header("Model SubSteering")]
    [SerializeField] float modelSteerOffset = 15f;
    [SerializeField] float modelSteerOffsetSmoothing = 0.2f;


    [Header("Other")]
    [SerializeField] float rampSmoothing = 8.0f;
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
        float dt = Time.deltaTime;

        //setting inputs to be used in fixed
        inputSpeed = maxSpeed * throttleInput; //in case we want a more dynamic throttle system
        inputRotation = steerInput * steerPower; //jik ^
        currentAcceleration = acceleration;

        if (Mathf.Abs(throttleInput) <= 0.01f) currentAcceleration = idleDecelleration; //if no input, idle, uses rb drag in combination
        else if (throttleInput < 0) { currentAcceleration = reverseAcceleration; inputSpeed = reverseMaxSpeed * throttleInput; }

        currentSpeed = Mathf.SmoothStep(currentSpeed, inputSpeed, dt * currentAcceleration);
        currentRotate = Mathf.Lerp(currentRotate, inputRotation, dt * steerAccelleration);

        //tie the kart to the sphere
        kartTransform.position = transform.position + kartOffset;

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

        if (grounded) //if grounded, do gravity
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration); //also rb gravity exists

        print(grounded);

        kartTransform.eulerAngles = Vector3.Lerp(kartTransform.eulerAngles, new Vector3(0, kartTransform.eulerAngles.y + currentRotate, 0), dt * steerAcceleration2);

        //AIR CONTROL!!!!!!

        //kart puppeting
        if (grounded)
        {
            RaycastHit hitNear;
            Physics.Raycast(kartTransform.position + (kartTransform.up * .1f), Vector3.down, out hitNear, 2.0f, floorLayerMask); //find floor

            kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, dt * rampSmoothing); //correct rotation
        }
        else
        {
            kartNormal.up = Vector3.Lerp(kartNormal.up, new Vector3(0, 0, 0), dt * rampSmoothing * 0.5f); //correct rotation
        }
        kartNormal.Rotate(0, kartTransform.eulerAngles.y, 0);
    }
}
