using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

public class BarcodeInputReader : MonoBehaviour
{
    private StringBuilder buffer = new StringBuilder();
    private float lastInputTime;
    private string lastScannedBarcode = "";
    public float inputTimeout = 0.2f;

    private void OnEnable()
    {
        Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnDisable()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char character)
    {
        if (character == '\n' || character == '\r')
        {
            ProcessBarcode();
        }
        else
        {
            buffer.Append(character);
            lastInputTime = Time.time;
        }
    }

    // private void Update()
    // {
    //     if (buffer.Length > 0 && Time.time - lastInputTime > inputTimeout)
    //     {
    //         ProcessBarcode(); // fallback in case Enter isn't sent
    //     }
    // }

    private void Start()
    {
        // if (Gamepad.current != null) Debug.Log("Connected to gamepad");
        // if (Joystick.current != null) Debug.Log("Connected to joystick");
        
    }

    private void ProcessBarcode()
    {
     if (buffer.Length == 0) return;

        string result = buffer.ToString().Trim();
        Debug.Log("Scanned: " + result);
        lastScannedBarcode = result; // Store for external access
        buffer.Clear();
    }

    public string GetInput()
    {
        if (string.IsNullOrEmpty(lastScannedBarcode)) return string.Empty;

        string result = lastScannedBarcode;
        lastScannedBarcode = "";
        return result;
    }
}