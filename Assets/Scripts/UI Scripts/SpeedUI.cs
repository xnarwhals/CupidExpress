using UnityEngine;
using TMPro;

public class SpeedUI : MonoBehaviour
{
    [Header("UI Ref")]
    public TextMeshProUGUI speedText;

    [Header("Cart Ref")]
    public Cart playerCart;

    [Header("Display Settings")]
    public string speedFormat = "{0} MPH";

    [Tooltip("lower = more update calls")]
    [Range(0.1f, 1f)]
    public float updateInterval = 0.1f; // Update every 0.1 seconds

    [Header("Colors")]
    public Color normalSpeedColor = new Color(1f, 1f, 0.8f); // Cream 
    public Color highSpeedColor = Color.red;
    public float highSpeedThreshold = 35f; // Red at high speeds

    private CartPhysics cartPhysics;
    private float lastUpdateTime;

    private void Start()
    {
        if (playerCart != null)
        {
            cartPhysics = playerCart.GetComponent<CartPhysics>();
        }
    }

    private void Update()
    {
        if (Time.unscaledTime - lastUpdateTime >= updateInterval)
        {
            UpdateSpeedDisplay();
            lastUpdateTime = Time.unscaledTime;
        }
    }

    private void UpdateSpeedDisplay()
    {
        if (cartPhysics == null) return;

        float speedMPS = GetCurSpeed(); // m/s 
        float speedMPH = speedMPS * 2.23694f; // for my fellow Americans 

        speedText.text = string.Format(speedFormat, Mathf.RoundToInt(speedMPH));

        if (speedMPH > highSpeedThreshold)
        {
            speedText.color = Color.Lerp(normalSpeedColor, highSpeedColor, (speedMPH - highSpeedThreshold) / 20f);
        }
        else
        {
            speedText.color = normalSpeedColor;
        }
    }

    private float GetCurSpeed()
    {
        if (cartPhysics == null) return 0f;

        Rigidbody rb = cartPhysics.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            return horizontalVelocity.magnitude; // m/s
        }

        return 0f;
    }

}
