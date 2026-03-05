using UnityEngine;
using TMPro;

public class SortingSceneManager : MonoBehaviour
{
    [Header("Trash Spawn Settings")]
    public Transform spawnParent;              // Empty parent object for spawned trash (drag an empty GO here)
    public GameObject trashPrefab;             // Prefab with DraggableTrash + Rigidbody2D + Collider2D + SpriteRenderer
    public Sprite[] trashSprites;              // Array: 0 = Plastic, 1 = Glass, 2 = Metal, 3 = Organic (drag sprites in order)

    [Header("UI")]
    public TextMeshProUGUI scoreText;          // Text showing "Caps: 0"

    [Header("Containers / Dumpsters")]
    public Dumpster[] dumpsters;               // Drag your 4 dumpster objects here (each with Dumpster script)

    [Header("Back Button")]
    public UnityEngine.UI.Button backButton;   // Optional: button to return to fishing scene

    void Start()
    {
        SpawnTrashFromInventory();
        UpdateScore();

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToFishing);
        }
    }

    private void SpawnTrashFromInventory()
    {
        var inv = InventoryManager.Instance;
        if (inv == null || inv.inventory.Count == 0)
        {
            Debug.Log("No trash in inventory or InventoryManager missing.");
            return;
        }

        for (int i = 0; i < inv.inventory.Count; i++)
        {
            TrashType type = inv.inventory[i];
            int spriteIndex = (int)type;

            if (spriteIndex >= trashSprites.Length || trashSprites[spriteIndex] == null)
            {
                Debug.LogWarning($"No sprite for type {type} at index {spriteIndex}");
                continue;
            }

            GameObject trashObj = Instantiate(trashPrefab, spawnParent);
            trashObj.GetComponent<SpriteRenderer>().sprite = trashSprites[spriteIndex];

            var draggable = trashObj.GetComponent<DraggableTrash>();
            if (draggable != null)
            {
                draggable.trashType = type;
                draggable.inventoryIndex = i; // so it can remove itself from list on correct drop
            }

            // Position in a nice row at top of screen
            float xPos = (i - inv.inventory.Count / 2f) * 1.5f; // centered
            trashObj.transform.position = new Vector3(xPos, 4f, 0);
        }
    }

    public void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Caps: {InventoryManager.Instance.caps}";
        }
    }

    public void BackToFishing()
    {
        InventoryManager.Instance.GoToFishing();
    }
    public void OnSortPressed()
    {
        InventoryManager.Instance.GoToFishing();  // Calls the method in InventoryManager
    }
}