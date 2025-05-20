using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MainMenu";

    private VisualElement pauseMenu;
    private VisualElement backgroundOverlay;
    private Button resumeButton;
    private Button settingsButton;
    private Button quitButton;

    private bool isPaused = false;
    private UIDocument uiDocument;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        SetupUI();
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (backgroundOverlay != null)
            backgroundOverlay.style.display = DisplayStyle.None;

        if (pauseMenu != null)
            pauseMenu.style.display = DisplayStyle.None;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;

        backgroundOverlay = root.Q<VisualElement>("background");
        pauseMenu = root.Q<VisualElement>("pauseMenu");
        resumeButton = root.Q<Button>("resumeButton");
        settingsButton = root.Q<Button>("settingsButton");
        quitButton = root.Q<Button>("quitButton");

        if (resumeButton != null)
            resumeButton.clicked += ResumeGame;

        if (settingsButton != null)
            settingsButton.clicked += OpenSettings;

        if (quitButton != null)
            quitButton.clicked += ExitToMainMenu;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (backgroundOverlay != null)
            backgroundOverlay.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;

        if (pauseMenu != null)
        {
            pauseMenu.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;

            Vector3 startScale = isPaused ? new Vector3(0.8f, 0.8f, 1f) : new Vector3(1f, 1f, 1f);
            Vector3 endScale = isPaused ? new Vector3(1f, 1f, 1f) : new Vector3(0.8f, 0.8f, 1f);

            pauseMenu.style.scale = new Scale(startScale);
            StartCoroutine(AnimateMenuScale(pauseMenu, startScale, endScale, isPaused ? 0.3f : 0.15f));
        }

        Time.timeScale = isPaused ? 0f : 1f;
    }

    private IEnumerator AnimateMenuScale(VisualElement element, Vector3 startScale, Vector3 endScale, float duration)
    {
        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;

        while (Time.unscaledTime < endTime)
        {
            float t = (Time.unscaledTime - startTime) / duration;
            Vector3 currentScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0, 1, t));
            element.style.scale = new Scale(currentScale);
            yield return null;
        }

        element.style.scale = new Scale(endScale);
    }

    private void ResumeGame()
    {
        isPaused = false;

        if (backgroundOverlay != null)
            backgroundOverlay.style.display = DisplayStyle.None;

        if (pauseMenu != null)
            pauseMenu.style.display = DisplayStyle.None;

        Time.timeScale = 1f;
    }

    private void OpenSettings()
    {
        Debug.Log("Abriendo opciones...");
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene); // Sin fade
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}
