using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;  // Importa para gestionar escenas
public class MenuManager : MonoBehaviour
{
    public UIDocument initialScreenUI;
    public UIDocument loginUI;
    public UIDocument registerUI;
    public UIDocument gameMode;
    public UIDocument loadNewGameUI;
    public UIDocument DeleteGameUI;
    private Login loginScript;
    private NuevaPartida nuevaPartida;
    private CargarPartida cargarPartidaScript;
    private DeleteGame deleteGameScript; // Asegúrate de tener la referencia a DeleteGame

    private void Start()
    {
        // Inicializar las referencias de UI
        if (initialScreenUI == null) Debug.LogError("Initial Screen UI no está asignado!");
        if (loginUI == null) Debug.LogError("Login UI no está asignado!");
        if (registerUI == null) Debug.LogError("Register UI no está asignado!");
        if (gameMode == null) Debug.LogError("Main Menu UI no está asignado!");
        if (loadNewGameUI == null) Debug.LogError("New Game/Load Game no está asignado!");
        if (DeleteGameUI == null) Debug.LogError("Delete UI no esta asignado!");

        // Inicialización de los objetos visuales de la UI
        VisualElement initialScreenRoot = initialScreenUI.rootVisualElement;
        VisualElement loginRoot = loginUI.rootVisualElement;
        VisualElement registerRoot = registerUI.rootVisualElement;
        VisualElement gameModeRoot = gameMode.rootVisualElement;
        VisualElement loadGameRoot = loadNewGameUI.rootVisualElement;
        VisualElement deleteGameRoot = DeleteGameUI.rootVisualElement;

        // Inicialización de scripts
        loginScript = FindObjectOfType<Login>();
        if (loginScript != null)
        {
            loginScript.OnLoginResult += VerificarLoginYEntrar;
        }
        else
        {
            Debug.LogError("No se encontró el script de Login.");
        }

        nuevaPartida = FindObjectOfType<NuevaPartida>();
        cargarPartidaScript = FindObjectOfType<CargarPartida>();
        
        // Asegúrate de inicializar correctamente DeleteGame
        deleteGameScript = FindObjectOfType<DeleteGame>();
        if (deleteGameScript == null)
        {
            Debug.LogError("No se encontró el script DeleteGame en la escena.");
        }

        CambiarPantalla(initialScreenUI);

        // Configuración de los botones
        Button btnNext = initialScreenRoot.Q<Button>("nextButton");
        if (btnNext != null) btnNext.clicked += () => CambiarPantalla(loginUI);

        Button btnGoToRegister = loginRoot.Q<Button>("goesToRegisterScreen");
        if (btnGoToRegister != null) btnGoToRegister.clicked += () => CambiarPantalla(registerUI);

        Button btnSinglePlayer = gameModeRoot.Q<Button>("SinglePlayerButton");
        if (btnSinglePlayer != null) btnSinglePlayer.clicked += () => CambiarPantalla(loadNewGameUI);

        Button btnLoadGame = loadGameRoot.Q<Button>("LoadGameButton");
        if (btnLoadGame != null) btnLoadGame.clicked += () => CargarPartida();

        Button btnNewGame = loadGameRoot.Q<Button>("NewGameButton");
        if (btnNewGame != null) btnNewGame.clicked += () => IniciarNuevaPartida();

        Button btnDeleteGame = loadGameRoot.Q<Button>("DeleteGameButton");
        if (btnDeleteGame != null)
        {
            btnDeleteGame.clicked += () => CambiarPantalla(DeleteGameUI);
        }

        Button btnExit = loadGameRoot.Q<Button>("ExitButton");
        if (btnExit != null)
        {
            btnExit.clicked += () => CambiarPantalla(loadNewGameUI);
        }


        Button btnAcceptDelete = deleteGameRoot.Q<Button>("acceptButton");
        if (btnAcceptDelete != null)
        {
            btnAcceptDelete.clicked += () => ConfirmarBorrarPartida();
        }

        Button btnCancelDelete = deleteGameRoot.Q<Button>("declineButton");
        if (btnCancelDelete != null) btnCancelDelete.clicked += () => CambiarPantalla(loadNewGameUI);
        
    }

    private void IniciarNuevaPartida()
    {
        if (nuevaPartida != null)
        {
            nuevaPartida.nickname = PlayerPrefs.GetString("nickname", "No encontrado");  // Si no hay nickname, se usará "Player" como predeterminado
            nuevaPartida.gameName = "Nueva Partida";

            PlayerPrefs.SetString("nickname", nuevaPartida.nickname);
            PlayerPrefs.SetString("gameName", nuevaPartida.gameName);
            PlayerPrefs.SetFloat("position_x", -60f);
            PlayerPrefs.SetFloat("position_y", 12f);
            PlayerPrefs.SetInt("health", 100);
            PlayerPrefs.SetInt("coins", 0);
            PlayerPrefs.Save();


            // Log de los datos guardados en PlayerPrefs
            Debug.Log("Datos de la partida guardados:");
            Debug.Log($"Nickname: {nuevaPartida.nickname}");
            Debug.Log($"Game Name: {nuevaPartida.gameName}");
            Debug.Log($"Position X: 0f, Position Y: 0f");
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

    // Aquí llamamos al método DeletePlayerGame de DeleteGame y pasamos el callback
    DeleteGame deleteGameScript = FindObjectOfType<DeleteGame>();
    if (deleteGameScript != null)
    {
        deleteGameScript.DeletePlayerGame(nickname, (success) =>
        {
            if (success)
            {
                Debug.Log("Partida eliminada con éxito, volviendo al menú principal.");
                CambiarPantalla(loadNewGameUI);  // Volver a la pantalla de "Cargar Nueva Partida"
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
        // Obtener el nickname desde PlayerPrefs
        string nickname = PlayerPrefs.GetString("nickname", "");  // Si no hay nickname guardado, se usará "" como predeterminado.

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("No hay un usuario logueado para cargar una partida.");
            return;
        }

        // Llamar al script CargarPartida para obtener los datos desde el servidor
        if (cargarPartidaScript != null)
        {
            Debug.Log("Cargando partida para el usuario: " + nickname);
            cargarPartidaScript.StartLoadGame(nickname);  // Pasar el nickname leído de PlayerPrefs
        }
        else
        {
            Debug.LogError("No se encontró el script CargarPartida.");
        }
    }



    public void CambiarPantalla(UIDocument nuevaPantalla)
    {
        if (nuevaPantalla == null)
        {
            Debug.LogError("nuevaPantalla es NULL, no se puede cambiar de pantalla.");
            return;
        }

        initialScreenUI.rootVisualElement.style.display = DisplayStyle.None;
        loginUI.rootVisualElement.style.display = DisplayStyle.None;
        registerUI.rootVisualElement.style.display = DisplayStyle.None;
        gameMode.rootVisualElement.style.display = DisplayStyle.None;
        loadNewGameUI.rootVisualElement.style.display = DisplayStyle.None;
        DeleteGameUI.rootVisualElement.style.display = DisplayStyle.None;

        nuevaPantalla.rootVisualElement.style.display = DisplayStyle.Flex;

        Debug.Log("Pantalla cambiada a: " + nuevaPantalla.name);
    }

    private void VerificarLoginYEntrar(bool loginExitoso)
    {
        if (loginExitoso)
        {
            Debug.Log("Login exitoso. Cambiando a la pantalla de juego.");
            CambiarPantalla(gameMode);
        }
        else
        {
            Debug.LogError("Error en el login, credenciales incorrectas.");
            // Aquí no hacemos nada más, no cambiamos de pantalla.
        }
    }


    private void CargarEscena(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log($"Escena cambiada a: {sceneName}");
    }



    private void BorrarPartida()
    {
        string nickname = PlayerPrefs.GetString("nickname", "");
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("No hay un usuario logueado para borrar la partida.");
            return;
        }

        DeleteGame deleteGameScript = FindObjectOfType<DeleteGame>();
        if (deleteGameScript != null)
        {
            deleteGameScript.DeletePlayerGame(nickname);
        }
        else
        {
            Debug.LogError("No se encontró el script DeleteGame.");
        }
    }


    private void MostrarError(string mensaje)
    {
        var root = loginUI.rootVisualElement;
        var errorLabel = root.Q<Label>("ErrorLabel"); // Asumiendo que tienes un Label con este nombre

        if (errorLabel != null)
        {
            errorLabel.text = mensaje;
            errorLabel.style.display = DisplayStyle.Flex; // Mostrar el label de error
        }
    }
}


