using UnityEngine;

public class RaceUI : MonoBehaviour
{
    public GameObject pauseMenu;
    private void Start()
    {
        GameManager.Instance.OnRaceStateChanged += OnRaceStateChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnRaceStateChanged -= OnRaceStateChanged;
    }

    private void OnRaceStateChanged(GameManager.RaceState newState)
    {
        if (newState == GameManager.RaceState.Paused)
        {
            showPauseMenu();
        }
        else if (newState == GameManager.RaceState.Racing)
        {
            hidePauseMenu();
        }
    }

    public void showPauseMenu()
    {
        pauseMenu.SetActive(true);
    }

    public void hidePauseMenu()
    {
        pauseMenu.SetActive(false);
    }



}
