using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;  // ← THIS LINE WAS MISSING!
using TMPro;

public class InventoryDisplay : MonoBehaviour
{
    [Header("UI Elements - One Icon + Text")]
    public Image trashIcon;                    // Drag your one trash icon Image here
    public TextMeshProUGUI trashCountText;     // Drag your TextMeshPro object here
    public Button sortButton;                  // Optional: your Sort button

    [Header("Icon Settings")]
    public Sprite trashIconSprite;             // Drag your trash icon sprite here (bag, can, etc.)

    void Start()
    {
        if (trashIcon != null && trashIconSprite != null)
        {
            trashIcon.sprite = trashIconSprite;
        }
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("Missing InventoryManager! Add GameManager object.");
            return;
        }

        var inv = InventoryManager.Instance;

        // Update text
        if (trashCountText != null)
        {
            trashCountText.text = $"{inv.inventory.Count} / {inv.maxCapacity}";
        }

        // Optional: enable/disable Sort button when full
        if (sortButton != null)
        {
            sortButton.interactable = inv.inventory.Count >= inv.maxCapacity;
        }
    }

    // Connect this to SortButton OnClick in Inspector
    public void OnSortPressed()
    {
        InventoryManager.Instance.GoToSorting();  // Calls the method in InventoryManager
    }
    public void OnBackButtonPressed()
    {
        InventoryManager.Instance.GoToFishing();  // Calls the method in InventoryManager
    }
}