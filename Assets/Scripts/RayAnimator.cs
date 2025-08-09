using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayAnimator : MonoBehaviour
{
    [SerializeField] Rigidbody PlayerRigidBody;
    [SerializeField] Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerRigidBody == null) print("Set RB on Ray Animator!!!!");
        if (animator == null) print("Set animator on Ray!");
    }

    // Update is called once per frame
    void Update()
    {
        if (!(PlayerRigidBody && animator)) return;

        animator.SetFloat("Speed", PlayerRigidBody.velocity.magnitude);
    }
}
