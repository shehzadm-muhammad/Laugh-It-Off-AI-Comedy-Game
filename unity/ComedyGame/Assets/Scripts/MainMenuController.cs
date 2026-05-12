using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject aboutPanel;

    public void StartGame()
    {
        SceneManager.LoadScene("CharacterSelect");
    }

    public void OpenAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(true);
    }

    public void CloseAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}