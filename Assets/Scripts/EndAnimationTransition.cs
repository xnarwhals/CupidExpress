using UnityEngine.Splines;
using UnityEngine;

public class EndAnimationTransition : MonoBehaviour
{
    private SplineAnimate splineAnimator;
    public GameObject cameraEndPos;
    public Camera mainCamera;

    private void OnEnable()
    {
        GameManager.Instance.OnRaceStateChanged += OnRaceStateChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnRaceStateChanged -= OnRaceStateChanged;
    }

    private void Awake()
    {
        splineAnimator = GetComponent<SplineAnimate>();
        if (splineAnimator == null)
        {
            Debug.LogError("SplineAnimate component not found on this Transform.");
        }
    }

    private void OnRaceStateChanged(GameManager.RaceState newState)
    {
        if (newState == GameManager.RaceState.Finished)
        {
            if (splineAnimator != null && cameraEndPos != null && mainCamera != null)
            {
                // Set the spline animator to the end position
                splineAnimator.enabled = true;
                splineAnimator.Play();

                // Move the camera to the end position
                mainCamera.transform.position = cameraEndPos.transform.position;
                mainCamera.transform.rotation = cameraEndPos.transform.rotation;
            }
            else
            {
                Debug.LogWarning("SplineAnimator or Camera End Position is not set.");
            }
        }
    }

}
