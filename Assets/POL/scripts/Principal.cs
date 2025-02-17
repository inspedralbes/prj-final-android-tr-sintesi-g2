using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Principal : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        Button singlePlayerButton = root.Q<Button>("SinglePlayerButton");
        Button multiplayerButton = root.Q<Button>("MultiplayerButton");
        Button creditsButton = root.Q<Button>("CreditsButton");
        
        singlePlayerButton.clicked += () => LoadScene("CargarPartida");
        multiplayerButton.clicked += () => LoadScene("EnUnFuturo");
        creditsButton.clicked += () => LoadScene("EnUnFuturo");
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
