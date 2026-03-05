using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour   // ← change "Back" to "BackButton"
{
    public void ReturnToMainScene()
    {
        SceneManager.LoadScene("MainScene");  // ← your exact main scene name
    }
}