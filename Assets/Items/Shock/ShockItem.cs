using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Shock")]
public class ShockItem : KartItem
{

    public float shockDuration = 2.5f; // duration of the shock effect

    public override void Use(Cart cartUsingItem, bool throwBackward)
    {
        if (cartUsingItem != null)
        {
            var leaderboard = GameManager.Instance.GetCartLeaderboard();
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var cart = leaderboard[i];
                //cart != cartUsingItem
                if (cart != null && cart != cartUsingItem)
                {
                    cart.Shock(shockDuration);
                    ItemManager.Instance.ClearCartItems(cart);
                }
            }

            AudioManager.Instance.PlayTomatoThrow();
        }
    }

    
}
