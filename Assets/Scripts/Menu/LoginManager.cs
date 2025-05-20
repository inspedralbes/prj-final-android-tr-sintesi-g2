using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public UIDocument initialScreenUI;
    public UIDocument loginUI;
    public UIDocument registerUI;

    private Login loginScript;

    void Start()
    {
        if (initialScreenUI == null) Debug.LogError("InitialScreen UI no asignado");
        if (loginUI == null) Debug.LogError("Login UI no asignado");
        if (registerUI == null) Debug.LogError("Register UI no asignado");

        VisualElement initialScreenRoot = initialScreenUI.rootVisualElement;
        VisualElement loginRoot = loginUI.rootVisualElement;
        VisualElement registerRoot = registerUI.rootVisualElement;

        loginScript = FindObjectOfType<Login>();
        if (loginScript != null)
        {
            loginScript.OnLoginResult += VerificarLoginYEntrar;
        }

        // Botones
        Button btnNext = initialScreenRoot.Q<Button>("nextButton");
        if (btnNext != null) btnNext.clicked += () => CambiarPantalla(loginUI);

        Button btnGoToRegister = loginRoot.Q<Button>("goesToRegisterScreen");
        if (btnGoToRegister != null) btnGoToRegister.clicked += () => CambiarPantalla(registerUI);

        Button btnBackToLogin = registerRoot.Q<Button>("backToLoginButton");
        if (btnBackToLogin != null) btnBackToLogin.clicked += () => CambiarPantalla(loginUI);


      
        CambiarPantalla(initialScreenUI);
    }

    private void VerificarLoginYEntrar(bool loginExitoso)
    {
        if (loginExitoso)
        {
            Debug.Log("Login exitoso. Cargando escena de menú principal.");
            SceneManager.LoadScene("Menu");
        }
        else
        {
            MostrarError("Credenciales incorrectas.");
        }
    }

    public void CambiarPantalla(UIDocument nuevaPantalla)
    {
        initialScreenUI.rootVisualElement.style.display = DisplayStyle.None;
        loginUI.rootVisualElement.style.display = DisplayStyle.None;
        registerUI.rootVisualElement.style.display = DisplayStyle.None;
        
        nuevaPantalla.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void MostrarError(string mensaje)
    {
        var root = loginUI.rootVisualElement;
        var errorLabel = root.Q<Label>("ErrorLabel");

        if (errorLabel != null)
        {
            errorLabel.text = mensaje;
            errorLabel.style.display = DisplayStyle.Flex;
        }
    }
}
