
using UnityEngine.UI;
using UnityEngine;

public class KetchupEffect : MonoBehaviour
{
    [Header("Visual Effect Settings")]
    public Image ketchupOverlay;
    public AnimationCurve splatCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Effect Settings")]
    public float maxOpacity = 0.6f;
    public float effectDuration = 10f;

    private bool isEffectActive = false;
    private float effectTimer = 0f;


    private void Awake()
    {
        if (ketchupOverlay == null)
        {
            Debug.LogError("KetchupEffect script requires an Image component for the overlay.");
        }
        else
        {
            ketchupOverlay.gameObject.SetActive(false); 
        }     
    }
    private void Update()
    {
        if (isEffectActive)
        {
            effectTimer += Time.deltaTime;
            float t = effectTimer / effectDuration; // Duration of the effect
            if (t >= 1f)
            {
                EndEffect();
            }
            else
            {
                float curveValue = splatCurve.Evaluate(t);
                UpdateOverlayAlpha(curveValue * maxOpacity);
            }
        }

    }

    private void EndEffect()
    {
        isEffectActive = false;
        effectTimer = 0f;
        ketchupOverlay.gameObject.SetActive(false);
    }

    private void UpdateOverlayAlpha(float alpha)
    {
        if (ketchupOverlay != null)
        {
            Color color = ketchupOverlay.color;
            color.a = alpha;
            ketchupOverlay.color = color;
        }
    }

    public void StartKetchupEffect()
    {
        if (!isEffectActive)
        {
            isEffectActive = true;
            effectTimer = 0f;
            ketchupOverlay.gameObject.SetActive(true);

            // AudioManager.Instance.PlayTomatoHit(); // Play sound effect
        }
    }


}
