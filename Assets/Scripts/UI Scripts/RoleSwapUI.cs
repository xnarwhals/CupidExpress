using UnityEngine.UI;
using UnityEngine;

public class RoleSwapUI : MonoBehaviour
{
    public GameObject swapArrowDriver;
    public GameObject swapArrowPassenger;
    public Image DriverIcon;
    public Image PassengerIcon;

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
