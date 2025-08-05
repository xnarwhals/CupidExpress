using TMPro;
using UnityEngine;

public class PlayerPosition : MonoBehaviour
{
    public TextMeshProUGUI positionText;
    public Cart playerCart;

    private void Start()
    {
        if (playerCart == null) return;

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
        if (position == 1)
        {
            positionText.text = "1st";
        }
        else if (position == 2)
        {
            positionText.text = "2nd";
        }
        else if (position == 3)
        {
            positionText.text = "3rd";
        }
        else
        {
            positionText.text = position + "th";
        }
    }
}
