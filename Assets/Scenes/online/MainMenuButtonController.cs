using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuButtonController : MonoBehaviour
{
    [Tooltip("Nombre de la escena del menú principal")]
    [SerializeField] private string menuSceneName = "MainMenu";
    
    private Button mainMenuButton;
    
    private void OnEnable()
    {
        // Obtener la referencia al documento UXML
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        // Buscar el botón en el documento
        mainMenuButton = root.Q<Button>("main-menu-button");
        
        // Añadir listener al botón
        if (mainMenuButton != null)
        {
            mainMenuButton.clicked += OnMainMenuButtonClicked;
        }
        else
        {
            Debug.LogError("No se encontró el botón 'main-menu-button' en el UI Document");
        }
    }

    private void OnDisable()
    {
        // Remover el listener cuando se desactiva el componente
        if (mainMenuButton != null)
        {
            mainMenuButton.clicked -= OnMainMenuButtonClicked;
        }
    }

    private void OnMainMenuButtonClicked()
    {
        // Cargar la escena del menú principal
        Debug.Log("Volviendo al menú principal: " + menuSceneName);
        SceneManager.LoadScene(menuSceneName);
    }
}