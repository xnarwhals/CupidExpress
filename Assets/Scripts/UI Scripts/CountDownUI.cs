using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountDownUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [Tooltip("Image that displays the countdown sprite")]
    public Image img;
    [Tooltip("Order: 3, 2, 1, GO (GO is last)")]
    public Sprite[] sprites;

    [Header("Animation")]
    public float popFrom = 0.5f;
    public float popTo = 1.15f;
    public float popTime = 0.25f;
    public float settleTime = 0.12f;
    public float holdTime = 3f;   // keep number/GO visible briefly
    public float fadeTime = 0.35f;
    // Shorter hold for the "GO" frame
    public float goHoldTime = 0.6f;

    Coroutine playing;
    // the numeric value the countdown starts from (e.g. 3 for 3..2..1..GO). Cached from GameManager.countDownDuration
    private int startCount = -1;

    void Awake()
    {
        if (!img) img = GetComponentInChildren<Image>(true);
        HideInstant();
    }

    void Start()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnCountdownUpdate += ShowNumber;
            GameManager.Instance.CountdownGO += OnCountdownGO;
            // cache the configured countdown start number so we can map sprites correctly
            startCount = Mathf.CeilToInt(GameManager.Instance.countDownDuration);
            // basic validation: we expect sprites.Length >= startCount + 1 (numbers + GO)
            if (sprites != null && sprites.Length < startCount + 1)
            {
                Debug.LogWarning($"[CountDownUI] sprites.Length ({(sprites != null ? sprites.Length : 0)}) is smaller than expected startCount+1 ({startCount + 1}). Indexing may be incorrect.");
            }
        }
            
    }

    void OnDisable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnCountdownUpdate -= ShowNumber;
            GameManager.Instance.CountdownGO -= OnCountdownGO;
        }
    }

    /// <summary>
    /// number: 3,2,1,0(=GO). Pass negative to hide.
    /// </summary>
    public void ShowNumber(int number)
    {
        if (number < 0 || sprites == null || sprites.Length == 0 || !img)
        {
            HideInstant();
            return;
        }

        // Map the incoming countdown number to the sprite index.
        // We support sprites arranged like: [N, N-1, ..., 1, GO] where N is the starting countdown number.
        int index;
        if (number == 0)
        {
            index = sprites.Length - 1; // GO is last
        }
        else
        {
            // If we cached a startCount from GameManager, use it to compute index: index = startCount - number
            // Fallback to the previous (number-1) behavior if startCount is unknown.
            if (startCount > 0)
                index = startCount - number;
            else
                index = number - 1;
        }

        index = Mathf.Clamp(index, 0, sprites.Length - 1);
        img.sprite = sprites[index];
        img.enabled = true;

        // reset visual state
        var c = img.color; c.a = 1f; img.color = c;
        img.rectTransform.localScale = Vector3.one * popFrom;

        if (playing != null) StopCoroutine(playing);
        // use a shorter hold time for GO (number == 0)
        float useHold = (number == 0) ? goHoldTime : holdTime;
        playing = StartCoroutine(PlayTween(useHold));
    }

    IEnumerator PlayTween(float holdDuration)
    {
        var rt = img.rectTransform;

        // pop up
        float t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popTime);
            float s = Mathf.Lerp(popFrom, popTo, 1f - (1f - p) * (1f - p)); // ease-out
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        // settle to 1
        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / settleTime);
            rt.localScale = Vector3.one * Mathf.Lerp(popTo, 1f, p);
            yield return null;
        }

    // hold visible (important so GO actually shows)
    yield return new WaitForSeconds(holdDuration);

        // fade out
        t = 0f;
        Color start = img.color;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeTime);
            img.color = new Color(start.r, start.g, start.b, Mathf.Lerp(1f, 0f, p));
            yield return null;
        }

        HideInstant();
        playing = null;
    }

    // Called when countdown reaches zero
    private void OnCountdownGO()
    {
        // Show the GO sprite (mapped to 0 in ShowNumber)
        ShowNumber(0);
    }

    void HideInstant()
    {
        if (!img) return;
        img.enabled = false;
        var c = img.color; c.a = 0f; img.color = c;
        img.rectTransform.localScale = Vector3.one;
    }
}