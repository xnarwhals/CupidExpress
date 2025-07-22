using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ArduinoMessageHandler : MonoBehaviour
{
    //public int delay = 200;
    [DoNotSerialize] public int input0 = 0;
    [DoNotSerialize] public int input1 = 0;

    // Invoked when a line of data is received from the serial device.
    void OnMessageArrived(string msg)
    {
        //Debug.Log("Message arrived: " + msg); //print

        string[] words = msg.Split(", "); //split into 2 vals
        int.TryParse(words[0], out input0); //parse to int and set
        int.TryParse(words[1], out input1);
    }

    // Invoked when a connect/disconnect event occurs. The parameter 'success'
    // will be 'true' upon connection, and 'false' upon disconnection or
    // failure to connect.
    void OnConnectionEvent(bool success)
    {
        if (success)
            Debug.Log("Connection established");
        else
            Debug.Log("Connection attempt failed or disconnection detected");
    }

    SerialController serialController;
    private void Start()
    {
        serialController = GameObject.FindAnyObjectByType<SerialController>().GetComponent<SerialController>();
        //serialController.SendSerialMessage(delay.ToString());
    }
}
