using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class CreditsMenuButton : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Menu"; // Pon aquí el nombre de la escena por defecto

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var button = root.Q<Button>("back-to-menu-button");
        if (button != null)
        {
            button.clicked += () => SceneManager.LoadScene(sceneToLoad);
        }
    }
}