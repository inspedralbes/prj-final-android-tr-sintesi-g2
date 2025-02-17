using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    private VisualElement pauseMenu;
    private bool isPaused = false;

    void Start()
    {
        // Obtener el UIDocument y el root
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Obtener el menú de pausa
        pauseMenu = root.Q<VisualElement>("pauseMenu");

        // Botones
        var resumeButton = root.Q<Button>("resumeButton");
        var settingsButton = root.Q<Button>("settingsButton");
        var quitButton = root.Q<Button>("quitButton");

        // Asignar funciones a los botones
        resumeButton.clicked += ResumeGame;
        settingsButton.clicked += OpenSettings;
        quitButton.clicked += QuitGame;

        // Asegurar que el menú de pausa esté oculto al inicio
        pauseMenu.style.display = DisplayStyle.None;
    }

    void Update()
    {
        // Detectar la tecla "Esc" para abrir/cerrar el menú de pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenu.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;
        Time.timeScale = isPaused ? 0 : 1; // Pausar o reanudar el juego
    }

    private void ResumeGame()
    {
        isPaused = false;
        pauseMenu.style.display = DisplayStyle.None;
        Time.timeScale = 1;
    }

    private void OpenSettings()
    {
        Debug.Log("Abriendo opciones...");
        // Aquí puedes cargar la escena de opciones o mostrar otro menú
    }

    private void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
