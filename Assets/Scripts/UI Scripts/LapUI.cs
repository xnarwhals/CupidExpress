using UnityEngine;

using TMPro;


public class LapUI : MonoBehaviour
{
    [Header("UI Ref")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timerText;

    [Header("Cart Ref")]
    public Cart playerCart;

    [Header("UI settings")]
    public string lapTextFormat = "{0}/{1}";

    public Color normalColor = Color.white;
    public Color finalLapColor = Color.yellow;

    private GameManager gm;

    private void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            GameManager.Instance.OnCartLapCompleted += OnLapCompleted;
            GameManager.Instance.OnRaceStateChanged += OnRaceStateChanged;
        }

        UpdateLapUI();
    }

    private void Update()
    {
        if (timerText == null || playerCart == null || gm == null)
            return;

        var raceState = gm.GetCurrentRaceState();
        if (raceState == GameManager.RaceState.Racing)
        {
            float raceTime = gm.GetCartRaceTime(playerCart);
            timerText.text = raceTime.ToString("F2") + "s";
        }
    }

    private void OnDestroy()
    {
        if (gm != null)
        {
            GameManager.Instance.OnCartLapCompleted -= OnLapCompleted;
            GameManager.Instance.OnRaceStateChanged -= OnRaceStateChanged;
        }
    }

    private void OnLapCompleted(Cart cart, int newLap)
    {
        if (cart == playerCart) // bots dont have ui
        {
            UpdateLapUI();
        }
    }

    private void OnRaceStateChanged(GameManager.RaceState newState)
    {
        UpdateLapUI();
    }

    private void UpdateLapUI()
    {
        if (lapText == null || playerCart == null || GameManager.Instance == null) return;


        int curLap = gm.GetCartLap(playerCart);
        int totalLaps = gm.totalLaps;
        var raceState = gm.GetCurrentRaceState();

        switch (raceState)
        {
            case GameManager.RaceState.WaitingToStart:
                lapText.color = normalColor;
                break;

            case GameManager.RaceState.CountDown:
                lapText.color = normalColor;
                break;

            case GameManager.RaceState.Racing:
                int displayLap = Mathf.Min(curLap, totalLaps);
                lapText.text = string.Format(lapTextFormat, displayLap, totalLaps);
                lapText.color = (curLap == totalLaps) ? finalLapColor : normalColor;
                break;

            case GameManager.RaceState.Finished:
                lapText.text = "Finished!";
                lapText.color = finalLapColor;
                if (timerText != null)
                    timerText.text = gm.GetCartRaceTime(playerCart).ToString("F2") + "s";
                break;

            case GameManager.RaceState.Paused:
                lapText.text = "Paused";
                lapText.color = Color.gray;
                break;
        }
    }

    private void UpdateRaceTimer()
    {
        if (timerText == null || playerCart == null || GameManager.Instance == null) return;

        float raceTime = GameManager.Instance.GetCartRaceTime(playerCart);
        timerText.text = raceTime.ToString("F2") + "s";
    }

}
