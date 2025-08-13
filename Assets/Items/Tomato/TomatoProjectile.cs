using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class TomatoProjectile : MonoBehaviour
{
    private Tomato tomato;
    private Rigidbody rb;
    private Cart throwingCart;
    private bool hasHit = false;

    public void Initialize(Tomato itemData, Cart cart, Vector3 throwDirection)
    {
        tomato = itemData;
        throwingCart = cart;
        rb = GetComponent<Rigidbody>(); // tomato rb

        Vector3 cartVelocity = Vector3.zero;

        if (throwingCart != null)
        {
            Rigidbody cartRb = throwingCart.GetComponent<Rigidbody>();
            cartVelocity = cartRb != null ? cartRb.velocity : Vector3.zero;
        }

        LaunchTomato(throwDirection, cartVelocity);
        Destroy(gameObject, 10f); // Destroy after 10 seconds if freaky behavior occurs
    }

    private void LaunchTomato(Vector3 throwDirection, Vector3 cartVelocity)
    {
  
        Vector3 velocity = throwDirection * tomato.throwForce;
        velocity.y += Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * tomato.arcHeight); // Adjust for arc height

        rb.velocity = velocity + cartVelocity;
        rb.angularVelocity = Random.insideUnitSphere * 5f; // Add some spin
    }

    private void OnCollisionEnter(Collision other)
    {
        if (hasHit) return; // Prevent multiple hits
        // Debug.Log($"Tomato hit {other.gameObject.name}");

        Cart hitCart = other.gameObject.GetComponent<Cart>(); 
        if (hitCart != null && (throwingCart == null || hitCart != throwingCart)) // testing 
        {
            HitCart(hitCart);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Track"))
        {
            HitGround();
        }
    }

    private void HitCart(Cart hitCart)
    {
        hasHit = true;
        Debug.Log($"Tomato hit {hitCart.CartName}!");

        if (hitCart.CartID == 0) hitCart.StartKetchupEffect(); 

        hitCart.SpinOut(tomato.directHitSpinOutDuration);

        AudioManager.Instance.PlayTomatoHit();

        Destroy(gameObject);
    }

    private void HitGround()
    {
        hasHit = true;
        CreateKetchupSplat(transform.position);
        // Debug.Log("Tomato on floor alert");

        AudioManager.Instance.PlayTomatoHit();

        Destroy(gameObject);
    }

    private void CreateKetchupSplat(Vector3 splatPosition)
    {

        splatPosition += Vector3.up * 0.9f;

        if (Physics.Raycast(splatPosition, Vector3.down, out RaycastHit hit, 10f))
        {
            splatPosition = hit.point; // ensure its on the floor
        }

        GameObject splat = Instantiate(tomato.ketchupSplatPrefab, splatPosition, Quaternion.identity);
        splat.transform.localScale = new Vector3(tomato.splatRadius * 2, 0.1f, tomato.splatRadius * 2);

        KetchupPuddle puddle = splat.GetComponent<KetchupPuddle>();
        puddle.Initialize(tomato.enterKetchupSpinOutDuration, tomato.splatDuration);
    }
        
    
}
