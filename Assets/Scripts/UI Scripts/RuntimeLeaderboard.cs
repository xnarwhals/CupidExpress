using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeLeaderboard : MonoBehaviour
{
    public TMPro.TMP_Text leaderboardText;
    [Tooltip("Optional: override which scene/track leaderboard to show. Leave empty to use active scene.")]
    public string sceneNameOverride;

    private GameManager gm;
    private Coroutine waitForGM;

    private void Awake()
    {
        if (leaderboardText == null)
            Debug.LogError("[RuntimeLeaderboard] leaderboardText not assigned.");
    }

    private void OnEnable()
    {
        gm = GameManager.Instance;
        if (gm != null)
            gm.OnRaceFinished += UpdateLeaderboard;
        else
        {
            if (waitForGM != null) StopCoroutine(waitForGM);
            waitForGM = StartCoroutine(WaitForGameManager());
        }

        UpdateLeaderboard();
    }

    private void OnDisable()
    {
        if (waitForGM != null) { StopCoroutine(waitForGM); waitForGM = null; }
        if (gm != null) gm.OnRaceFinished -= UpdateLeaderboard;
    }

    private IEnumerator WaitForGameManager()
    {
        float timeout = 5f;
        float t = 0f;
        while (GameManager.Instance == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        gm = GameManager.Instance;
        if (gm != null)
            gm.OnRaceFinished += UpdateLeaderboard;
        else
            Debug.LogWarning("[RuntimeLeaderboard] GameManager not found to subscribe OnRaceFinished.");
        waitForGM = null;
    }

    public void UpdateLeaderboard()
    {
        if (leaderboardText == null) return;

        string keyScene = string.IsNullOrEmpty(sceneNameOverride) ? SceneManager.GetActiveScene().name : sceneNameOverride;
        var list = LocalLeaderboard.GetLeaderboard(keyScene);

        if (list == null || list.Count == 0)
        {
            leaderboardText.text = "No times recorded yet.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        int index = 1;
        foreach (var entry in list)
        {
            string name = string.IsNullOrEmpty(entry.name) ? "Player" : entry.name;
            sb.AppendFormat("{0}. {1}: {2:F2} seconds\n", index, name, entry.time);
            index++;
        }

        leaderboardText.text = sb.ToString();
    }
}