using UnityEngine;
using UnityEngine.SceneManagement;

public class HookMovement : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode descendKey = KeyCode.Space;
    public KeyCode ascendKey = KeyCode.W;
    public KeyCode releaseKey = KeyCode.LeftShift;

    [Header("Pivot & Rope")]
    public Transform pivot;                    // Main pivot (boat/start of rope)
    public Transform ropeStartPoint;           // Manual start point on hook (drag RopeStartPoint child here)
    public Transform ropeEndPoint;             // Manual end point on hook (drag RopeEndPoint child here)
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
    public string sortingSceneName = "SortingScene";

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

    [Header("Rope Line (VRVICA)")]
    public LineRenderer ropeLine;              // Drag LineRenderer here
    public float ropeWidth = 0.15f;
    public Color ropeColor = new Color(0.6f, 0.3f, 0.1f);
    public Material ropeMaterial;              // Drag rope texture material here

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

        // Setup rope line
        if (ropeLine == null)
        {
            ropeLine = gameObject.GetComponent<LineRenderer>();
            if (ropeLine == null)
            {
                ropeLine = gameObject.AddComponent<LineRenderer>();
            }

            ropeLine.positionCount = 2;
            ropeLine.startWidth = ropeWidth;
            ropeLine.endWidth = ropeWidth;
            ropeLine.useWorldSpace = true;

            if (ropeMaterial != null)
            {
                ropeLine.material = ropeMaterial;
                ropeLine.textureMode = LineTextureMode.Tile;
            }
            else
            {
                ropeLine.material = new Material(Shader.Find("Sprites/Default"));
                ropeLine.startColor = ropeColor;
                ropeLine.endColor = ropeColor;
            }

            ropeLine.sortingLayerName = "Default";
            ropeLine.sortingOrder = 1;
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
        UpdateRopeLine();

        if (Input.GetKeyDown(releaseKey) && carriedTrash != null)
        {
            ReleaseTrash(false);
        }
    }

    void LateUpdate()
    {
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
                        if (InventoryManager.Instance.CanAddTrash(tt.type))
                        {
                            InventoryManager.Instance.AddTrash(tt.type);
                            Debug.Log($"Added {tt.type} to inventory");
                        }
                        else
                        {
                            Debug.Log("Inventory full! Sort trash first.");
                        }
                    }
                    Destroy(carriedTrash);
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

    private void UpdateRopeLine()
    {
        if (ropeLine == null || pivot == null) return;

        ropeLine.positionCount = 2;
        ropeLine.SetPosition(0, pivot.position);          // Start at pivot (boat)

   
        // End at your manual RopeEndPoint
        if (ropeEndPoint != null)
        {
            ropeLine.SetPosition(1, ropeEndPoint.position);
        }
        else
        {
            // Fallback if not assigned
            ropeLine.SetPosition(1, transform.position);
            Debug.LogWarning("RopeEndPoint not assigned – using hook center");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (carriedTrash != null) return;

        if (other.CompareTag("Turtle") || other.CompareTag("Pufferfish"))
        {
            // Camera shake
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(0.2f, 0.15f);
            }

            // Hook goes back
            if (currentState != HookState.Reeling)
            {
                currentState = HookState.Reeling;
                targetRopeLength = swingRopeLength;
            }

            return;
        }

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
}