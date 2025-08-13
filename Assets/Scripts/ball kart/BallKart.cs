using UnityEngine;

public class BallKart : CartPhysics
{
    [SerializeField] Transform kartTransform; //the parent of the kart (used for movement)
    [SerializeField] Transform kartNormal; //the kart child of transform, parent of model
    [SerializeField] Transform kartModel; //the actual model
    public Vector3 targetModelScale = Vector3.one;
    private float scaleLerpSpeed = 10f;

    [Header("Movement Settings")]
    [SerializeField] float floorGravity = 25f;
    [SerializeField] float airGravity = 25f;
    [SerializeField] float steerAccelleration = 4f;
    [SerializeField] float steerAcceleration2 = 5f;
    [SerializeField] float idleDecelleration = 1.0f;
    [SerializeField] float reverseAcceleration = 2.0f;
    [SerializeField] float reverseMaxSpeed = 15.0f;
    [SerializeField, Range(0.0f, 1.0f)] float AirControl = 0.5f;
    [SerializeField] float maxAngularVelocity = 7.0f;
    [SerializeField] float driftMaxAngularVelocity = 7.0f;

    [Header("Visual Stuff")]
    [SerializeField] float modelSteerOffset = 15f;
    [SerializeField] float modelDriftOffset = 20f;
    [SerializeField] float modelSteerOffsetSmoothing = 0.2f;
    [SerializeField] float kartOrientationRayLength = 1.0f;
    [SerializeField] float rampSmoothing = 8.0f;
    [SerializeField] float airSmoothing = 0.2f;


    [Header("Controls")]
    [SerializeField] bool invertSteering = false;

    [Header("Other")]
    [SerializeField] LayerMask floorLayerMask;
    [SerializeField] Transform resetTransform;
    private float spinOutDirection = 1f;

    public float currentSpeed = 0.0f;
    float currentRotate = 0.0f;
    float inputSpeed; //in case we want a more dynamic throttle system
    float inputRotation; //jik ^
    float currentAcceleration;
    private float originalDrag;
    bool grounded = false;

    Vector3 kartOffset; //makes it flush with the floor

    public override void Awake()
    {
        rb = GetComponent<Rigidbody>();

        kartOffset = kartTransform.position - transform.position;
    }

    [SerializeField] float postBoostDecceleration = 1.5f; //accel
    [SerializeField] float postBoostDecay = 0.01f;
    bool postBoost = false;
    private void Update()
    {
        // Shock minify
        kartModel.localScale = Vector3.Lerp(kartModel.localScale, targetModelScale, Time.deltaTime * scaleLerpSpeed);

        //Update()
        float dt = Time.deltaTime;
        if (invertSteering) steerInput = -steerInput;

        if (isSpinningOut || GameManager.Instance.GetCurrentRaceState() != GameManager.RaceState.Racing) return;

        //setting inputs to be used in fixed
        inputSpeed = maxSpeed * throttleInput; //in case we want a more dynamic throttle system
        inputRotation = steerInput * steerPower; //jik ^
        currentAcceleration = acceleration;

        //throttle forward vs backward acceleration & speed
        if (Mathf.Abs(throttleInput) <= 0.01f) currentAcceleration = idleDecelleration; //if no input, idle, uses rb drag in combination
        else if (throttleInput < 0) { currentAcceleration = reverseAcceleration; inputSpeed = reverseMaxSpeed * throttleInput; }

        postBoost = currentSpeed > maxSpeed + 0.01f;
        if (postBoost && inputSpeed > 0.01f) //if we're post boost and we want to go forward
        {
            currentSpeed = Mathf.SmoothStep(currentSpeed, currentSpeed - postBoostDecay, dt * postBoostDecceleration); //post boost sustain
        }
        else
        {
            float accelMultiplier = 1.0f;
            if (!grounded) accelMultiplier = AirControl;

            currentSpeed = Mathf.SmoothStep(currentSpeed, inputSpeed, dt * currentAcceleration * accelMultiplier); //acceleration
        }

        currentRotate = Mathf.Lerp(currentRotate, inputRotation, dt * steerAccelleration);

        //Drift(?)

        //tie the kart to the sphere
        kartTransform.position = transform.position + kartOffset;

        //model steering exaggeration/offset
        float steerDir = steerInput;
        steerDir *= DriftInput ? modelDriftOffset : modelSteerOffset;
        kartModel.localRotation = Quaternion.Euler(Vector3.Lerp(kartModel.localEulerAngles, new Vector3(0, (steerDir), kartModel.localEulerAngles.z), modelSteerOffsetSmoothing)); //model steering

    }

    public override void FixedUpdate()
    {

        if (GameManager.Instance.GetCurrentRaceState() != GameManager.RaceState.Racing) return;

        if (isSpinningOut)
        {
            spinOutTimer += Time.fixedDeltaTime;
            kartNormal.Rotate(Vector3.up * spinOutDirection * 400f * Time.fixedDeltaTime, Space.Self);

            if (spinOutTimer >= spinOutDuration)
            {
                isSpinningOut = false;
                targetModelScale = Vector3.one; // scale reset
                rb.drag = originalDrag;
            }
            return; // Skip normal movement while spinning out
        }

        float dt = Time.deltaTime;
        rb.AddForce(kartTransform.forward * currentSpeed, ForceMode.Acceleration);

        RaycastHit hitGravCheck;
        Physics.Raycast(kartTransform.position + (kartTransform.up * .1f), Vector3.down, out hitGravCheck, 2.0f, floorLayerMask); //find floor

        grounded = hitGravCheck.collider;

        float gravity = airGravity;
        if (grounded) gravity = floorGravity;

        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration); //also rb gravity exists

        kartTransform.eulerAngles = Vector3.Lerp(kartTransform.eulerAngles, new Vector3(0, kartTransform.eulerAngles.y + currentRotate, 0), dt * steerAcceleration2);

        //Drift
        if (DriftInput)
        {
            rb.maxAngularVelocity = driftMaxAngularVelocity;
        }
        else { rb.maxAngularVelocity = maxAngularVelocity; }

        //AIR CONTROL!!!!!!


        //kart puppeting
        RaycastHit hitNear;
        Physics.Raycast(kartTransform.position + (kartTransform.up * .1f), Vector3.down, out hitNear, kartOrientationRayLength, floorLayerMask); //find floor
        if (hitNear.collider) //if hit ground
        {
            kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, dt * rampSmoothing); //correct rotation
        }
        else
        {
            kartNormal.up = Vector3.Lerp(kartNormal.up, new Vector3(0, 1, 0), dt * airSmoothing); //correct rotation
        }
        kartNormal.Rotate(0, kartTransform.eulerAngles.y, 0);
    }


    public override void SpinOut(float duration)
    {
        base.SpinOut(duration);
        spinOutDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        originalDrag = rb.drag;
        rb.drag = 1.5f;
    }

    public void Shock(float duration)
    {
        SpinOut(duration);
        rb.velocity = Vector3.zero;
        targetModelScale = Vector3.one * 0.6f; // scale down by 60%
    }


    public override void Reset()
    {
        if (resetTransform)
        {
            transform.position = resetTransform.position;
            kartTransform.rotation = resetTransform.rotation;

            rb.velocity = new Vector3(0f, 0f, 0f);
            rb.angularVelocity = new Vector3(0f, 0f, 0f);

            //reset all vars
            currentSpeed = 0.0f;
            currentRotate = 0.0f;
            inputSpeed = 0.0f;
            inputRotation = 0.0f;
            currentAcceleration = 0.0f;
        }
        else print("Warning: resetTransform on ballKart not assigned");
    }

    float defaultMaxSpeed; //to store the max speed pre pad
    float defaultAccel; //to store the max speed pre pad
    public void Boost(float speed, float accel, bool toggle)
    {
        if (toggle)
        {
            defaultMaxSpeed = maxSpeed;
            defaultAccel = accel;

            maxSpeed = speed;
            acceleration = accel;
            SetThrottle(1f);
        }
        else
        {
            maxSpeed = defaultMaxSpeed;
            acceleration = defaultAccel;
        }
    }

    public void ApplyInstantBoost(float force)
    {
        if (rb != null)
        {
            rb.AddForce(kartTransform.forward * force, ForceMode.VelocityChange);
        }
    }

}
