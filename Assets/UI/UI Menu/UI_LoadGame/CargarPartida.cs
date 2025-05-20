using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements; // Necesario para UI Toolkit

public class CargarPartida : MonoBehaviour
{
    private string servidorURL = "http://localhost:3002/loadGame/"; // Cambia por tu URL real

    [Header("Posición inicial por defecto (editable desde Inspector)")]
    [SerializeField] private float defaultPositionX = 0f;
    [SerializeField] private float defaultPositionY = 0f;

    private void Awake()
    {
        // Busca el UIDocument en el GameObject
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            var exitButton = root.Q<Button>("ExitButton");
            if (exitButton != null)
            {
                exitButton.clicked += () =>
                {
                    SceneManager.LoadScene("Menu");
                };
            }
        }
    }

    public void StartLoadGame(string nickname)
    {   
        Debug.Log("Cargando partida para el jugador: " + nickname);
        StartCoroutine(LoadGameFromServer(nickname));
    }

    private IEnumerator LoadGameFromServer(string nickname)
    {
        Debug.Log("Cargando partida para el jugador: " + nickname);
        string urlConNickname = servidorURL + nickname;

        using (UnityWebRequest www = UnityWebRequest.Get(urlConNickname))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al cargar la partida: " + www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Respuesta cruda del servidor: " + jsonResponse);

                // Deserializar directamente el objeto recibido
                ServerGameData datosPartida = JsonUtility.FromJson<ServerGameData>(jsonResponse);

                if (datosPartida == null)
                {
                    Debug.LogError("No se pudo deserializar la partida.");
                    yield break;
                }

                Debug.Log("Datos deserializados correctamente:");
                Debug.Log("Level: " + datosPartida.level);
                Debug.Log("position_x: " + datosPartida.position_x);
                Debug.Log("position_y: " + datosPartida.position_y);
                Debug.Log("health: " + datosPartida.health);
                Debug.Log("coins: " + datosPartida.coins);

                GuardarDatosEnPlayerPrefs(datosPartida);

                // Cargar la escena del nivel guardado
                if (!string.IsNullOrEmpty(datosPartida.level))
                {
                    Debug.Log("Cargando escena guardada: " + datosPartida.level);
                    SceneManager.LoadScene(datosPartida.level);
                }
                else
                {
                    Debug.LogWarning("Level vacío, cargando escena por defecto.");
                    SceneManager.LoadScene("MAPA TUTORIALL");
                }
            }
        }
    }

    private void GuardarDatosEnPlayerPrefs(ServerGameData datos)
    {
        if (datos == null)
        {
            Debug.LogError("No se pueden guardar datos nulos en PlayerPrefs.");
            return;
        }

        PlayerPrefs.SetString("gameName", datos.level); // opcional
        PlayerPrefs.SetFloat("position_x", datos.position_x);
        PlayerPrefs.SetFloat("position_y", datos.position_y);
        PlayerPrefs.SetInt("health", datos.health);
        PlayerPrefs.SetInt("coins", datos.coins);
        PlayerPrefs.SetString("current_scene", datos.level);
        PlayerPrefs.Save();

        Debug.Log("Datos guardados en PlayerPrefs:");
        Debug.Log($"Level: {datos.level}");
        Debug.Log($"Posición X: {datos.position_x}, Y: {datos.position_y}");
        Debug.Log($"Salud: {datos.health}, Monedas: {datos.coins}");
    }
}

// Clase que refleja el JSON real devuelto por el backend
[System.Serializable]
public class ServerGameData
{
    public int id;
    public int playerId;
    public string playerNickname;
    public float score;
    public int time;
    public string status;
    public string createdAt;
    public string level;
    public float position_x;
    public float position_y;
    public int health;
    public int coins;
}
