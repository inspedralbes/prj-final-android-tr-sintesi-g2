 using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class DeleteGame : MonoBehaviour
{
    private string serverUrl = "http://localhost:3000/deleteGame/"; // Cambia esto por tu URL real

    // Llamamos a esta función desde el MenuManager para eliminar la partida
    public void DeletePlayerGame(string nickname, System.Action<bool> callback = null)
    {
        StartCoroutine(DeleteRequest(nickname, callback));
    }

    // Esta es la nueva versión de DeleteRequest que maneja la eliminación tanto en el servidor como en PlayerPrefs
    IEnumerator DeleteRequest(string nickname, System.Action<bool> callback = null)
    {
        // Primero eliminamos la partida desde el servidor
        string url = serverUrl + nickname;
        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            yield return www.SendWebRequest();

            // Si la eliminación del servidor fue exitosa, también eliminamos los datos en PlayerPrefs
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Partida eliminada con éxito del servidor");

                // Eliminar los datos de la partida desde PlayerPrefs
                PlayerPrefs.DeleteKey("nickname");
                PlayerPrefs.DeleteKey("gameName");
                PlayerPrefs.DeleteKey("position_x");
                PlayerPrefs.DeleteKey("position_y");
                PlayerPrefs.DeleteKey("health");
                PlayerPrefs.DeleteKey("coins");
                PlayerPrefs.Save();

                // Llamamos al callback, si se proporcionó, para indicar que todo ha ido bien
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError("Error al eliminar partida del servidor: " + www.error);

                // Llamamos al callback, si se proporcionó, para indicar que hubo un error
                callback?.Invoke(false);
            }
        }
    }
}
