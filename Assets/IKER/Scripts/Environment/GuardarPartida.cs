using UnityEngine;
using UnityEngine.Networking;
using System.Collections;


public class GuardarPartida : MonoBehaviour
{

    void Start()
    {
        string nickname = PlayerPrefs.GetString("nickname", "No encontrado");
        Debug.Log("Nickname en la nueva escena: " + nickname);
    }

    private string apiUrl = "http://localhost:3000"; // Cambia esto si usas otra URL

    public void SaveCheckpoint(float x, float y, int health, int coins)
    {
        // Recuperar el nickname almacenado en PlayerPrefs
        string nickname = PlayerPrefs.GetString("nickname", "");
        Debug.Log("Guardando checkpoint para: " + nickname);

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("No se encontró un nickname guardado en PlayerPrefs.");
            return;
        }

        StartCoroutine(SaveCheckpointRequest(nickname, x, y, health, coins));
    }

    private IEnumerator SaveCheckpointRequest(string nickname, float x, float y, int health, int coins)
    {
        Debug.Log("Guardando checkpoint para: " + nickname + " en la posición (" + x + ", " + y + ")");
        var data = new CheckpointData()
        {
            game_name = "Nombre del juego",   // Asegúrate de enviar el nombre correcto si es necesario
            game_status = "active",           // Establecer el estado del juego
            total_progress = 50.00f,          // Progreso total (por ejemplo, 50%)
            time_played = 120,                // Tiempo jugado en segundos
            position_x = x,
            position_y = y,
            health = health,
            coins = coins
        };

        string jsonData = JsonUtility.ToJson(data);
        Debug.Log("Datos JSON a enviar: " + jsonData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl + "/updateGame/" + nickname, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Checkpoint guardado con éxito.");
                Debug.Log("Respuesta del servidor: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error al guardar checkpoint: " + request.result);
                Debug.LogError("Error al guardar checkpoint: " + UnityWebRequest.Result.Success);
                Debug.LogError("Error al guardar checkpoint: " + request.error);
                Debug.LogError("Respuesta del servidor: " + request.downloadHandler.text);
            }
        }
    }
}

[System.Serializable]
public class CheckpointData
{
    public string game_name;
    public string game_status;
    public float total_progress;
    public int time_played;
    public float position_x;
    public float position_y;
    public int health;
    public int coins;
}
