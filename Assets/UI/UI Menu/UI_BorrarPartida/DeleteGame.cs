using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class DeleteGame : MonoBehaviour
{
    public CargarPartidaAsset cargarPartidaAsset;
    private string serverUrl = "http://localhost:3002/deleteGame/"; // Cambia esto por tu URL real

    // Llamamos a esta función desde el MenuManager para eliminar la partida
    public void DeletePlayerGame(string nickname, System.Action<bool> callback = null)
    {
        StartCoroutine(DeleteRequest(nickname, callback));
    }

    // Esta es la nueva versión de DeleteRequest que maneja la eliminación tanto en el servidor como en PlayerPrefs
    IEnumerator DeleteRequest(string nickname, System.Action<bool> callback = null)
    {
        string url = serverUrl + nickname;
        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Partida eliminada con éxito del servidor");

                PlayerPrefs.DeleteKey("nickname");
                PlayerPrefs.DeleteKey("gameName");
                PlayerPrefs.DeleteKey("position_x");
                PlayerPrefs.DeleteKey("position_y");
                PlayerPrefs.DeleteKey("health");
                PlayerPrefs.DeleteKey("coins");
                PlayerPrefs.Save();

                // Espera a cargar la info nueva
                // if (cargarPartidaAsset != null)
                //     yield return StartCoroutine(cargarPartidaAsset.LoadLastGameCoroutine(nickname));

                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError("Error al eliminar partida del servidor: " + www.error);
                callback?.Invoke(false);
            }
        }
    }
}