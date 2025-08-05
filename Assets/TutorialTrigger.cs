using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private Image customImage;

    void OnTriggerEnter(Collider other)
    {
        customImage.enabled = true;
        Time.timeScale = 0;
    }

    void OnTriggerExit(Collider other)
    {
        customImage.enabled = false;
        Time.timeScale = 1;
        Destroy(customImage);
    }

    //Check every frame to see if button is being pressed. Let player move if button is pressed.
    [SerializeField] float timeAmount, timeElapsed;
    void Update()
    {
        print("hi");
    }


}
