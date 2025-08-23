using UnityEngine.UI;
using UnityEngine;

public class PlayerPosition : MonoBehaviour
{
    // public TextMeshProUGUI positionText;
    public Cart playerCart;
    public bool useEffect = false;
    private Image positionIcon;
    public Sprite[] placeIcons; // Assign in Inspector: 0 = 1st, 1 = 2nd, etc.
    
    [Header("Splat Feedback")]
    [Tooltip("Scale multiplier when the icon splats.")]
    public float splatScale = 1.4f;
    [Tooltip("Total duration of the splat animation (seconds).")]
    public float splatDuration = 0.25f;
    [Tooltip("If true, animate only when the sprite actually changes.")]
    public bool onlyOnChange = true;

    // runtime
    private Sprite previousSprite;
    private Coroutine splatCoroutine;

    private void Awake()
    {
        positionIcon = GetComponent<Image>();
        if (positionIcon == null)
        {
            Debug.LogError("PlayerPosition script requires an Image component to display position icon.");
        }
    }

    private void Start()
    {
        if (playerCart == null)
        {
            Debug.LogWarning("No cart assigned to PlayerPosition.");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCartPositionChanged += OnCartPositionChanged;
        }

        UpdatePositionUI();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCartPositionChanged -= OnCartPositionChanged;
        } 
    }

    private void OnCartPositionChanged(Cart cart, int position)
    {
        if (cart == playerCart)
        {
            FormatPosition(position);
        }
    }

    private void UpdatePositionUI()
    {
        if (playerCart != null)
        {
            int position = GameManager.Instance.GetCartPosition(playerCart);
            FormatPosition(position);

        }
    }

    private void FormatPosition(int position)
    {

        if (placeIcons != null && position > 0 && position <= placeIcons.Length)
        {
            Sprite newSprite = placeIcons[position - 1];
            if (positionIcon != null)
            {
                bool changed = previousSprite != newSprite;
                positionIcon.sprite = newSprite;
                if ((!onlyOnChange || changed) && useEffect)
                {
                    if (splatCoroutine != null) StopCoroutine(splatCoroutine);
                    splatCoroutine = StartCoroutine(DoSplat());
                }
                previousSprite = newSprite;
            }
        }

        // if (position == 1)
        // {
        //     positionText.text = "1st";
        // }
        // else if (position == 2)
        // {
        //     positionText.text = "2nd";
        // }
        // else if (position == 3)
        // {
        //     positionText.text = "3rd";
        // }
        // else
        // {
        //     positionText.text = position + "th";
        // }
    }

    private System.Collections.IEnumerator DoSplat()
    {
        if (positionIcon == null) yield break;

        Transform t = positionIcon.transform;
        Vector3 original = t.localScale;
        Vector3 target = original * splatScale;

        float half = splatDuration * 0.5f;
        float elapsed = 0f;

        // grow
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / half);
            // smooth ease
            float e = Mathf.SmoothStep(0f, 1f, p);
            t.localScale = Vector3.Lerp(original, target, e);
            yield return null;
        }

        // shrink back
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / half);
            float e = Mathf.SmoothStep(0f, 1f, p);
            t.localScale = Vector3.Lerp(target, original, e);
            yield return null;
        }

        t.localScale = original;
        splatCoroutine = null;
    }
}
