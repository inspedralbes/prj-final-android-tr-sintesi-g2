using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private void OnEnable()
    {
        // Obtener el UIDocument y su raíz
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("No se encontró un UIDocument en este GameObject.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("rootVisualElement no se ha inicializado correctamente.");
            return;
        }

        // Obtener los botones
        Button singlePlayerButton = root.Q<Button>("SinglePlayerButton");
        Button multiplayerButton = root.Q<Button>("MultiplayerButton");
        Button creditsButton = root.Q<Button>("CreditsButton");

        // Validar que los botones existen antes de asignar eventos
        if (singlePlayerButton == null || multiplayerButton == null || creditsButton == null)
        {
            Debug.LogError("Uno o más botones no fueron encontrados en el UXML.");
            return;
        }

        // Eliminar eventos previos para evitar múltiples suscripciones
        singlePlayerButton.clicked -= () => LoadScene("CargarPartida");
        singlePlayerButton.clicked += () => LoadScene("CargarPartida");

        multiplayerButton.clicked -= () => LoadScene("EnUnFuturo");
        multiplayerButton.clicked += () => LoadScene("EnUnFuturo");

        creditsButton.clicked -= () => LoadScene("EnUnFuturo");
        creditsButton.clicked += () => LoadScene("EnUnFuturo");
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
