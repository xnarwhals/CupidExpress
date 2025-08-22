using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] Transform targetTransform;

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
        if (targetTransform == null) { Debug.LogWarning("Target Transform not assigned on teleporter!"); return; }
        if (AudioManager.Instance) AudioManager.Instance.PlayPortalSound();

        try
        {
            Transform kartTransform = other.GetComponent<BallKart>().kartTransform;
            kartTransform.rotation = targetTransform.rotation;
        }
        catch { other.transform.rotation = targetTransform.rotation; }

        try
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();

            rb.velocity = targetTransform.forward * rb.velocity.magnitude;
        } catch { }

        other.transform.position = targetTransform.position;
    }
}
