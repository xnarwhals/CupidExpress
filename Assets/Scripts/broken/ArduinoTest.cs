using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO.Ports;
using System.Threading;

public class ArduinoTest : MonoBehaviour
{
    Thread IOThread = new Thread(DataThread);
    static SerialPort sp;

    static string incomingMsg = "";
    const int arduinoDelay = 200;

    static int test = 0;

    static void DataThread()
    {
        sp = new SerialPort("COM3", 9600);
        sp.Open();

        while (true)
        {
            incomingMsg = sp.ReadExisting();

            Thread.Sleep(arduinoDelay);
        }
    }

    private void OnDestroy()
    {
        IOThread.Abort();
        sp.Close();
    }

    // Start is called before the first frame update
    void Start()
    {
        IOThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        print(incomingMsg);
        
        /*string rawData = incomingMsg;
        string[] cutData = rawData.Split(", ");

        print("L: " + cutData[0] + ", R: " + cutData[1]);*/
    }
}
