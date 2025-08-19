using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayAnimator : MonoBehaviour
{
    BallKart movement;
    [SerializeField] Animator animator;

    [SerializeField, Range(0.0f, 1.0f)] float runPercentage = 0.95f;
    [SerializeField] float stopDeadzone = 0.5f;

    private void Awake()
    {
        movement = FindAnyObjectByType<BallKart>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (movement == null) print("No ballkart found");
        if (animator == null) print("Set animator on Ray!");
    }

    // Update is called once per frame
    void Update()
    {
        if (!(movement && animator)) return;

        int state = 0;

        if (movement.currentSpeed > movement.maxSpeed * runPercentage) state = 2;
        else if (Mathf.Abs(movement.currentSpeed) > stopDeadzone) state = 1;
        
        animator.SetInteger("State", state);
    }
}
