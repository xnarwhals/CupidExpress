using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testMessageSender : MonoBehaviour
{
    //public int delay = 200;

    // Invoked when a line of data is received from the serial device.
    void OnMessageArrived(string msg)
    {
        Debug.Log("Message arrived: " + msg);
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
