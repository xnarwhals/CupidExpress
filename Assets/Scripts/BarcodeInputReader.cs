using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

public class BarcodeInputReader : MonoBehaviour
{
    private StringBuilder buffer = new StringBuilder();
    private float lastInputTime;
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

    private void Update()
    {
        if (buffer.Length > 0 && Time.time - lastInputTime > inputTimeout)
        {
            ProcessBarcode(); // fallback in case Enter isn't sent
        }
    }

    private void ProcessBarcode()
    {
        if (buffer.Length == 0) return;

        string result = buffer.ToString().Trim();
        Debug.Log("Scanned: " + result);
        buffer.Clear();
    }
}