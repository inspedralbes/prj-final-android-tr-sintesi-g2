using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;  // Asegúrate de importar este espacio de nombres

public class InitialScreen : MonoBehaviour
{
    private Button nextButton;

    void Start()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        
        // Obtén el botón de tu UXML
        nextButton = rootVisualElement.Q<Button>("nextButton");

        // Asocia un evento al botón
        nextButton.clicked += OnButtonClicked;

        // Asocia un evento para detectar teclas presionadas
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyPress);
    }

    // Método para cambiar a la siguiente pantalla cuando se hace clic en el botón
    private void OnButtonClicked()
    {
        LoadNextScene();
    }

    // Método para detectar la tecla presionada
    private void OnKeyPress(KeyDownEvent evt)
    {
        LoadNextScene();
    }

    // Método para cargar la siguiente escena
    private void LoadNextScene()
    {
        // Puedes cambiar el nombre de la escena según lo necesites
        SceneManager.LoadScene("NextScene");
    }
}
