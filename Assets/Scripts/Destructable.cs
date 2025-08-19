using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Destructable : MonoBehaviour
{
    [Header("Trigger Conditions")]
    [Tooltip("Minimum normal-impact speed needed to react.")]
    public float minTriggerSpeed = 5f;
    [Tooltip("Speed at/above which you get max impulse.")]
    public float maxTriggerSpeed = 25f;

    [Header("Impulse Settings")]
    [Tooltip("Impulse when barely triggered.")]
    public float minImpulse = 5f;
    [Tooltip("Impulse when hit at or above maxTriggerSpeed.")]
    public float maxImpulse = 25f;
    [Tooltip("Upward bias added to the launch direction.")]
    public float upBias = 0.2f;
    [Tooltip("Random cone spread (degrees) around the base launch direction.")]
    public float randomConeDegrees = 35f;
    [Tooltip("Extra random spin based on impulse.")]
    public float torqueScale = 0.3f;

    [Header("Misc")]
    [Tooltip("Prevent multiple launches from the same bump.")]
    public bool oneShot = true;
    [Tooltip("Only react to these layers (set your Player/Kart layer).")]
    public LayerMask reactToLayers = ~0; // default: everything

    private Rigidbody rb;
    private bool triggered;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float respawnTimer;
    [SerializeField] private float respawnAfterTriggered = 5f; 



    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) Debug.LogError("Destructable requires a Rigidbody.");
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (!triggered) return;
        if (respawnTimer > 0f)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                // Debug.Log("Respawning destructable object");
                Respawn();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (oneShot && triggered) return;

        // Only react to things on allowed layers AND that have a Rigidbody
        if (((1 << collision.gameObject.layer) & reactToLayers) == 0)
            return;
        if (collision.rigidbody == null)
            return;

        // Use the first contact for normal/point (good enough for most cases)
        ContactPoint contact = collision.GetContact(0);

        // Impact speed ALONG THE NORMAL (how 'hard' the bump is)
        // Positive when the other object is moving into us along the normal.
        float normalSpeed = Mathf.Max(0f, Vector3.Dot(collision.relativeVelocity, -contact.normal));

        triggered = true;
        AudioManager.Instance.PlayHitCone(); 
        respawnTimer = respawnAfterTriggered;

        // Map speed -> impulse
        float t = Mathf.InverseLerp(minTriggerSpeed, maxTriggerSpeed, normalSpeed);
        float impulse = Mathf.Lerp(minImpulse, maxImpulse, t);

        // Base launch direction = contact normal + small upward bias
        Vector3 baseDir = (contact.normal + Vector3.up * upBias).normalized;

        // Randomize within a cone around baseDir
        Vector3 dir = RandomDirectionInCone(baseDir, randomConeDegrees);

        // Apply impulse at the contact for nicer reactions (can add spin)
        rb.AddForceAtPosition(dir * impulse, contact.point, ForceMode.Impulse);

        // Add some random torque so it feels less stiff
        Vector3 randomAxis = Random.onUnitSphere;
        rb.AddTorque(randomAxis * (impulse * torqueScale), ForceMode.Impulse);
    }

    // Returns a direction within a cone around 'axis' (in degrees)
    private static Vector3 RandomDirectionInCone(Vector3 axis, float coneAngleDeg)
    {
        axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
        float angleRad = coneAngleDeg * Mathf.Deg2Rad;

        // Random rotation around axis:
        Quaternion twist = Quaternion.AngleAxis(Random.Range(0f, 360f), axis);
        // Random tilt away from axis:
        float u = Random.value;                          // 0..1
        float cosTheta = Mathf.Lerp(1f, Mathf.Cos(angleRad), u);
        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);

        // Build an orthonormal basis around axis
        Vector3 ortho = Vector3.Cross(axis, Vector3.up);
        if (ortho.sqrMagnitude < 1e-4f) ortho = Vector3.Cross(axis, Vector3.right);
        ortho.Normalize();
        Vector3 ortho2 = Vector3.Cross(axis, ortho);

        Vector3 localDir = cosTheta * axis + sinTheta * (Mathf.Cos(0f) * ortho + Mathf.Sin(0f) * ortho2);
        return (twist * localDir).normalized;
    }

    private void Respawn()
    {
        // Prevent immediate physics interaction while we snap back
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Reset transform to original pose
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // Clear state and restore RB settings
        triggered = false;
        respawnTimer = 0f;
        
        if (rb)
        {
            rb.isKinematic = false;
            rb.angularDrag = 0.05f; // Reset to default angular drag
        }
    }


}