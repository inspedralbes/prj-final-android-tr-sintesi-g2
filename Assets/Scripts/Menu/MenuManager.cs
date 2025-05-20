using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public UIDocument shopUI;
    public UIDocument gameMode;
    public UIDocument loadNewGameUI;
    public UIDocument DeleteGameUI;

    public UIDocument creditsUI;
    private NuevaPartida nuevaPartida;
    private CargarPartida cargarPartidaScript;
    private DeleteGame deleteGameScript;


    private void Start()
    {
        if (gameMode == null) Debug.LogError("Main Menu UI no está asignado!");
        if (loadNewGameUI == null) Debug.LogError("New Game/Load Game UI no está asignado!");
        if (DeleteGameUI == null) Debug.LogError("Delete UI no está asignado!");
        if (shopUI == null) Debug.LogError("Shop UI no está asignado!");

        VisualElement gameModeRoot = gameMode.rootVisualElement;
        VisualElement loadGameRoot = loadNewGameUI.rootVisualElement;
        VisualElement deleteGameRoot = DeleteGameUI.rootVisualElement;
        VisualElement shopRoot = shopUI.rootVisualElement;
        VisualElement creditsRoot = creditsUI.rootVisualElement;

        nuevaPartida = FindObjectOfType<NuevaPartida>();
        cargarPartidaScript = FindObjectOfType<CargarPartida>();
        deleteGameScript = FindObjectOfType<DeleteGame>();

        if (deleteGameScript == null)
        {
            Debug.LogError("No se encontró el script DeleteGame en la escena.");
        }

        CambiarPantalla(gameMode);

        // Botones
        Button btnShop = loadGameRoot.Q<Button>("ShopButton");
        if (btnShop != null) btnShop.clicked += () => CambiarPantalla(shopUI);

        Button btnCredits = gameModeRoot.Q<Button>("CreditsButton");
        if (btnCredits != null) btnCredits.clicked += () => CambiarPantalla(creditsUI);

        
        Button btnBackShop = shopRoot.Q<Button>("shop-back-button");
        if (btnBackShop != null) btnBackShop.clicked += () => CambiarPantalla(loadNewGameUI);

        Button btnSinglePlayer = gameModeRoot.Q<Button>("SinglePlayerButton");
        if (btnSinglePlayer != null) btnSinglePlayer.clicked += () => CambiarPantalla(loadNewGameUI);

        Button btnLoadGame = loadGameRoot.Q<Button>("LoadGameButton");
        if (btnLoadGame != null) btnLoadGame.clicked += () => CargarPartida();

        Button btnNewGame = loadGameRoot.Q<Button>("NewGameButton");
        if (btnNewGame != null) btnNewGame.clicked += () => IniciarNuevaPartida();

        Button btnDeleteGame = loadGameRoot.Q<Button>("DeleteGameButton");
        if (btnDeleteGame != null) btnDeleteGame.clicked += () => CambiarPantalla(DeleteGameUI);

        Button btnExit = loadGameRoot.Q<Button>("ExitButton");
        if (btnExit != null) btnExit.clicked += () => CambiarPantalla(gameMode);

        Button btnAcceptDelete = deleteGameRoot.Q<Button>("acceptButton");
        if (btnAcceptDelete != null) btnAcceptDelete.clicked += () => ConfirmarBorrarPartida();

        Button btnCancelDelete = deleteGameRoot.Q<Button>("declineButton");
        if (btnCancelDelete != null) btnCancelDelete.clicked += () => CambiarPantalla(loadNewGameUI);
    }

    private void IniciarNuevaPartida()
    {
        if (nuevaPartida != null)
        {
            nuevaPartida.nickname = PlayerPrefs.GetString("nickname", "No encontrado");
            nuevaPartida.gameName = "Nueva Partida";

            PlayerPrefs.SetString("nickname", nuevaPartida.nickname);
            PlayerPrefs.SetString("gameName", nuevaPartida.gameName);
            PlayerPrefs.SetFloat("position_x", -60f);
            PlayerPrefs.SetFloat("position_y", 12f);
            PlayerPrefs.SetInt("health", 100);
            PlayerPrefs.SetInt("coins", 0);
            PlayerPrefs.Save();

            Debug.Log("Datos de la partida guardados:");
            Debug.Log($"Nickname: {nuevaPartida.nickname}");
            Debug.Log($"Game Name: {nuevaPartida.gameName}");
            Debug.Log($"Position X: -60f, Position Y: 12f");
            Debug.Log($"Health: 100, Coins: 0");

            nuevaPartida.StartNewGame();
        }

        CargarEscena("cinematic");
    }

    private void ConfirmarBorrarPartida()
    {
        string nickname = PlayerPrefs.GetString("nickname", "");
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("No hay un usuario logueado para borrar la partida.");
            return;
        }

        if (deleteGameScript != null)
        {
            deleteGameScript.DeletePlayerGame(nickname, (success) =>
            {
                if (success)
                {
                    Debug.Log("Partida eliminada con éxito, volviendo al menú principal.");
                    CambiarPantalla(loadNewGameUI);
                }
                else
                {
                    Debug.LogError("No se pudo eliminar la partida.");
                }
            });
        }
    }

    private void CargarPartida()
    {
        string nickname = PlayerPrefs.GetString("nickname", "");
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("No hay un usuario logueado para cargar una partida.");
            return;
        }

        if (cargarPartidaScript != null)
        {
            Debug.Log("Cargando partida para el usuario: " + nickname);
            cargarPartidaScript.StartLoadGame(nickname);
        }
        else
        {
            Debug.LogError("No se encontró el script CargarPartida.");
        }
    }

    public void CambiarPantalla(UIDocument nuevaPantalla)
    {
        gameMode.rootVisualElement.style.display = DisplayStyle.None;
        loadNewGameUI.rootVisualElement.style.display = DisplayStyle.None;
        DeleteGameUI.rootVisualElement.style.display = DisplayStyle.None;
        shopUI.rootVisualElement.style.display = DisplayStyle.None;
        creditsUI.rootVisualElement.style.display = DisplayStyle.None;

        nuevaPantalla.rootVisualElement.style.display = DisplayStyle.Flex;

        Debug.Log("Pantalla cambiada a: " + nuevaPantalla.name);
    }

    private void CargarEscena(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log($"Escena cambiada a: {sceneName}");
    }
}
