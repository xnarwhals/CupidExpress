using UnityEngine;
using UnityEngine.InputSystem;

public class StartUI : MonoBehaviour
{
    private BarcodeInputReader barcodeInputReader;
    private void Awake()
    {
        barcodeInputReader = GetComponent<BarcodeInputReader>();
        if (barcodeInputReader == null)
        {
            Debug.LogError("BarcodeInputReader component is missing on StartUI.");
        }
    }

    private void Update()
    {
        string input = barcodeInputReader?.GetInput(); // 1) any barcode opens scene 
        if (!string.IsNullOrEmpty(input))
        {
            Debug.Log("Barcode input received: " + input);
            SceneLoader.Instance.LoadScene(1);
        }

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) // 2) X key opens scene
        {
            Debug.Log("Keyboard input detected, loading scene.");
            SceneLoader.Instance.LoadScene(1);
        }


        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) // 3) Gamepad button opens scene
        {
            SceneLoader.Instance.LoadScene(1);
        }
        
        
    }
}
