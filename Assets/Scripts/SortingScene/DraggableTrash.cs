using UnityEngine;

public class DraggableTrash : MonoBehaviour
{
    public TrashType trashType;
    public int inventoryIndex = -1;

    private Rigidbody2D rb;
    private Camera mainCam;
    private Vector3 mouseOffset;
    private bool isDragging = false;

    [Header("Throw & Physics")]
    public float throwStrength = 80f;         // How far/fast the flick/throw goes
    public float gravityAfterRelease = 10f;    // How fast it falls (higher = faster drop)

    [Header("Screen Bounce")]
    public float bounceFactor = 1f;         // How much speed kept after bounce (0.7 = lose 30%)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        // Start floating (no gravity)

        rb.freezeRotation = false; // allow spin on throw
    }

    void OnMouseDown()
    {
        isDragging = true;
        mouseOffset = mainCam.ScreenToWorldPoint(Input.mousePosition) - transform.position;

        // Lift above others
        transform.position += Vector3.forward * 0.1f;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        transform.position = mousePos - mouseOffset;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        // Apply gravity now
        rb.gravityScale = gravityAfterRelease;

        // Calculate throw from mouse movement
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDelta = mousePos - (transform.position + mouseOffset);
        Vector2 throwVel = mouseDelta * throwStrength;

        // Add upward flick for nice arc
        throwVel.y += 5f; // adjust this for more/less arc

        rb.linearVelocity = throwVel;

        // Optional: add random spin on throw
        rb.angularVelocity = Random.Range(-200f, 200f);
    }

    void Update()
    {
        // Bounce off screen bounds (invisible walls)
        if (!isDragging)
        {
            BounceOffScreen();
        }
    }

    private void BounceOffScreen()
    {
        if (mainCam == null) return;

        float camHeight = mainCam.orthographicSize * 2f;
        float camWidth = camHeight * mainCam.aspect;

        Vector3 pos = transform.position;
        Vector2 vel = rb.linearVelocity;

        bool bounced = false;

        // Left
        if (pos.x < -camWidth / 2)
        {
            pos.x = -camWidth / 2;
            vel.x = Mathf.Abs(vel.x) * bounceFactor;
            bounced = true;
        }
        // Right
        if (pos.x > camWidth / 2)
        {
            pos.x = camWidth / 2;
            vel.x = -Mathf.Abs(vel.x) * bounceFactor;
            bounced = true;
        }
        // Top
        if (pos.y > camHeight / 2)
        {
            pos.y = camHeight / 2;
            vel.y = -Mathf.Abs(vel.y) * bounceFactor;
            bounced = true;
        }
        // Bottom
        if (pos.y < -camHeight / 2)
        {
            pos.y = -camHeight / 2;
            vel.y = Mathf.Abs(vel.y) * bounceFactor;
            bounced = true;
        }

        if (bounced)
        {
            transform.position = pos;
            rb.linearVelocity = vel;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Dumpster dumpster = other.GetComponent<Dumpster>();
        if (dumpster != null)
        {
            if (dumpster.acceptedType == trashType)
            {
                InventoryManager.Instance.caps += dumpster.rewardCaps;
                InventoryManager.Instance.RemoveTrash(trashType);
                SortingSceneManager ssm = FindObjectOfType<SortingSceneManager>();
                if (ssm != null) ssm.UpdateScore();
                Destroy(gameObject);
                Debug.Log($"Scored {trashType} +{dumpster.rewardCaps} caps!");
            }
            else
            {
                Debug.Log("Wrong dumpster!");
            }
        }
    }
}