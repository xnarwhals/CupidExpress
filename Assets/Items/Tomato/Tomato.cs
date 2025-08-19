using UnityEngine;

[CreateAssetMenu(menuName = "Kart Item/Tomato")]
public class Tomato : KartItem
{
    [Header("Tomato Specific")]
    public float throwForce = 15f;
    public float arcHeight = 3f;
    public GameObject throwableTomatoPrefab; 
    public GameObject ketchupSplatPrefab;
    [Range(0.1f, 50f)]
    public float lockOnDistance = 6f; 


    // Ketchup Splat Properties (Hit ground)
    public float splatRadius = 2f;
    public float splatDuration = 10f;
    // public float splatSlowdownEffect = 0.5f; // Percent velocity reduction


    // Tomato Hit 
    public float directHitSpinOutDuration = 2f;
    public float enterKetchupSpinOutDuration = 10f;
    public override void Use(Cart cartUsingItem, bool throwBackward)
    {
        if (cartUsingItem != null && throwableTomatoPrefab != null)
        {
            Vector3 throwDirection = throwBackward ? -cartUsingItem.forwardRef.forward : cartUsingItem.forwardRef.forward;
            ThrowTomato(cartUsingItem, throwDirection);
        }
    }

    private void ThrowTomato(Cart user, Vector3 throwDirection)
    {
        Vector3 throwPosition = user.itemSlot.position; // throw from where it is visually
        Debug.DrawLine(throwPosition, throwPosition + throwDirection * 5f, Color.green, 2f);


        GameObject tomato = Instantiate(throwableTomatoPrefab, throwPosition, Quaternion.identity);

        TomatoProjectile tomatoProjectile = tomato.GetComponent<TomatoProjectile>();

        
        if (tomatoProjectile != null)
        {
            tomatoProjectile.Initialize(this, user, throwDirection);
        }

        AudioManager.Instance.PlayTomatoThrow();
    }

}
