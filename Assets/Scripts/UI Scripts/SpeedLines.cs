using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedLines : MonoBehaviour
{
    [SerializeField, Range(0.0f, 1.5f)] float threshold = 0.8f;
    [SerializeField] float flipWait = 1.0f;
    BallKart ballkart;
    Image image;

    // Start is called before the first frame update
    void Start()
    {
        ballkart = FindAnyObjectByType<BallKart>();
        image = GetComponent<Image>();
    }

    float timer = 0.0f;
    // Update is called once per frame
    void Update()
    {
        image.enabled = ballkart.currentSpeed >= ballkart.maxSpeed * threshold;

        if (image.enabled)
        {
            timer += Time.deltaTime;
            if (timer > flipWait)
            {
                transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
                timer = 0.0f;
            }
        } else timer = 0.0f;    }
}
