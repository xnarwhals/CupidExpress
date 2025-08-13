using UnityEngine;

public class KetchupPuddle : MonoBehaviour
{
    private float puddleDuration;
    private float spinOutDuration;

    public void Initialize(float puddleSpinOutDuration, float duration)
    {
        puddleDuration = duration;
        spinOutDuration = puddleSpinOutDuration;
        // Debug.Log("Spin out duration: " + spinOutDuration);
        GetComponent<Collider>().isTrigger = true;
        Destroy(gameObject, puddleDuration); // Destroy after duration
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Cart")) return;

        Cart hitCart = other.GetComponent<Cart>();
        if (hitCart != null && !hitCart.IsSpinningOut())
        {
            hitCart.SpinOut(spinOutDuration);
        }

        // destroy puddle after trigger 
        Destroy(gameObject, 1f);
    }

    // first thought was slowdown but nah
    // private void OnTriggerExit(Collider other)
    // {
    //     Cart hitCart = other.GetComponent<Cart>();
    //     if (hitCart != null)
    //     {

    //     }
    // }
}