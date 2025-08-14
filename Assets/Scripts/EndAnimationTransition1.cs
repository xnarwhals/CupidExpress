using UnityEngine.Splines;
using UnityEngine;
using Cinemachine;

public class EndAnimationTransition : MonoBehaviour
{
    private SplineAnimate splineAnimator;
    private SplineContainer spline;
    public CinemachineVirtualCamera vCam;
    [SerializeField] private Animator ray;
    [SerializeField] private float splineProgressStart = 0.3f; // change based on spline


    private void OnDisable()
    {
        GameManager.Instance.OnRaceFinished -= OnRaceFinished;
    }

    private void Awake()
    {
        ray = GetComponentInChildren<Animator>();
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
        if (ray == null)
        {
            Debug.LogError("Ray Animator not found");
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

        ray.SetTrigger("EndAnimation");

        splineAnimator = gameObject.AddComponent<SplineAnimate>();
        splineAnimator.PlayOnAwake = false;
        splineAnimator.Container = spline;
        splineAnimator.AnimationMethod = SplineAnimate.Method.Speed;
        splineAnimator.MaxSpeed = 30f;
        splineAnimator.NormalizedTime = splineProgressStart;
        splineAnimator.Play();

    }

}
