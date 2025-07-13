using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BallKart : CartPhysics
{
    public GameObject kartModel;

    float speed, currentSpeed = 0.0f;
    float rotate, currentRotate = 0.0f;

    public override void setup()
    {
        
    }

    public override void Movement(Rigidbody rigidbody, float dt, float accell, float maxVel, float steerPow, float breakingForce, float throttle, float steerIn)
    {
        if (throttle > 0) speed = accell;
        if (throttle < 0) speed = breakingForce;
        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f); speed = 0f;

        rotate = steerIn * steerPow;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f); rotate = 0f;

        rigidbody.AddForce(kartModel.transform.forward * currentSpeed, ForceMode.Acceleration);

        kartModel.transform.position = rigidbody.position;
    }
}
