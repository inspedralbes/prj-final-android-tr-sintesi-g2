using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class BotonClick : MonoBehaviour
{
    // Método público para asignar en On Click
    public void VolverAMainMenu()
    {
        // Cerrar cualquier conexión de red existente
        if (NetworkClient.active)
        {
            NetworkClient.Disconnect();
        }

        // Si somos el servidor (host), detener el servidor
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }

        // Cargar la escena principal
        SceneManager.LoadScene("MainMenu");
    }
}
