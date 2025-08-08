using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boostPad : MonoBehaviour
{
    [SerializeField] float boostSpeed = 100.0f;
    [SerializeField] float boostAccelleration = 100.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        BallKart cartPhysics = other.gameObject.GetComponent<BallKart>();
        if (cartPhysics)
        {
            cartPhysics.Boost(boostSpeed, boostAccelleration, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BallKart cartPhysics = other.gameObject.GetComponent<BallKart>();
        if (cartPhysics)
        {
            cartPhysics.Boost(boostSpeed, boostAccelleration, false);
        }
    }
}
