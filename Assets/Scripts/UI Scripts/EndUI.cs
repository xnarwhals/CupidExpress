using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndUI : MonoBehaviour
{
    public TMP_Text leaderboardText;
    public GameObject leaderboardPanel;
    public GameObject secondLeaderboardPanel;
    public GameObject temp;
    public GameObject backButton;
    public GameObject[] other;
    // position 1st, 2nd, etc. + cart name + new line
    private string leaderboardFormat = "{0}. {1}\n";

    private void Awake()
    {
        if (leaderboardText == null || leaderboardPanel == null || temp == null)
        {
            Debug.LogError("Leaderboard text or panel is missing on EndUI.");
        }
    }

    private void Update()
    {
        bool finished = GameManager.Instance != null && GameManager.Instance.GetCurrentRaceState() == GameManager.RaceState.Finished;
        if (finished)
        {
            if (!leaderboardPanel.activeSelf)
            {
                ShowLeaderboard();
            }
        }
        else
        {
            if (leaderboardPanel.activeSelf)
            {
                HideLeaderboard();
            }
        }
    }

    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        secondLeaderboardPanel.SetActive(true);
        backButton.SetActive(true);
        temp.SetActive(true);
        GenerateLeaderboard();

        if (other != null)
        {
            foreach (var go in other)
            {
                if (go != null) go.SetActive(false);
            }
        }
    }

    public void HideLeaderboard()
    {
        leaderboardPanel.SetActive(false);
        secondLeaderboardPanel.SetActive(false);
        temp.SetActive(false);
        backButton.SetActive(false);
        if (other != null)
        {
            foreach (var go in other)
            {
                if (go != null) go.SetActive(true);
            }
        }
    }

    private void GenerateLeaderboard()
    {
        var leaderboard = GameManager.Instance.GetCartLeaderboard();
        int count = 1;
        foreach (Cart cart in leaderboard)
        {
            string cartName = cart != null ? cart.CartName : "Unknown Cart";
            leaderboardText.text += string.Format(leaderboardFormat, count, cartName);
            count++;
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
    
    public void NextRace()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneLoader.Instance.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("No next scene available, loading main menu instead.");
            SceneLoader.Instance.LoadScene(0); // Load main menu if no next scene
        }
    }
}
