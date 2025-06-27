
using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Projectile")]
public class ProjectileItem : KartItem
{
    public override void Use(Cart cartUsingItem)
    {
        if (cartUsingItem != null)
        {
            Debug.Log($"{cartUsingItem.name} used {itemName}");
        }
    }
}
