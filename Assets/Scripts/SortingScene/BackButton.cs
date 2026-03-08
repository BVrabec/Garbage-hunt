using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour   // ← change "Back" to "BackButton"
{
    public void ReturnToMainScene()
    {
        InventoryManager.Instance.GoToFishing();  // Calls the method in 
    }
}