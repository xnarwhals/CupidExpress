using UnityEngine.UI;
using UnityEngine;

public class PlayerPosition : MonoBehaviour
{
    // public TextMeshProUGUI positionText;
    public Cart playerCart;
    private Image positionIcon;
    public Sprite[] placeIcons; // Assign in Inspector: 0 = 1st, 1 = 2nd, etc.

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
            positionIcon.sprite = placeIcons[position - 1];
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
}
