using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boostPad : MonoBehaviour
{
    [SerializeField] float boost = 100.0f;
    float boostAccelleration = 100.0f;

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
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySodaBlast();
            cartPhysics.Boost(boost, boostAccelleration, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BallKart cartPhysics = other.gameObject.GetComponent<BallKart>();
        if (cartPhysics)
        {
            cartPhysics.Boost(boost, boostAccelleration, false);
        }
    }
}
