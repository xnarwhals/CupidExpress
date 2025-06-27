using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Boost")]
public class BoostItem : KartItem
{
    public float boostForce = 1500f;

    public override void Use(Cart cartUsingItem)
    {
        if (cartUsingItem != null)
        {
            // add force logic 
            Debug.Log($"{cartUsingItem.name} used {itemName}");
        }
    }

}
