using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class LapUI : MonoBehaviour
{
    [Header("UI Ref")]
    public TextMeshProUGUI lapText;

    [Header("Cart Ref")]
    public Cart playerCart;

    [Header("UI settings")]
    public string lapTextFormat = "Lap {0}/{1}";

    public Color normalColor = Color.white;
    public Color finalLapColor = Color.yellow;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCartLapCompleted += OnLapCompleted;
            GameManager.Instance.OnRaceStateChanged += OnRaceStateChanged;
        }

        UpdateLapUI();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
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
            // complete lap sound
        }
    }

    private void OnRaceStateChanged(GameManager.RaceState newState)
    {
        UpdateLapUI();
    }

    private void UpdateLapUI()
    {
        if (lapText == null || playerCart == null || GameManager.Instance == null)
        {
            return;
        }

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
                lapText.text = "Countdown...";
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
                break;

            case GameManager.RaceState.Paused:
                lapText.text = "Paused";
                lapText.color = Color.gray;
                break;
        }
    }

}
