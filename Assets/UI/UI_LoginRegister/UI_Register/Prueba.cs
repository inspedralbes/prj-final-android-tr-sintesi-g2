using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.Networking;

public class Prueba : MonoBehaviour
{
    private const string ApiUrl = "http://localhost:3001/player"; 
    public UIDocument loginUI;  // Referencia a la pantalla de login
    private LoginManager loginManager; // Referencia a loginManager

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        var textField = root.Q<TextField>("TextNickname");
        var registerButton = root.Q<Button>("Register");

        loginManager = FindObjectOfType<LoginManager>(); // Obtener referencia a loginManager

        if (registerButton != null && textField != null)
        {
            registerButton.clicked += () =>
            {
                string nickname = textField.value.Trim(); // Elimina espacios en blanco

                if (string.IsNullOrEmpty(nickname))
                {
                    Debug.LogWarning("El campo de nickname está vacío.");
                    return; // Evita continuar si el campo está vacío
                }

                RegisterPlayer(nickname);
            };
        }
    }

    private void RegisterPlayer(string nickname)
    {
        StartCoroutine(PostPlayer(nickname));
    }

    private IEnumerator PostPlayer(string nickname)
    {
        string json = $"{{\"nickname\":\"{nickname}\"}}";
        var request = new UnityWebRequest(ApiUrl, "POST");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Jugador registrado: {request.downloadHandler.text}");

            // Verificar si la respuesta es un éxito (código 201)
            if (request.responseCode == 201)
            {
                if (loginManager != null && loginUI != null)
                {
                    loginManager.CambiarPantalla(loginUI);
                }
                else
                {
                    Debug.LogError("loginManager o loginUI no están asignados.");
                }
            }
            else if (request.responseCode == 409) // Si el código de respuesta es 409, el nickname ya está registrado
            {
                Debug.LogError("El nickname ya está registrado. Por favor elige otro.");
                // Aquí puedes agregar lógica para mostrar un mensaje de error en la UI.
            }
            else
            {
                Debug.LogError("Registro fallido, respuesta inesperada del servidor.");
            }
        }
        else
        {
            if (request.responseCode == 500)  // Si el error es 500 (Internal Server Error)
            {
                Debug.LogError("Error en el servidor. Intenta más tarde.");
                // Aquí podrías mostrar un mensaje en la UI que indique que el servidor está fallando.
            }
            else
            {
                Debug.LogError($"Error al registrar jugador: {request.error}");
            }
        }
    }
}
