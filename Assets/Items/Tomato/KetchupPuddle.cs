using UnityEngine;

public class KetchupPuddle : MonoBehaviour
{
    private float slowdownEffect;
    private float puddleDuration;

    public void Initialize(float slowdown, float duration)
    {
        slowdownEffect = slowdown;
        puddleDuration = duration;

        GetComponent<Collider>().isTrigger = true;
        Destroy(gameObject, puddleDuration); // Destroy after duration
    }

    private void OnTriggerEnter(Collider other)
    {
        Cart hitCart = other.GetComponent<Cart>();
        if (hitCart != null)
        {

        }
    }

    private void OnTriggerExit(Collider other)
    {
        Cart hitCart = other.GetComponent<Cart>();
        if (hitCart != null)
        {

        }
    }
}