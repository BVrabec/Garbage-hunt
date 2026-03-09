using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject playPanel;

    void Start()
    {
        mainPanel.SetActive(true);
        playPanel.SetActive(false);
    }

    public void OpenPlayMenu()
    {
        mainPanel.SetActive(false);
        playPanel.SetActive(true);
    }

    public void BackToMain()
    {
        mainPanel.SetActive(true);
        playPanel.SetActive(false);
    }

    public void PlayMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void PlayArctic()
    {
        SceneManager.LoadScene("Arctic");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game"); // dela samo v buildu
    }
}