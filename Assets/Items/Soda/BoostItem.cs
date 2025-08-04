using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Boost")]
public class BoostItem : KartItem
{
    // public float boostForce = 1500f;
    public float boostDuration = 2f;
    public float speedMultiplier = 1.5f;
    

    public override void Use(Cart cartUsingItem, bool throwBackward)
    {
        if (cartUsingItem != null)
        {
            cartUsingItem.ApplyBoost(boostDuration, speedMultiplier);
        }
    }

}
