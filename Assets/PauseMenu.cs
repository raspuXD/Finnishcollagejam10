using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles pause menu toggling via the new Input System.
/// Attach to a persistent GameObject (e.g. GameManager or Player).
/// Assign the PauseMenuPanel in the Inspector.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject pauseMenuPanel;
    [SerializeField] GameObject GameMenuPanel;

    [Header("Input")]
    // Expects an InputActionReference wired to your "Pause" action
    // (e.g. Player/Pause mapped to Escape or Start button)
    [SerializeField] InputActionReference pauseAction;

    bool _isPaused;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        pauseMenuPanel.SetActive(false);
    }

    void OnEnable()
    {
        pauseAction.action.Enable();
        pauseAction.action.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        pauseAction.action.performed -= OnPausePerformed;
        pauseAction.action.Disable();
    }

    // ── Input callback ───────────────────────────────────────────

    void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (_isPaused) Resume();
        else           Pause();
    }

    // ── Public API (wire to Resume button in the UI) ─────────────

    public void Pause()
    {
        _isPaused = true;
        pauseMenuPanel.SetActive(true);
        GameMenuPanel.SetActive(false);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Resume()
    {
        _isPaused = false;
        pauseMenuPanel.SetActive(false);
        GameMenuPanel.SetActive(true);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    /// <summary>Call from a "Quit" button.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}