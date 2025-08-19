using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Profiling;

public class SimpleAIDriver : MonoBehaviour
{
    public float speed = 20f;
    private float baseSpeed;

    // for state effects
    private Vector3 originalModelScale;
    private Quaternion originalModelRotation;
    public float shockScaleMultiplier = 0.6f; // 60% of original

    // Spline and state management
    private SplineContainer spline;
    private static SplineContainer sharedSpline;
    private SimpleAIStateController stateController;
    public AIDriverState CurrentState => stateController.currentState;

    private GameManager gm;
    private Rigidbody rb;

    [Header("References")]
    public Transform modelTransform;
    public Cart ThisCart;
    public Cart player;

    [Header("Offset Settings")]
    public Vector3 localOffset = Vector3.zero;
    private float splineLength;
    [SerializeField] private float progress = 0f; // modify based on track 

    // Start transition 
    private float transitionMoveSpeed = 12f;   // m/s toward target
    private float transitionTurnSpeed = 360f;  // deg/s
    private float arriveDistance = 0.1f;       // how close counts as arrived
    private float arriveAngleDeg = 3f;  
    private bool transitioningToSpline = true;

    [Header("Rubberbanding Settings")]
    [SerializeField] private float speedUpWhenPlayerFirst = 1.5f; // player leads → all CPUs speed up
    [SerializeField] private float slowWhenCPUFirst = 0.8f;  
    [SerializeField] private float rubberbandCheckInterval = 0.5f; // how often to check for rubberbanding
    private float lastRubberBandCheckTime = 0f;
    private float rubberBandModifier = 1f;
    private bool shrinkOnShock = false;


    private void OnEnable()
    {
        if (stateController != null)
            stateController.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (stateController != null)
            stateController.OnStateChanged -= HandleStateChanged;
    }

    private void Awake()
    {
        ThisCart = GetComponent<Cart>();
        rb = GetComponent<Rigidbody>();

        if (spline == null)
        {
            if (sharedSpline == null)
            {
                sharedSpline = FindObjectOfType<SplineContainer>();
            }
            spline = sharedSpline;
        }

        stateController = gameObject.AddComponent<SimpleAIStateController>();
        stateController.Initialize(this);

        if (modelTransform != null)
        {
            originalModelScale = modelTransform.localScale;
            originalModelRotation = modelTransform.localRotation;
        }

    }

    private void Start()
    {
        gm = GameManager.Instance;
        player = gm.GetPlayerCart();

        if (spline == null) Debug.LogError("SplineContainer component not found on car!");
        if (modelTransform == null) Debug.LogError("Model Transform not assigned on SimpleAIDriver!");

        splineLength = spline.Spline.GetLength();
        baseSpeed = speed;

        // Find nearest progress on spline from current world position:
        float3 nearest;
        float t;
        SplineUtility.GetNearestPoint(spline.Spline, spline.transform.InverseTransformPoint(transform.position), out nearest, out t);
        progress = Mathf.Repeat(t, 1f);

        transitioningToSpline = true; // start by easing in, not snapping

    }

    private void Update()
    {

        if (spline == null || gm == null || !gm.AICanMoveState()) return; // idk you can be more fancy for finish or smth

        // Handle scale and rotation lerping based on state
        if (modelTransform != null)
        {
            if (stateController.currentState == AIDriverState.SpinningOut && shrinkOnShock)
            {
                Vector3 targetScale = originalModelScale * shockScaleMultiplier;
                modelTransform.localScale = Vector3.Lerp(modelTransform.localScale, targetScale, Time.deltaTime * 8f);
            }
            else if (stateController.currentState == AIDriverState.Recovering)
            {
                modelTransform.localScale = Vector3.Lerp(modelTransform.localScale, originalModelScale, Time.deltaTime * 8f);
                modelTransform.localRotation = Quaternion.Lerp(modelTransform.localRotation, originalModelRotation, Time.deltaTime * 8f);
            }
        }

        float3 pos = SplineUtility.EvaluatePosition(spline.Spline, progress);
        float3 tangent = SplineUtility.EvaluateTangent(spline.Spline, progress);

        Vector3 worldPos = spline.transform.TransformPoint(pos);
        Vector3 worldTangent = spline.transform.TransformDirection(tangent);
        Vector3 worldNormal = spline.transform.up; // or EvaluateUpVector

        Quaternion targetRot = Quaternion.LookRotation(worldTangent, worldNormal);
        Vector3 offsetPos = worldPos + targetRot * localOffset;

        if (transitioningToSpline)
        {
            // Move and rotate TOWARD the target pose
            transform.position = Vector3.MoveTowards(
                transform.position,
                offsetPos,
                transitionMoveSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                transitionTurnSpeed * Time.deltaTime
            );

            // Arrived?
            bool closeEnough =
                Vector3.Distance(transform.position, offsetPos) <= arriveDistance &&
                Quaternion.Angle(transform.rotation, targetRot) <= arriveAngleDeg;

            if (closeEnough)
            {
                transitioningToSpline = false;
            }

            // While transitioning, do NOT advance progress (target stays stable)
            return;
        }

        // Normal spline-following once we've arrived
        if (stateController.currentState != AIDriverState.SpinningOut)
        {
            progress = (progress + (speed * Time.deltaTime / splineLength)) % 1f;

            // Snap to the updated target (now that we're on-track)
            transform.position = offsetPos;
            transform.rotation = targetRot;

            if (Time.time - lastRubberBandCheckTime >= rubberbandCheckInterval)
            {
                RubberBand();
                lastRubberBandCheckTime = Time.time;
            }
        }
    }


    public void SetLaneOffset(float offsetAmount)
    {
        if (modelTransform == null) return;

        localOffset.x = offsetAmount;
    }

    private void HandleStateChanged(AIDriverState oldState, AIDriverState newState)
    {
        switch (newState)
        {
            case AIDriverState.Normal:
                speed = baseSpeed;
                break;
            case AIDriverState.Boosting:
                break;
            case AIDriverState.SpinningOut:
                speed = 0;
                break;
            case AIDriverState.Recovering:
                speed = baseSpeed * 0.25f; // Slow down during recovery (psuedo acceleration)
                break;
            case AIDriverState.Rubberbanding:
                speed = baseSpeed * rubberBandModifier;
                break;
            case AIDriverState.Stunned:
                // Handle stunned logic
                break;
            default:
                break;
        }

        if (newState == AIDriverState.SpinningOut) shrinkOnShock = false; 
    }

    // If player cart is in first speed up CPU to be around player then return to normal 
    // If THIS cpu is in first, slow down by a a factor
    private void RubberBand()
    {
        if (gm == null || player == null || ThisCart == null) return;
        if (stateController == null) return;

        var s = stateController.currentState;
        // never rubberband while incapacitated
        if (s == AIDriverState.SpinningOut || s == AIDriverState.Stunned) return;

        int playerPos = gm.GetCartPosition(player);
        int myPos = gm.GetCartPosition(ThisCart);

        // Player leads: speed up this CPU (if not already 1st)
          if (playerPos == 1 && myPos != 1)
        {
            // Player leads -> apply full speed-up
            rubberBandModifier = speedUpWhenPlayerFirst;
            speed = baseSpeed * rubberBandModifier;

            if (stateController.currentState != AIDriverState.Rubberbanding)
                stateController.TryChangeState(AIDriverState.Rubberbanding);
        }
        else if (myPos == 1 && playerPos != 1)
        {
            // This CPU leads -> apply fixed slow down
            rubberBandModifier = slowWhenCPUFirst;
            speed = baseSpeed * rubberBandModifier;

            if (stateController.currentState == AIDriverState.Rubberbanding)
                stateController.TryChangeState(AIDriverState.Normal);
        }
        else
        {
            // No rubberbanding
            rubberBandModifier = 1f;
            speed = baseSpeed;

            if (stateController.currentState == AIDriverState.Rubberbanding)
                stateController.TryChangeState(AIDriverState.Normal);
        }
    }

    public bool ShouldRubberBand()
    {
        int playerPos = gm.GetCartPosition(player);
        int myPos = gm.GetCartPosition(ThisCart);
        return (playerPos == 1 && myPos != 1) || (myPos == 1 && playerPos != 1);
    }

    #region Public Methods
    public float GetSplineProgress()
    {
        return progress;
    }

    public Rigidbody GetRB()
    {   
        if (rb != null) return rb;
        return null;
    }

    public void SpinOut(float duration)
    {
        shrinkOnShock = false;
        stateController.TryChangeState(AIDriverState.SpinningOut, duration);

    }

    public void StartBoost(float duration, float speedMultiplier)
    {
        if (!stateController.CanUseItems()) return;
        stateController.TryChangeState(AIDriverState.Boosting, duration);
        speed = baseSpeed * speedMultiplier;
    }

    public void Shock(float duration)
    {
        if (!stateController.CanUseItems()) return;
        shrinkOnShock = true; 
        stateController.TryChangeState(AIDriverState.SpinningOut, duration);
        // Scale lerp is now handled in Update based on state
    }

    public Vector3 GetAimPoint()
    {
        return ThisCart.col.bounds.center;
    }

    public Vector3 GetPredictivePosition(float speed, Vector3 targetVelocity)
    {
        // Predict where the target will be when the projectile arrives
        Vector3 aimPoint = GetAimPoint();
        Vector3 toTarget = aimPoint - transform.position;
        float distance = toTarget.magnitude;
        float travelTime = distance / speed;
        return aimPoint + targetVelocity * travelTime;
    }

    public void SetBaseSpeed(float newSpeed)
    {
        speed = newSpeed;
    }


    #endregion

    private void OnDrawGizmos()
    {
        // show leaderboard position 
        if (spline == null || modelTransform == null) return;
        int pos = (int)GameManager.Instance?.GetCartPosition(ThisCart);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
        transform.position + Vector3.up * 2.5f,
        $"Position: {pos}\n" +
        $"Lap: {GameManager.Instance.GetCartLap(ThisCart)}\n" +
        $"Checkpoint: {GameManager.Instance.GetCartCheckpoint(ThisCart)}\n" +
        $"Spline Progress: {GetSplineProgress():P2}\n"

    );
#endif

    }

}
