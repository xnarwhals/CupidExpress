using UnityEngine;
using UnityEngine.InputSystem;


public class StartUI : MonoBehaviour
{
    private BarcodeInputReader barcodeInputReader;
    public TMPro.TMP_InputField inputField;

    private void Awake()
    {
        barcodeInputReader = GetComponent<BarcodeInputReader>();
        if (barcodeInputReader == null)
        {
            Debug.LogError("BarcodeInputReader component is missing on StartUI.");
        }
    }
    
    public void setPlayerName()
    {
        PlayerData.PlayerName = inputField.text;
    }

    private void Update()
    {
        string input = barcodeInputReader?.GetInput(); // 1) any barcode opens scene 
        if (!string.IsNullOrEmpty(input))
        {
            Debug.Log("Barcode input received: " + input);
            SceneLoader.Instance.LoadScene(1);
        }

        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) // 3) Gamepad button opens scene
        {
            SceneLoader.Instance.LoadScene(1);
        }
    }

}
