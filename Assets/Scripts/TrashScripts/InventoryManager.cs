using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory")]
    public List<TrashType> inventory = new List<TrashType>();
    public int maxCapacity = 3;

    [Header("Caps (Money)")]
    public int caps = 0;

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
        SaveData();
    }

    public void UpgradeCapacity()
    {
        if (caps >= 50)
        {
            caps -= 50;
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
        SceneManager.LoadScene("SortingScene");  // exact scene name!
    }
   public void GoToFishing()
{
    SceneManager.LoadScene("MainScene");  // ← CHANGE to your main fishing scene name (e.g. "Arena", "MainScene")
}

    public void OnSortPressed()
    {
        InventoryManager.Instance.GoToFishing();  // Calls the method in InventoryManager
    }
}