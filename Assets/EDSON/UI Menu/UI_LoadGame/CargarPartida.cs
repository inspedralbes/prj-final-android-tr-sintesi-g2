using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CargarPartida : MonoBehaviour
{
    private string servidorURL = "http://localhost:3000/loadGame/"; // Cambia por tu URL real

    public void StartLoadGame(string nickname)
    {   
        StartCoroutine(LoadGameFromServer(nickname));
    }

    private IEnumerator LoadGameFromServer(string nickname)
    {
        Debug.Log("Cargando partida para el jugador: " + nickname);
        string urlConNickname = servidorURL + nickname; // Usamos el nickname dinámico en la URL

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
                Debug.Log("Respuesta del servidor: " + jsonResponse);
                PartidaDatos datosPartida = JsonUtility.FromJson<PartidaDatos>(jsonResponse);
                Debug.Log("Datos de la partida cargados desde el backend:" + datosPartida.game.gameName);

                // Guardar los datos de la partida en PlayerPrefs
                GuardarDatosEnPlayerPrefs(datosPartida);
                SceneManager.LoadScene("MAPA TUTORIALL"); // Cargar la escena principal
            }
        }
    }

    private void GuardarDatosEnPlayerPrefs(PartidaDatos datos)
    {
    
        PlayerPrefs.SetString("gameName", datos.game.gameName); // Cambié para acceder a 'gameName' dentro de 'game'
        PlayerPrefs.SetFloat("position_x", datos.game.position_x);
        PlayerPrefs.SetFloat("position_y", datos.game.position_y);
        PlayerPrefs.SetInt("health", datos.game.health);
        PlayerPrefs.SetInt("coins", 10);
        PlayerPrefs.Save();

        Debug.Log("Datos de la partida cargados desde el backend:");
        Debug.Log($"Game Name: {datos.game.gameName}");
        Debug.Log($"Posición X: {datos.game.position_x}, Y: {datos.game.position_y}");
        Debug.Log($"Salud: {datos.game.health}, Monedas: {datos.game.coins}");
    }
}

[System.Serializable]
public class InventoryItemE
{
    public int id_item;
    public int quantity;
}

[System.Serializable]
public class Game
{
    public int gameId;
    public string gameName;
    public string gameStatus;
    public string totalProgress;
    public int timePlayed;
    public float position_x;  // Coordenada X
    public float position_y;  // Coordenada Y
    public int health;
    public int coins;
    public InventoryItemE[] inventory;  // Inventario
}

[System.Serializable]
public class PartidaDatos
{
    public string message;
    public Game game;
}

