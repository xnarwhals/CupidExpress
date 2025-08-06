using UnityEngine;
using UnityEngine.Splines;

public class RollingMelon : MonoBehaviour
{
    private float AOERadius;
    private float melonTrackSpeed;
    private Cart leaderCart;
    private Cart userCart;
    private GameObject aoeVisualInstance;
    private SplineContainer raceTrack;
    private SplineAnimate splineAnimator;

    private bool homingToLeader = false;
    private float homingSpeed = 30f;
    private float directExplodeDistance = 2f;

    private void Awake()
    {
        splineAnimator = GetComponent<SplineAnimate>();
        raceTrack = GameManager.Instance.raceTrack;
        if (splineAnimator == null)
        {
            Debug.LogError("RollingMelon requires a SplineAnimate component to function properly.");
        }
    }

    public void Initialize(float AOERadius, float melonTrackSpeed, Cart leader, Cart user, GameObject aoeVisualPrefab)
    {
        aoeVisualInstance = aoeVisualPrefab;
        this.AOERadius = AOERadius;
        this.melonTrackSpeed = melonTrackSpeed;
        leaderCart = leader;
        userCart = user;

        if (raceTrack != null && leaderCart != null)
        {
            if (splineAnimator != null)
            {
                splineAnimator.NormalizedTime = user.GetSplineProgress();
            }
        }
        Debug.Log("Melon start progress: " + splineAnimator.NormalizedTime);
    }

    // Collider to swap to homing behavior
    private void OnTriggerEnter(Collider col)
    {
        Cart cartInCollider = col.GetComponent<Cart>();

        if (cartInCollider == leaderCart)
        {
            homingToLeader = true;
        }
    } 

    private void Update()
    {
        if (leaderCart == null || raceTrack == null) return;

        if (homingToLeader)
        {
            splineAnimator.Pause();
            Vector3 leaderPos = leaderCart.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, leaderPos, homingSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, leaderPos) < directExplodeDistance)
            {
                Explode();
            }
        }
    }


    private void Start()
    {
        splineAnimator.Container = raceTrack;
        splineAnimator.MaxSpeed = melonTrackSpeed;
        splineAnimator.Play();
    }


    private void Explode()
    {
        leaderCart.SpinOut(7f);
        aoeVisualInstance = Instantiate(aoeVisualInstance, leaderCart.transform.position, Quaternion.identity);
        aoeVisualInstance.transform.localScale = Vector3.one * AOERadius * 2f;

        Debug.Log("Boom!");
        // AOE: affect all carts in radius
        Collider[] hits = Physics.OverlapSphere(leaderCart.transform.position, AOERadius);
        foreach (var hit in hits)
        {
            Cart cart = hit.GetComponent<Cart>();
            if (cart != null)
            {
                cart.SpinOut(3f); // Apply long spin out effect
            }
        }

        Destroy(gameObject);
        Destroy(aoeVisualInstance, 2f);
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (splineAnimator != null)
        {
            // Draw a label at the melon position showing the spline progress
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Melon Spline Progress: {splineAnimator.NormalizedTime:F3}"
            );
        }

    }
    #endif


}
