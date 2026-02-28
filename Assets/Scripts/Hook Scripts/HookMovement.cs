using UnityEngine;

public class HookMovement : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode descendKey = KeyCode.Space;     // Tap to drop
    public KeyCode ascendKey = KeyCode.W;          // Manual reel (optional)
    public KeyCode releaseKey = KeyCode.LeftShift; // Manual drop (optional)

    [Header("Pivot & Rope")]
    [Tooltip("Empty GameObject at the boat where the rope starts")]
    public Transform pivot;
    public float swingRopeLength = 0.8f;           // Short when at surface
    public float maxRopeLength = 7.5f;             // Max depth

    [Header("Swing – Snappier & Responsive")]
    public float swingSpeed = 2.5f;                // Faster oscillation
    public float maxSwingAngle = 90f;              // ±90° = full 180° arc
    public float manualSwingStrength = 80f;        // Stronger A/D control
    public float swingDamping = 0.96f;             // Less damping = livelier

    [Header("Speeds")]
    public float ropeSpeed = 10f;                  // Faster extend/reel

    [Header("Auto Features")]
    public bool autoReturnOnGrab = true;
    public bool destroyTrashOnReturn = true;

    [Header("Grab")]
    public Transform grabPoint;                    // Tip of the claw
    public float carrySmoothSpeed = 20f;

    [Header("Claw Rotation – Whole Claw Turns Nicely")]
    [Tooltip("How strongly the claw tilts with swing angle")]
    public float tiltMultiplier = 0.7f;
    [Tooltip("Maximum claw tilt angle")]
    public float maxTiltAngle = 60f;

    [Header("Debug")]
    public HookState currentState;
    [SerializeField] private float currentAngle;
    [SerializeField] private float currentRopeLength;

    public enum HookState
    {
        Swinging,
        Dropping,
        Reeling
    }

    private Rigidbody2D rb;
    private GameObject carriedTrash = null;
    private float targetAngle;
    private float targetRopeLength;
    private float angleVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (grabPoint == null) grabPoint = transform;

        if (pivot == null)
        {
            Debug.LogError("Missing Pivot! Create empty child under boat and assign it.");
            pivot = transform; // fallback
        }
    }

    void Start()
    {
        targetRopeLength = swingRopeLength;
        currentRopeLength = swingRopeLength;
        targetAngle = 0f;
        currentAngle = 0f;
        currentState = HookState.Swinging;
    }

    void Update()
    {
        HandleInputAndStates();

        // Swing logic (only when swinging)
        if (currentState == HookState.Swinging)
        {
            float autoSwing = Mathf.Sin(Time.time * swingSpeed) * maxSwingAngle;
            float manual = Input.GetAxisRaw("Horizontal") * manualSwingStrength;
            targetAngle = autoSwing + manual;

            // Stronger, snappier pull toward target
            float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);
            angleVelocity += angleDiff * 35f * Time.deltaTime;  // increased force
            angleVelocity *= swingDamping;
            currentAngle += angleVelocity * Time.deltaTime;
        }

        // Rope length change
        float lengthDir = Mathf.Sign(targetRopeLength - currentRopeLength);
        currentRopeLength += lengthDir * ropeSpeed * Time.deltaTime;
        currentRopeLength = Mathf.Clamp(currentRopeLength, swingRopeLength, maxRopeLength);

        UpdateHookPosition();

        // Carry trash perfectly aligned
        if (carriedTrash != null)
        {
            carriedTrash.transform.localPosition = Vector3.zero;
            carriedTrash.transform.localRotation = Quaternion.identity;
        }

        // Optional manual release
        if (Input.GetKeyDown(releaseKey) && carriedTrash != null)
        {
            ReleaseTrash(false); // don't destroy
        }
    }

    void HandleInputAndStates()
    {
        bool atTop = currentRopeLength <= swingRopeLength + 0.05f;

        if (currentState == HookState.Swinging)
        {
            if (Input.GetKeyDown(descendKey) && atTop)
            {
                currentState = HookState.Dropping;
                targetRopeLength = maxRopeLength;
                Debug.Log("Dropping...");
            }
        }
        else if (currentState == HookState.Dropping)
        {
            if (Input.GetKeyDown(ascendKey))
            {
                currentState = HookState.Reeling;
                targetRopeLength = swingRopeLength;
            }
        }
        else if (currentState == HookState.Reeling)
        {
            if (atTop)
            {
                currentState = HookState.Swinging;
                currentAngle = 0f;
                Debug.Log("Returned to top");

                if (carriedTrash != null && destroyTrashOnReturn)
                {
                    Debug.Log("Trash collected / scored!");
                    Destroy(carriedTrash);
                    carriedTrash = null;
                }
            }
        }
    }

    void UpdateHookPosition()
    {
        // Calculate direction from pivot
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
        Vector2 targetPos = (Vector2)pivot.position + direction * currentRopeLength;

        transform.position = targetPos;

        // Claw rotation – makes whole claw turn nicely with swing
        float targetTilt = currentAngle * tiltMultiplier;               // negative = natural hang
        targetTilt = Mathf.Clamp(targetTilt, -maxTiltAngle, maxTiltAngle);
        transform.rotation = Quaternion.Euler(0f, 0f, targetTilt);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (carriedTrash != null) return;

        if (other.CompareTag("Trash") || other.GetComponent<Trash>() != null)
        {
            GrabTrash(other.gameObject);
        }
    }

    private void GrabTrash(GameObject trash)
    {
        carriedTrash = trash;
        trash.transform.SetParent(grabPoint);
        trash.transform.localPosition = Vector3.zero;
        trash.transform.localRotation = Quaternion.identity;

        var trashRb = trash.GetComponent<Rigidbody2D>();
        if (trashRb) trashRb.bodyType = RigidbodyType2D.Kinematic;

        var trashFloat = trash.GetComponent<Trash>();
        if (trashFloat) trashFloat.enabled = false;

        Debug.Log("Trash grabbed!");

        // Auto-return when something is caught
        if (autoReturnOnGrab && currentState != HookState.Reeling)
        {
            currentState = HookState.Reeling;
            targetRopeLength = swingRopeLength;
            Debug.Log("Auto-reeling with trash!");
        }
    }

    private void ReleaseTrash(bool destroy = true)
    {
        if (carriedTrash == null) return;

        carriedTrash.transform.SetParent(null);

        var trashRb = carriedTrash.GetComponent<Rigidbody2D>();
        if (trashRb)
        {
            trashRb.bodyType = RigidbodyType2D.Dynamic;
            trashRb.linearVelocity += new Vector2(Random.Range(-1.5f, 1.5f), -1f);
        }

        var trashFloat = carriedTrash.GetComponent<Trash>();
        if (trashFloat) trashFloat.enabled = true;

        if (destroy)
        {
            Destroy(carriedTrash);
        }

        carriedTrash = null;
        Debug.Log(destroy ? "Trash scored/destroyed" : "Trash dropped");
    }
}