using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class StartUI : MonoBehaviour
{
    private BarcodeInputReader barcodeInputReader;
    public TMPro.TMP_InputField inputField;
    public TMPro.TMP_Text leaderboardText;
    private void Awake()
    {
        barcodeInputReader = GetComponent<BarcodeInputReader>();
        if (barcodeInputReader == null)
        {
            Debug.LogError("BarcodeInputReader component is missing on StartUI.");
        }
    }

    private void Start()
    {
        if (LocalLeaderboard.BestTimes.Count > 0)
        {
            ShowLeaderboard();
        }
        else
        {
            leaderboardText.text = "No times recorded yet.";
        }
       
        // inputField.text = PlayerData.PlayerName;    
    }




    private void Update()
    {
        string input = barcodeInputReader?.GetInput(); // 1) any barcode opens scene 
        if (!string.IsNullOrEmpty(input))
        {
            Debug.Log("Barcode input received: " + input);
            PlayerData.PlayerName = inputField.text;
            SceneLoader.Instance.LoadScene(1);
        }

        // if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) // 2) X key opens scene
        // {
        //     Debug.Log("Keyboard input detected, loading scene.");
        //     PlayerData.PlayerName = inputField.text;
        //     SceneLoader.Instance.LoadScene(1);
        // }



        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) // 3) Gamepad button opens scene
        {
            PlayerData.PlayerName = inputField.text;
            SceneLoader.Instance.LoadScene(1);
        }
    }

    void ShowLeaderboard()
    {
        for (int i = 0; i < LocalLeaderboard.BestTimes.Count; i++)
        {
            leaderboardText.text += $"{i + 1}. {LocalLeaderboard.BestNames[i]}: {LocalLeaderboard.BestTimes[i]:F2} seconds\n";
        }
    }
}
