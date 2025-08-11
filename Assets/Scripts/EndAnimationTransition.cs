using UnityEngine.Splines;
using UnityEngine;
using Cinemachine;

public class EndAnimationTransition : MonoBehaviour
{
    private SplineAnimate splineAnimator;
    private SplineContainer spline;
    public CinemachineVirtualCamera vCam;
    private BallKart ballKart;
    [SerializeField] private float splineProgressStart = 0.3f; // change based on spline


    private void OnDisable()
    {
        GameManager.Instance.OnRaceFinished -= OnRaceFinished;
    }

    private void Awake()
    {
        ballKart = FindFirstObjectByType<BallKart>();
    }

    private void Start()
    {
        spline = GameManager.Instance.raceTrack;

        GameManager.Instance.OnRaceFinished += OnRaceFinished;
        if (spline == null)
        {
            Debug.LogError("SplineContainer not found in GameManager!");
            return;
        }
        if (vCam == null)
        {
            Debug.LogError("CinemachineVirtualCamera not assigned!");
            return;
        }

    } 

    // 0 10 -30
    private void OnRaceFinished()
    {
        // animate this trannsition 
        var transposer = vCam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_FollowOffset = new Vector3(0, 7.5f, 22); // Adjust camera offset for end animation
        }

        splineAnimator = gameObject.AddComponent<SplineAnimate>();
        splineAnimator.PlayOnAwake = false;
        splineAnimator.Container = spline;
        splineAnimator.AnimationMethod = SplineAnimate.Method.Speed;
        splineAnimator.MaxSpeed = 30f;
        splineAnimator.NormalizedTime = splineProgressStart;
        splineAnimator.Play();

    }

}
