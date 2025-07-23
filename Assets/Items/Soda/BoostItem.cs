using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Boost")]
public class BoostItem : KartItem
{
    public float boostForce = 1500f;

    public override void Use(Cart cartUsingItem, bool throwBackward)
    {
        if (cartUsingItem != null)
        {
            cartUsingItem.ApplyBoost(boostForce);
        }
    }

}
