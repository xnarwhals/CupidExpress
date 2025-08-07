using TMPro;
using UnityEngine;

public class EndUI : MonoBehaviour
{
    public TMP_Text leaderboardText;
    public GameObject leaderboardPanel;
    public GameObject[] other;
    // position 1st, 2nd, etc. + cart name + new line
    private string leaderboardFormat = "{0}. {1}\n";

    private void Awake()
    {
        if (leaderboardText == null || leaderboardPanel == null)
        {
            Debug.LogError("Leaderboard text or panel is missing on EndUI.");
        }
    }

    private void Update()
    {
        if (GameManager.Instance.GetCurrentRaceState() == GameManager.RaceState.Finished)
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
}
