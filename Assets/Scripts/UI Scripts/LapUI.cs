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
    public string lapTextFormat = "Lap {0}/{1}";

    public Color normalColor = Color.white;
    public Color finalLapColor = Color.yellow;

    private int countdownNumber = -1;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCartLapCompleted += OnLapCompleted;
            GameManager.Instance.OnRaceStateChanged += OnRaceStateChanged;
            GameManager.Instance.OnCountdownUpdate += OnCountdownUpdate;
        }

        UpdateLapUI();
    }

    private void Update()
    {
        if (timerText == null || playerCart == null || GameManager.Instance == null)
            return;

        var raceState = GameManager.Instance.GetCurrentRaceState();
        if (raceState == GameManager.RaceState.Racing)
        {
            float raceTime = GameManager.Instance.GetCartRaceTime(playerCart);
            timerText.text = "Time: " + raceTime.ToString("F2") + "s";
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCartLapCompleted -= OnLapCompleted;
            GameManager.Instance.OnRaceStateChanged -= OnRaceStateChanged;
            GameManager.Instance.OnCountdownUpdate -= OnCountdownUpdate;
        }
    }

    private void OnLapCompleted(Cart cart, int newLap)
    {
        if (cart == playerCart) // bots dont have ui
        {
            UpdateLapUI();
            UpdateRaceTimer();
            // complete lap sound
        }
    }

    private void OnRaceStateChanged(GameManager.RaceState newState)
    {
        UpdateLapUI();
    }

    private void UpdateLapUI()
    {
        if (lapText == null || playerCart == null || GameManager.Instance == null) return;


        int curLap = GameManager.Instance.GetCartLap(playerCart);
        int totalLaps = GameManager.Instance.totalLaps;
        var raceState = GameManager.Instance.GetCurrentRaceState();

        switch (raceState)
        {
            case GameManager.RaceState.WaitingToStart:
                lapText.text = "Ready to Race!";
                lapText.color = normalColor;
                break;

            case GameManager.RaceState.CountDown:
                lapText.text = countdownNumber.ToString();
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
                    timerText.text = "Time: " + GameManager.Instance.GetCartRaceTime(playerCart).ToString("F2") + "s";
                break;

            case GameManager.RaceState.Paused:
                lapText.text = "Paused";
                lapText.color = Color.gray;
                break;
        }
    }

    private void OnCountdownUpdate(int number)
    {
        countdownNumber = number;
        UpdateLapUI();
    }

    private void UpdateRaceTimer()
    {
        if (timerText == null || playerCart == null || GameManager.Instance == null) return;

        float raceTime = GameManager.Instance.GetCartRaceTime(playerCart);
        timerText.text = "Time: " + raceTime.ToString("F2") + "s";
    }

}
