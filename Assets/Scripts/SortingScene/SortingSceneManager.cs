using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TrashSpriteEntry
{
    public TrashType type;
    public Sprite sprite;
}

public class SortingSceneManager : MonoBehaviour
{
    [Header("Trash Spawn Settings")]
    public Transform spawnParent;
    public GameObject trashPrefab;

    [Header("Trash Sprites")]
    public List<TrashSpriteEntry> trashSprites = new List<TrashSpriteEntry>();

    [Header("Test Trash (optional)")]
    [SerializeField] private List<TrashType> testTrashTypes = new List<TrashType>();

    [Header("Selection UI")]
    public Transform selectionPanel;
    public GameObject selectionButtonPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    [Header("Containers / Dumpsters")]
    public Dumpster[] dumpsters;

    [Header("Back Button")]
    public Button backButton;

    void Start()
    {
        PopulateSelectionUI();
        UpdateScore();

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToFishing);
        }
    }

    Sprite GetSpriteForType(TrashType type)
    {
        foreach (var entry in trashSprites)
        {
            if (entry.type == type)
                return entry.sprite;
        }

        Debug.LogWarning($"No sprite assigned for TrashType.{type}");
        return null;
    }

    private void PopulateSelectionUI()
    {
        if (selectionPanel == null || selectionButtonPrefab == null)
        {
            Debug.LogWarning("Selection UI not set up → spawning all directly.");
            SpawnAllAvailable();
            return;
        }

        for (int i = selectionPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(selectionPanel.GetChild(i).gameObject);
        }

        Dictionary<TrashType, int> typeCounts = GetTypeCounts();

        if (typeCounts.Count == 0)
        {
            Debug.Log("No trash types available to display.");
            return;
        }

        foreach (var pair in typeCounts)
        {
            TrashType type = pair.Key;
            int count = pair.Value;

            GameObject btnObj = Instantiate(selectionButtonPrefab, selectionPanel);
            Button btn = btnObj.GetComponent<Button>();

            if (btn == null)
            {
                Debug.LogWarning("Button prefab missing Button component!");
                Destroy(btnObj);
                continue;
            }

            Image icon = btnObj.GetComponent<Image>() ?? btnObj.GetComponentInChildren<Image>();

            Sprite sprite = GetSpriteForType(type);

            if (icon != null)
            {
                if (sprite != null)
                {
                    icon.sprite = sprite;
                    icon.enabled = true;
                }
                else
                {
                    icon.enabled = false;
                }
            }

            TextMeshProUGUI countLabel = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (countLabel != null)
            {
                countLabel.text = count > 1 ? $"×{count}" : "";
            }

            TrashType captured = type;
            btn.onClick.AddListener(() => SpawnTrashOfType(captured));
        }
    }

    private Dictionary<TrashType, int> GetTypeCounts()
    {
        var dict = new Dictionary<TrashType, int>();

        List<TrashType> source = testTrashTypes.Count > 0 ? testTrashTypes : InventoryManager.Instance?.inventory;

        if (source == null || source.Count == 0) return dict;

        foreach (TrashType t in source)
        {
            dict.TryGetValue(t, out int cnt);
            dict[t] = cnt + 1;
        }

        return dict;
    }

    private void SpawnAllAvailable()
    {
        var counts = GetTypeCounts();
        int i = 0;

        foreach (var pair in counts)
        {
            for (int k = 0; k < pair.Value; k++)
            {
                Vector3 pos = new Vector3((i - counts.Count / 2f) * 1.8f, 4f, 0f);
                SpawnTrashOfType(pair.Key, pos);
                i++;
            }
        }
    }

    public void SpawnTrashOfType(TrashType type, Vector3? forcedPosition = null)
    {
        if (trashPrefab == null || spawnParent == null)
        {
            Debug.LogError("Missing trashPrefab or spawnParent!");
            return;
        }

        Sprite sprite = GetSpriteForType(type);
        if (sprite == null) return;

        Vector3 pos = forcedPosition ?? new Vector3(Random.Range(-4f, 4f), 5f, 0f);

        GameObject obj = Instantiate(trashPrefab, spawnParent);
        obj.transform.position = pos;

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr) sr.sprite = sprite;

        var drag = obj.GetComponent<DraggableTrash>();
        if (drag != null)
        {
            drag.trashType = type;
        }
    }

    public void UpdateScore()
    {
        if (scoreText != null && InventoryManager.Instance != null)
        {
            scoreText.text = $"Caps: {InventoryManager.Instance.caps}";
        }
    }

    public void BackToFishing()
    {
        if (InventoryManager.Instance != null)
        {
            // This will go back to the scene the player came from
            InventoryManager.Instance.GoBackToPreviousScene();
        }
    }

    public void OnSortPressed()
    {
        BackToFishing();
    }
}