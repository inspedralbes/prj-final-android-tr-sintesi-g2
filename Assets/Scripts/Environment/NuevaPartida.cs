using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena
using UnityEngine.UI;  // Para mostrar el mensaje de advertencia
using System.Collections;

public class NuevaPartida : MonoBehaviour
{
    private string apiUrl = "http://localhost:3002"; // Cambia esto por tu URL del backend
    public string nickname;
    public string gameName;
    private int gameId;  // El ID del juego ya no se asigna manualmente

    public GameObject warningMessage;  // Referencia al GameObject del mensaje de advertencia


    public void StartNewGame()
    {
        Debug.Log("Intentando guardar checkpoint para gameId: " + gameId);

        // Inicia directamente la creación de una nueva partida
        StartCoroutine(NewGameRequest());
    }

    private IEnumerator NewGameRequest()
    {
        NewGameData data = new NewGameData()
        {
            nickname = nickname,
            game_name = gameName,
            position_x = -60f,
            position_y = 12f,
            health = 100,
            coins = 0
        };
        Debug.Log("Datos de la nueva partida: " + JsonUtility.ToJson(data));

        string jsonData = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl + "/newGame", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Partida creada con éxito: " + request.downloadHandler.text);
                // Asignar el gameId desde la respuesta del servidor
                GameResponse response = JsonUtility.FromJson<GameResponse>(request.downloadHandler.text);
                if (response != null)
                {
                    gameId = response.gameId;  // Obtener el gameId de la respuesta
                    Debug.Log("ID del juego asignado: " + gameId);

                    // Cambiar de escena solo si la partida se crea correctamente
                    SceneManager.LoadScene("cinematic"); // Cambia "GameScene" al nombre de tu escena de juego
                }
                else
                {
                    Debug.LogError("La respuesta del servidor no contiene un gameId válido.");
                }
            }
            else
            {
                Debug.LogError("Error al crear partida: " + request.error);
                Debug.LogError("Respuesta del servidor: " + request.downloadHandler.text);  // Imprime la respuesta del servidor
            }
        }
    }



    // Método para mostrar el mensaje de advertencia
    private void ShowWarning(string message)
    {
        warningMessage.SetActive(true);  // Activa el objeto del mensaje de advertencia
        warningMessage.GetComponent<Text>().text = message;  // Establece el texto del mensaje
    }
}

[System.Serializable]
public class NewGameData
{
    public string nickname;
    public string game_name;
    public float position_x;
    public float position_y;
    public int health;
    public int coins;
}

[System.Serializable]
public class GameResponse
{
    public int gameId;  // Este campo debe coincidir con el nombre y tipo que devuelve el servidor
}

