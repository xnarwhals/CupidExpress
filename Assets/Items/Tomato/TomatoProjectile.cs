using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class TomatoProjectile : MonoBehaviour
{
    private Tomato tomato;
    private Rigidbody rb;
    private Cart throwingCart;
    private Cart targetCart;
    private bool hasHit = false;

    // homing
    private bool isHoming = false;
    private Vector3 homingThrowerVelocity = Vector3.zero;
    [SerializeField] private float homingResponsiveness = 10f;

    public void Initialize(Tomato itemData, Cart cart, Vector3 throwDirection)
    {
        tomato = itemData;
        throwingCart = cart;
        rb = GetComponent<Rigidbody>();

        Vector3 cartVelocity = Vector3.zero;
        if (throwingCart != null)
        {
            Rigidbody cartRb = throwingCart.GetRB();
            cartVelocity = cartRb != null ? cartRb.velocity : Vector3.zero;
        }

        homingThrowerVelocity = cartVelocity;

        targetCart = FindClosestCart(transform.position);

        if (targetCart != null)
        {
            Debug.Log($"Locking onto {targetCart.CartName}");
            LaunchTomatoDirect(cartVelocity);
            isHoming = true;
        }
        else
        {
            Debug.Log("No valid target found, using normal throw");
            LaunchTomato(throwDirection, cartVelocity);
            isHoming = false;
        }

        Destroy(gameObject, 15f);
    }

    private void FixedUpdate()
    {
        if (!isHoming || targetCart == null || hasHit) return;
        float speed = tomato.throwForce;
        Vector3 targetVelocity = Vector3.zero;
        Rigidbody targetRb = targetCart.GetRB();
        if (targetRb != null && targetRb.velocity.sqrMagnitude > 0.01f)
        {
            targetVelocity = targetRb.velocity;
        }
        else
        {
            var ai = targetCart.AIDriver;
            if (ai != null)
                targetVelocity = ai.transform.forward * ai.speed;
            else
                targetVelocity = targetCart.transform.forward * 8f; // fallback assumed speed
        }

        // aim point: prefer collider center
        Vector3 aimPoint = targetCart.col != null ? targetCart.col.bounds.center : targetCart.transform.position;

        float distance = Vector3.Distance(transform.position, aimPoint);
        float travel = distance / Mathf.Max(0.0001f, speed);
        Vector3 predicted = aimPoint + targetVelocity * travel;

        Vector3 desiredVel = (predicted - transform.position).normalized * speed + homingThrowerVelocity;

        // smoothly steer toward desired velocity to avoid jitter; responsiveness tuned by homingResponsiveness
        rb.velocity = Vector3.Lerp(rb.velocity, desiredVel, Mathf.Clamp01(homingResponsiveness * Time.fixedDeltaTime));
}

    private void LaunchTomatoDirect(Vector3 cartVelocity)
    {
        if (targetCart == null)
        {
            Debug.LogError("Target cart is null in direct launch!");
            return;
        }

        float speed = tomato.throwForce * 2f; // dont run away from me >:(

        // Player case use physics to predict position
        Rigidbody targetRb = targetCart.GetRB();
        Vector3 targetVelocity = targetRb != null ? targetRb.velocity : Vector3.zero;

        // AI case
        Vector3 aimPoint;
        var aiDriver = targetCart.AIDriver;

        if (aiDriver != null)
        {
            aimPoint = aiDriver.GetPredictivePosition(speed, transform.position);
        }
        else
        {
            aimPoint = targetCart.col.bounds.center;
            float travel = Vector3.Distance(transform.position, aimPoint) / Mathf.Max(speed, 0.1f);
            aimPoint += targetRb.velocity * travel; // Predictive aim
        }


        // predict where the target will be when the tomato arrives
        Vector3 toTarget = aimPoint - transform.position;
        float distance = toTarget.magnitude;
        float travelTime = distance / speed;
        Vector3 predictedPos = aimPoint + targetVelocity * travelTime;

        Vector3 direction = (predictedPos - transform.position).normalized;


        rb.useGravity = false;
        rb.velocity = direction * speed + cartVelocity;
        rb.angularVelocity = Random.insideUnitSphere * 5f; // Add some spin
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
        
        HitGround();
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

    private Cart FindClosestCart(Vector3 origin)
    {
        var all = GameManager.Instance.AllCarts;
        float maxDistSqr = tomato.lockOnDistance * tomato.lockOnDistance;
        Cart closest = null;
        float best = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            var cart = all[i];
            if (cart == null || cart == throwingCart) continue;
            float d = (cart.transform.position - origin).sqrMagnitude;
            if (d <= maxDistSqr && d < best)
            {
                best = d;
                closest = cart;
            }
        }

        return closest;
    }
        
}