using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public System.Action<int> OnCapsChanged;

    [Header("Inventory")]
    public List<TrashType> inventory = new List<TrashType>();
    public int maxCapacity = 3;

    [Header("Caps (Money)")]
    public int caps = 0;

    [Header("Scene Tracking")]
    public string lastSceneName = "MainScene";

    void Awake()
    {
        // TEMPORARY RESET FOR TESTING – removes saved data every game start
        PlayerPrefs.DeleteAll();  // ← ADD THIS LINE (delete when you want saves to persist)
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Add this NEW method to your InventoryManager script
public void AddCaps(int amount)
{
    caps += amount;
    caps = Mathf.Max(0, caps);
    SaveData();
    
    OnCapsChanged?.Invoke(caps);
}

    public bool CanAddTrash(TrashType type)
    {
        return inventory.Count < maxCapacity;
    }

    public void AddTrash(TrashType type)
    {
        if (CanAddTrash(type))
        {
            inventory.Add(type);
            SaveData();
        }
    }

    public void RemoveTrash(TrashType type)
    {
        inventory.Remove(type);

        // Give reward for correct sorting
        AddCaps(10);

        // If all trash sorted, return to main scene automatically
        if (inventory.Count == 0 && SceneManager.GetActiveScene().name == "SortingScene")
        {
            GoToFishing();
        }
    }

    public void UpgradeCapacity()
    {
        if (caps >= 50)
        {
            AddCaps(-50);
            maxCapacity++;
         
            SaveData();
        }
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("Caps", caps);
        PlayerPrefs.SetInt("MaxCapacity", maxCapacity);
        PlayerPrefs.SetInt("InventoryCount", inventory.Count);
        for (int i = 0; i < inventory.Count; i++)
            PlayerPrefs.SetString("Trash_" + i, inventory[i].ToString());
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        caps = PlayerPrefs.GetInt("Caps", 0);
        maxCapacity = PlayerPrefs.GetInt("MaxCapacity", 3);
        inventory.Clear();
        int count = PlayerPrefs.GetInt("InventoryCount", 0);
        for (int i = 0; i < count; i++)
        {
            string typeStr = PlayerPrefs.GetString("Trash_" + i, "Plastic");
            if (System.Enum.TryParse<TrashType>(typeStr, out TrashType type))
                inventory.Add(type);
        }
        
    }

    public void GoToSorting()
    {
        lastSceneName = SceneManager.GetActiveScene().name;  // store current scene
        SceneManager.LoadScene("SortingScene");             // load sorting
    }
    public void GoToFishing()
    {
        if (!string.IsNullOrEmpty(lastSceneName) && lastSceneName != "SortingScene")
        {
            SceneManager.LoadScene(lastSceneName);  // return to where we came from
        }
        else
        {
            SceneManager.LoadScene("MainScene");   // fallback
        }
    }

    public void OnSortPressed()
    {
        InventoryManager.Instance.GoToFishing();  // Calls the method in InventoryManager
    }
    public void GoBackToPreviousScene()
    {
        // If lastSceneName was set and is not SortingScene, return to it
        if (!string.IsNullOrEmpty(lastSceneName) && lastSceneName != "SortingScene")
        {
            SceneManager.LoadScene(lastSceneName);
        }
        else
        {
            // fallback
            SceneManager.LoadScene("MainScene");
        }
    }
}