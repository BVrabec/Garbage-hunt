using UnityEngine;

public class DraggableTrash : MonoBehaviour
{
    public TrashType trashType;               // Set when spawning (Plastic, Glass, etc.)
    public int inventoryIndex = -1;           // Index in inventory list (for removal)

    private Rigidbody2D rb;
    private Camera mainCam;
    private Vector3 mouseOffset;
    private bool isDragging = false;

    [Header("Throw Settings")]
    public float throwStrength = 8f;          // How strong the flick/throw is
    public float gravityAfterRelease = 1f;    // Gravity when dropped

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        // Start with no gravity (so trash floats until dragged)
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    void OnMouseDown()
    {
        if (!enabled) return;

        isDragging = true;
        mouseOffset = mainCam.ScreenToWorldPoint(Input.mousePosition) - transform.position;

        // Lift slightly above others
        transform.position += Vector3.forward * 0.1f;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
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

        // Enable gravity so it falls
        rb.gravityScale = gravityAfterRelease;

        // Calculate throw velocity based on mouse movement
        Vector3 mouseDelta = mainCam.ScreenToWorldPoint(Input.mousePosition) - mouseOffset;
        Vector2 throwVelocity = mouseDelta * throwStrength;

        // Add upward flick for trickshot feel
        throwVelocity.y = Mathf.Max(throwVelocity.y, 2f);

        rb.linearVelocity = throwVelocity;
    }

    // Called when trash enters a dumpster trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        Dumpster dumpster = other.GetComponent<Dumpster>();
        if (dumpster != null)
        {
            // Correct dumpster?
            if (dumpster.acceptedType == trashType)
            {
                // Success!
                InventoryManager.Instance.caps += dumpster.rewardCaps;
                InventoryManager.Instance.RemoveTrash(trashType);

                // Optional: visual feedback
                Debug.Log($"Scored! +{dumpster.rewardCaps} caps for {trashType}");

                // Update UI score
                SortingSceneManager ssm = FindObjectOfType<SortingSceneManager>();
                if (ssm != null) ssm.UpdateScore();

                // Remove this trash
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Wrong dumpster!");
                // Optional: bounce back or penalty
            }
        }
    }
}