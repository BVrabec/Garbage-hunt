using UnityEngine;
using UnityEngine.SceneManagement;

public class HookMovement : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode descendKey = KeyCode.Space;
    public KeyCode ascendKey = KeyCode.W;
    public KeyCode releaseKey = KeyCode.LeftShift;

    [Header("Pivot & Rope")]
    public Transform pivot;
    public float swingRopeLength = 0.8f;
    public float maxRopeLength = 7.5f;

    [Header("Swing")]
    public float swingSpeed = 2.5f;
    public float maxSwingAngle = 90f;
    public float manualSwingStrength = 80f;
    public float swingDamping = 0.85f;

    [Header("Speeds")]
    public float ropeSpeed = 10f;

    [Header("Auto Features")]
    public bool autoReturnOnGrab = true;
    public string sortingSceneName = "SortingScene"; // ← change to your scene name

    [Header("Grab & Carry")]
    public Transform grabPoint;
    public Vector3 carryOffset = new Vector3(0f, -0.15f, -0.05f);
    public float carryFollowSpeed = 12f;

    [Header("Claw Rotation")]
    public float tiltMultiplier = 0.9f;
    public float maxTiltAngle = 60f;

    [Header("Trash Visibility")]
    public string trashSortingLayer = "Default";
    public int trashSortingOrder = 2;

    [Header("Debug")]
    public HookState currentState;
    [SerializeField] private float currentAngle;
    [SerializeField] private float currentRopeLength;

    public enum HookState { Swinging, Dropping, Reeling }

    private Rigidbody2D rb;
    private GameObject carriedTrash = null;
    private float targetAngle;
    private float targetRopeLength;
    private float angleVelocity;

    // Renderer used to draw rope (optional child)
    private RopeRenderer ropeRenderer;

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
            Debug.LogError("Missing Pivot!");
            pivot = transform;
        }

        // Try to locate a RopeRenderer in children
        ropeRenderer = GetComponent<RopeRenderer>();
        ropeRenderer = GetComponentInChildren<RopeRenderer>();
        if (ropeRenderer == null)
        {
            // not fatal, rope drawing is optional
            Debug.LogWarning("HookMovement: no RopeRenderer found in children. Rope won't be drawn until assigned.");
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

        if (currentState == HookState.Swinging)
        {
            float auto = Mathf.Sin(Time.time * swingSpeed) * maxSwingAngle;
            float manual = Input.GetAxisRaw("Horizontal") * manualSwingStrength;
            float target = auto + manual;

            float stiffness = 18f;
            float diff = Mathf.DeltaAngle(currentAngle, target);
            angleVelocity += diff * stiffness * Time.deltaTime;
            angleVelocity *= swingDamping;
            currentAngle += angleVelocity * Time.deltaTime;
        }

        float lengthDir = Mathf.Sign(targetRopeLength - currentRopeLength);
        currentRopeLength += lengthDir * ropeSpeed * Time.deltaTime;
        currentRopeLength = Mathf.Clamp(currentRopeLength, swingRopeLength, maxRopeLength);

        UpdateHookPosition();

        if (Input.GetKeyDown(releaseKey) && carriedTrash != null)
        {
            ReleaseTrash(false);
        }
    }

    void LateUpdate()
    {
        // Keep rope drawn every frame if renderer exists
        if (ropeRenderer != null)
        {
            ropeRenderer.RenderLine(transform.position, true);
        }

        if (carriedTrash == null) return;

        Vector3 desired = carryOffset;
        carriedTrash.transform.localPosition = Vector3.Lerp(
            carriedTrash.transform.localPosition,
            desired,
            Time.deltaTime * carryFollowSpeed
        );

        carriedTrash.transform.localRotation = Quaternion.Lerp(
            carriedTrash.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * carryFollowSpeed
        );

        // Keep sorting correct while carried
        SpriteRenderer[] srs = carriedTrash.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.sortingLayerName = trashSortingLayer;
            sr.sortingOrder = trashSortingOrder;
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

                if (carriedTrash != null)
                {
                    TrashTypeScript tt = carriedTrash.GetComponent<TrashTypeScript>();
                    if (tt != null)
                    {
                        // Add to inventory instead of loading scene
                        if (InventoryManager.Instance.CanAddTrash(tt.type))
                        {
                            InventoryManager.Instance.AddTrash(tt.type);
                            Debug.Log($"Added {tt.type} to inventory ({InventoryManager.Instance.inventory.Count}/{InventoryManager.Instance.maxCapacity})");
                        }
                        else
                        {
                            Debug.Log("Inventory full! Sort trash first.");
                            // Optional: drop trash back or keep it
                        }
                    }

                    Destroy(carriedTrash); // Remove from scene
                    carriedTrash = null;
                }
            }
        }
    }

    void UpdateHookPosition()
    {
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
        Vector2 targetPos = (Vector2)pivot.position + dir * currentRopeLength;
        transform.position = Vector2.Lerp(transform.position, targetPos, Time.deltaTime * 25f);

        float targetTilt = currentAngle * tiltMultiplier;
        targetTilt = Mathf.Clamp(targetTilt, -maxTiltAngle, maxTiltAngle);
        targetTilt += Mathf.Sign(currentAngle) * 4f;

        float curZ = transform.eulerAngles.z;
        if (curZ > 180) curZ -= 360;
        float newZ = Mathf.LerpAngle(curZ, targetTilt, Time.deltaTime * 18f);
        transform.rotation = Quaternion.Euler(0, 0, newZ);
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
        trash.transform.SetParent(grabPoint, true);

        var trashRb = trash.GetComponent<Rigidbody2D>();
        if (trashRb) trashRb.bodyType = RigidbodyType2D.Kinematic;

        var trashFloat = trash.GetComponent<Trash>();
        if (trashFloat) trashFloat.enabled = false;

        if (autoReturnOnGrab && currentState != HookState.Reeling)
        {
            currentState = HookState.Reeling;
            targetRopeLength = swingRopeLength;
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

        if (destroy) Destroy(carriedTrash);

        carriedTrash = null;
    }

    // Fixed MoveRope: works with the existing state/rope-length model and rope renderer.
    // It no longer references undefined fields like canRotate/move_Speed/min_Y/initial_Y etc.
    // Instead it uses currentState, pivot and rope lengths and draws the rope if available.
    void MoveRope()
    {
        // If swinging, rope does not move vertically here.
        if (currentState == HookState.Swinging)
            return;

        // Move along the hook's local up axis: Dropping goes down, Reeling goes up.
        Vector3 moveDirection = (currentState == HookState.Dropping) ? -transform.up : transform.up;
        float delta = ropeSpeed * Time.deltaTime;
        transform.position += moveDirection * delta;

        // Keep currentRopeLength consistent with distance to pivot so UpdateHookPosition logic stays valid.
        float dist = Vector2.Distance(pivot.position, transform.position);
        currentRopeLength = Mathf.Clamp(dist, swingRopeLength, maxRopeLength);

        // If we've reached extremes, snap state.
        if (Mathf.Approximately(currentRopeLength, maxRopeLength) || currentRopeLength >= maxRopeLength - 0.01f)
        {
            // At bottom
            currentRopeLength = maxRopeLength;
            // Optionally switch to reeling when hitting bottom
            currentState = HookState.Reeling;
        }

        if (Mathf.Approximately(currentRopeLength, swingRopeLength) || currentRopeLength <= swingRopeLength + 0.01f)
        {
            // At top
            currentRopeLength = swingRopeLength;
            currentState = HookState.Swinging;
            if (ropeRenderer != null) ropeRenderer.RenderLine(transform.position, false);
        }
        else
        {
            // Draw rope during movement
            if (ropeRenderer != null) ropeRenderer.RenderLine(transform.position, true);
        }
    }
}