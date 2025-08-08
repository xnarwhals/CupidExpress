using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint System")]
    public int checkpointIndex = 0;

    [Tooltip("Start and Finish")]
    public bool isStartFinish = false;

    private void OnTriggerEnter(Collider other)
    {
        Cart cart = other.GetComponent<Cart>();
        if (cart != null && GameManager.Instance != null)
        {
            GameManager.Instance.OnCartPassedCheckpoint(cart, checkpointIndex);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isStartFinish ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        //UnityEditor.Handles.Label(transform.position, $"Checkpoint {checkpointIndex}");
    }
}
