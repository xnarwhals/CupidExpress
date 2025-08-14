using UnityEngine.UI;
using UnityEngine;

public class RoleSwapUI : MonoBehaviour
{
    public GameObject swapArrowDriver;
    public GameObject swapArrowPassenger;
    public Image DriverIcon;
    public Image PassengerIcon;

    private void Awake()
    {
        if (swapArrowDriver == null || swapArrowPassenger == null)
        {
            Debug.LogError("Swap arrows are not assigned in the RoleSwapUI script.");
            return;
        }

        if (DriverIcon == null || PassengerIcon == null)
        {
            Debug.LogError("Driver or Passenger icons are not assigned in the RoleSwapUI script.");
            return;
            
        }
    } 

    public void DriverRequestSwap(bool show)
    {
        swapArrowDriver.SetActive(show);
    }

    public void PassengerRequestSwap(bool show)
    {
        swapArrowPassenger.SetActive(show);
    }

    public void Reset()
    {
        swapArrowDriver.SetActive(false);
        swapArrowPassenger.SetActive(false);  
    }

    public void SwapIcons()
    {
        var temp = DriverIcon.sprite;
        DriverIcon.sprite = PassengerIcon.sprite;
        PassengerIcon.sprite = temp;
    }
}
