using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{
    [SerializeField] bool startsTimeAtOne = true;
    [SerializeField] bool locksMouse = false;
    private void Start()
    {
        if(locksMouse)
        {
            UpdateMouseToBeLocked();
        }

        UpdateTheTimeIfNeeded(startsTimeAtOne);
    }

    public void UpdateMouseToBeLocked()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UpdateMouseToBeUnlocked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UpdateTheTimeIfNeeded(bool whatToDoWithTime)
    {
        Time.timeScale = whatToDoWithTime ? 1f : 0f;
    }

    public void LoadSceneNamed(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("mainmenu");
    }

    public void LoadSceneAgain()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void CloseTheGame()
    {
        Application.Quit();
    }
}
