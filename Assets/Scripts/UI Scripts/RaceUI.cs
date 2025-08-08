using UnityEngine;

public class RaceUI : MonoBehaviour
{
    public GameObject pauseMenu;

    public void showPauseMenu()
    {
        pauseMenu.SetActive(true);
    }

    public void hidePauseMenu()
    {
        pauseMenu.SetActive(false);
    }


}
