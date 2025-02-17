using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using System;

public class Login : MonoBehaviour
{
    private TextField nicknameField;
    private Button loginButton;
    private string apiUrl = "http://localhost:3000/loginPlayer"; 
    private MenuManager menuManager;

    public event Action<bool> OnLoginResult; // Evento para notificar el resultado del login

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        nicknameField = root.Q<TextField>("textNickname");
        loginButton = root.Q<Button>("loginButton");

        menuManager = FindObjectOfType<MenuManager>(); // Referencia a MenuManager

        // Intentar cargar el nickname desde PlayerPrefs si ya existe
        string savedNickname = PlayerPrefs.GetString("nickname", "");
        if (!string.IsNullOrEmpty(savedNickname))
        {
            nicknameField.value = savedNickname; // Rellenar el campo con el nickname guardado
        }

        if (loginButton != null)
        {
            loginButton.clicked += () => StartCoroutine(LoginPlayer());
        }
    }

   IEnumerator LoginPlayer()
{
    string nickname = nicknameField.value.Trim();
    if (string.IsNullOrEmpty(nickname))
    {
        Debug.LogWarning("Nickname vacío");
        OnLoginResult?.Invoke(false);
        yield break;
    }

    // Crear JSON con el nickname
    string jsonData = "{\"nickname\":\"" + nickname + "\"}";
    byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

    using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
    {
        www.uploadHandler = new UploadHandlerRaw(jsonToSend);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text; // Respuesta del servidor
            Debug.Log("Respuesta del servidor: " + responseText);

            if (!string.IsNullOrEmpty(responseText) && responseText != "Player no encontrado.")
            {
                // Guardar el nickname en PlayerPrefs
                PlayerPrefs.SetString("nickname", nickname);
                PlayerPrefs.Save(); // Forzar guardado inmediato

                // Verificar si se guardó correctamente
                string savedNickname = PlayerPrefs.GetString("nickname", "No guardado");
                Debug.Log($"Nickname guardado en PlayerPrefs: {savedNickname}");

                Debug.Log("Login exitoso.");
                OnLoginResult?.Invoke(true);
            }
            else
            {
                Debug.LogError("Error en el login, usuario no encontrado.");
                OnLoginResult?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError("Error en la petición: " + www.error);
            OnLoginResult?.Invoke(false);
        }
    }
}

}
