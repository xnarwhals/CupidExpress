using UnityEngine;

public class LocalLeaderboard1 : MonoBehaviour
{
    public TMPro.TMP_Text leaderboardText;

    private void Awake()
    {
        if (leaderboardText == null)
        {
            Debug.LogError("Leaderboard Text is not assigned in LocalLeaderboard1.");
        }
    }
     private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRaceFinished += UpdateLeaderboard;
        else
            Debug.LogWarning("GameManager.Instance is null in RuntimeLeaderboard.OnEnable.");
        UpdateLeaderboard();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRaceFinished -= UpdateLeaderboard;
    }

    private void Start()
    {
        GameManager.Instance.OnRaceFinished += UpdateLeaderboard;
        UpdateLeaderboard();
    }

    public void UpdateLeaderboard()
    {
         if (leaderboardText == null) return;

        leaderboardText.text = "";

        if (LocalLeaderboard.BestTimes == null || LocalLeaderboard.BestTimes.Count == 0)
        {
            leaderboardText.text = "No times recorded yet.";
            return;
        }

        int count = Mathf.Min(LocalLeaderboard.BestTimes.Count, LocalLeaderboard.BestNames.Count);
        for (int i = 0; i < count; i++)
        {
            string name = string.IsNullOrEmpty(LocalLeaderboard.BestNames[i]) ? "Player" : LocalLeaderboard.BestNames[i];
            leaderboardText.text += $"{i + 1}. {name}: {LocalLeaderboard.BestTimes[i]:F2} seconds\n";
        }
    }


}
