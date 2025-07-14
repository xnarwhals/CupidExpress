using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BallKart : CartPhysics
{
    public override void Awake()
    {
        
    }

    public override void FixedUpdate()
    {
        print(steerInput);
    }
}
