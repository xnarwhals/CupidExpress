using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

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
    private SimpleAIStateController stateController;
    public AIDriverState CurrentState => stateController.currentState;
    public Cart ThisCart => GetComponent<Cart>();

    [Header("References")]
    public Transform modelTransform;

    [Header("Offset Settings")]
    public Vector3 localOffset = Vector3.zero;

    private float splineLength;
    private float progress = 0f; // modify 


    private void OnEnable()
    {
        stateController.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        stateController.OnStateChanged -= HandleStateChanged;
    }

    private void Awake()
    {
        spline = FindObjectOfType<SplineContainer>();
        stateController = gameObject.AddComponent<SimpleAIStateController>();
        stateController.Initialize(this);

        if (ThisCart == null) Debug.LogError("Cart component not found on SimpleAIDriver!");

        if (modelTransform != null)
        {
            originalModelScale = modelTransform.localScale;
            originalModelRotation = modelTransform.localRotation;
        }
    }

    private void Start()
    {
        if (spline == null) Debug.LogError("SplineContainer component not found on car!");
        if (modelTransform == null) Debug.LogError("Model Transform not assigned on SimpleAIDriver!");

        splineLength = spline.Spline.GetLength();
        baseSpeed = speed;
    }

    private void Update()
    {
        if (spline == null || !GameManager.Instance.AICanMoveState()) return; // idk you can be more fancy for finish or smth

        // Handle scale and rotation lerping based on state
        if (modelTransform != null)
        {
            if (stateController.currentState == AIDriverState.SpinningOut)
            {
                Vector3 targetScale = originalModelScale * shockScaleMultiplier;
                modelTransform.localScale = Vector3.Lerp(modelTransform.localScale, targetScale, Time.deltaTime * 8f);
                // (Spin rotation handled in state controller)
            }
            else if (stateController.currentState == AIDriverState.Recovering)
            {
                modelTransform.localScale = Vector3.Lerp(modelTransform.localScale, originalModelScale, Time.deltaTime * 8f);
                modelTransform.localRotation = Quaternion.Lerp(modelTransform.localRotation, originalModelRotation, Time.deltaTime * 8f);
            }
        }

        // Only move along spline if not spinning out
        if (stateController.currentState != AIDriverState.SpinningOut)
        {
            progress = (progress + (speed * Time.deltaTime / splineLength)) % 1f;

            // Get position and tangent on spline
            float3 pos = SplineUtility.EvaluatePosition(spline.Spline, progress);
            float3 tangent = SplineUtility.EvaluateTangent(spline.Spline, progress);

            // Calculate world offset
            Vector3 worldPos = spline.transform.TransformPoint(pos);
            Vector3 worldTangent = spline.transform.TransformDirection(tangent);
            Vector3 worldNormal = spline.transform.up; // or use EvaluateUpVector if available

            // Set position and rotation
            Quaternion rot = Quaternion.LookRotation(worldTangent, worldNormal);
            Vector3 offsetPos = worldPos + rot * localOffset;

            transform.position = offsetPos;
            transform.rotation = rot;
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
                speed = baseSpeed * 0.1f; // Slow down during recovery (psuedo acceleration)
                break;
            case AIDriverState.Stunned:
                // Handle stunned logic
                break;
            default:
                break;
        }
    }

    #region Public Methods
    public float GetSplineProgress()
    {
        return progress;
    }

    public void SpinOut(float duration)
    {
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
        stateController.TryChangeState(AIDriverState.SpinningOut, duration);
        // Scale lerp is now handled in Update based on state
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
        $"Spline Progress: {GetSplineProgress():P2}"
    );
#endif

    }

}
