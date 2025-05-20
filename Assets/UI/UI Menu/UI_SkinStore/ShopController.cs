using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System.Text;
using System;

[System.Serializable]
public class Skin
{
    public string skin_name;
    public float price;
}

[System.Serializable]
public class SkinList
{
    public List<Skin> skins = new List<Skin>();
}

[System.Serializable]
public class PurchasedSkinsResponse
{
    public List<string> purchasedSkins;
}

public class ShopController : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private string jsonEndpoint = "http://localhost:3009/shop/json/skins"; // URL del JSON
    [SerializeField] private string imageBaseUrl = "http://localhost:3009/imagenes/shop/Portadas/"; // URL base para las imágenes
    [SerializeField] private string purchasedSkinsEndpoint = "http://localhost:3001/player/purchasedskins/"; // Endpoint para skins compradas
    [SerializeField] private string addNewSkins = "http://localhost:3001/player/addnewskin"; // Endpoint para skins compradas
    [SerializeField] private string updateCoinsEndpoint = "http://localhost:3001/player/coins"; // Nuevo endpoint para actualizar coins

    private List<Skin> skins = new List<Skin>();
    private List<string> purchasedSkins = new List<string>(); // Lista de skins compradas
    private int currentSkinIndex = 0;

    private UIDocument shopUIDocument;

    private void Awake()
    {
        shopUIDocument = GetComponent<UIDocument>();
        if (shopUIDocument == null)
        {
            Debug.LogError("No se encontró un UIDocument en este GameObject. Asegúrate de que el componente UIDocument esté presente.");
        }
        Debug.Log($"CAntidad de dinero:"+ PlayerPrefs.GetInt("coins", 0));
    }

    private void Start()
    {
        SetupUI();
        StartCoroutine(InitializeShop());
        Debug.Log($"CAntidad de dinero:"+ PlayerPrefs.GetInt("coins", 0));
    }

    private void SetupUI()
    {
        if (shopUIDocument == null)
            return;

        var root = shopUIDocument.rootVisualElement;

        // Configurar botones de navegación
        var leftArrow = root.Q<Button>("left-arrow");
        var rightArrow = root.Q<Button>("right-arrow");
        leftArrow.clicked += ShowPreviousSkin;
        rightArrow.clicked += ShowNextSkin;

        // Configurar botón de acción
        var actionButton = root.Q<Button>("action-button");
        actionButton.clicked += OnActionButtonClicked;
    }

    private IEnumerator InitializeShop()
    {
        // Obtener el nickname desde PlayerPrefs
        string nickname = PlayerPrefs.GetString("nickname", null);

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("No se encontró un nickname guardado en PlayerPrefs.");
            yield break;
        }

        // Obtener las skins compradas por el jugador
        yield return FetchPurchasedSkins(nickname);

        // Cargar todas las skins disponibles desde el servidor
        yield return LoadSkinsFromServer();
    }

    private IEnumerator FetchPurchasedSkins(string nickname)
    {
        purchasedSkins.Clear();
        Debug.Log($"Cantidad de dinero:"+ PlayerPrefs.GetInt("coins", 0));

        using (UnityWebRequest purchasedSkinsRequest = UnityWebRequest.Get(purchasedSkinsEndpoint + nickname))
        {
            yield return purchasedSkinsRequest.SendWebRequest();

            if (purchasedSkinsRequest.result == UnityWebRequest.Result.Success)
            {
                // Leer el JSON y deserializarlo
                string responseText = purchasedSkinsRequest.downloadHandler.text;
                PurchasedSkinsResponse response = JsonUtility.FromJson<PurchasedSkinsResponse>(responseText);
                purchasedSkins = response.purchasedSkins;

                Debug.Log($"Se cargaron {purchasedSkins.Count} skins compradas para el jugador {nickname}.");
            }
            else
            {
                Debug.LogError("Error al cargar las skins compradas desde el servidor: " + purchasedSkinsRequest.error);
                Debug.LogError("purchasedSkinsEndpoint: " + purchasedSkinsEndpoint + nickname);
            }
        }
    }

    private IEnumerator LoadSkinsFromServer()
    {
        Debug.Log("Cargando datos de skins desde el servidor...");

        using (UnityWebRequest jsonRequest = UnityWebRequest.Get(jsonEndpoint))
        {
            yield return jsonRequest.SendWebRequest();

            if (jsonRequest.result == UnityWebRequest.Result.Success)
            {
                // Leer el JSON y deserializarlo
                string responseText = jsonRequest.downloadHandler.text;
                SkinList skinList = JsonUtility.FromJson<SkinList>(responseText);
                skins = skinList.skins;

                Debug.Log($"Se cargaron {skins.Count} skins desde el servidor.");

                // Mostrar la primera skin
                if (skins.Count > 0)
                {
                    ShowSkin(0);
                }
            }
            else
            {
                Debug.LogError("Error al cargar el archivo JSON desde el servidor: " + jsonRequest.error);
            }
        }
    }

    private void ShowSkin(int index)
    {
        if (skins == null || skins.Count == 0)
            return;

        currentSkinIndex = index;

        var root = shopUIDocument.rootVisualElement;

        // Obtener datos de la skin actual
        var skin = skins[currentSkinIndex];
        string imageUrl = $"{imageBaseUrl}{skin.skin_name}.jpg";

        // Actualizar elementos del UI Toolkit
        var skinImage = root.Q<Image>("skin-image");
        var skinNameLabel = root.Q<Label>("skin-name-label");
        var priceLabel = root.Q<Label>("price-label");
        var actionButton = root.Q<Button>("action-button");
        var coinsLabel = root.Q<Label>("coins-amount-label");

        // Descargar y mostrar la imagen
        StartCoroutine(LoadTextureFromUrl(imageUrl, texture =>
        {
            if (texture != null)
            {
                skinImage.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                Debug.LogWarning($"No se pudo cargar la imagen para la skin: {skin.skin_name}");
            }
        }));

        // Actualizar etiquetas
        skinNameLabel.text = skin.skin_name;
        priceLabel.text = "$" + skin.price.ToString("F2");
        coinsLabel.text = PlayerPrefs.GetInt("coins", 0).ToString();

        // Actualizar el botón según el estado de la skin
        UpdateActionButton(skin.skin_name, actionButton);
    }

    private void UpdateActionButton(string skinName, Button actionButton)
    {
        string selectedSkin = PlayerPrefs.GetString("selectedSkin", null);

        if (purchasedSkins.Contains(skinName))
        {
            if (selectedSkin == skinName)
            {
                // Si la skin está seleccionada
                actionButton.text = "Desequipar";
                actionButton.style.backgroundColor = new StyleColor(Color.red);
            }
            else
            {
                // Si la skin está comprada pero no seleccionada
                actionButton.text = "Equipar";
                actionButton.style.backgroundColor = new StyleColor(new Color(1.0f, 0.65f, 0.0f)); // Naranja
            }
        }
        else
        {
            // Si la skin no está comprada
            actionButton.text = "Comprar";
            actionButton.style.backgroundColor = new StyleColor(Color.green);
        }
    }

    private void ShowPreviousSkin()
    {
        if (skins.Count == 0)
            return;

        currentSkinIndex = (currentSkinIndex - 1 + skins.Count) % skins.Count;
        ShowSkin(currentSkinIndex);
    }

    private void ShowNextSkin()
    {
        if (skins.Count == 0)
            return;

        currentSkinIndex = (currentSkinIndex + 1) % skins.Count;
        ShowSkin(currentSkinIndex);
    }
    private IEnumerator SaveSelectedSkinToDatabase(string nickname, string selectedSkin)
    {
        Debug.Log($"Guardando skin seleccionada en la base de datos: nickname: {nickname}, selectedSkin: {selectedSkin}");
        
        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(selectedSkin))
        {
            Debug.LogError("El nickname o la skin seleccionada están vacíos.");
            yield break;
        }

        // Crear un objeto JSON con la información necesaria
        string jsonData = "{\"nickname\":\"" + nickname + "\", \"selectedSkin\":\"" + selectedSkin + "\"}";
        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        // Configurar la solicitud PUT
        using (UnityWebRequest www = new UnityWebRequest("http://localhost:3001/player/selectedskin", "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Enviar la solicitud
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Skin seleccionada guardada en la base de datos con éxito.");
                Debug.Log("Respuesta del servidor: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Error al guardar la skin seleccionada en la base de datos: {www.error}");
            }
        }
    }

    // Modificar OnActionButtonClicked para llamar a SaveSelectedSkinToDatabase
    private void OnActionButtonClicked()
    {
        if (skins.Count == 0)
            return;

        var selectedSkin = skins[currentSkinIndex];
        var root = shopUIDocument.rootVisualElement;
        var actionButton = root.Q<Button>("action-button");

        if (actionButton.text == "Equipada")
        {
            // No hacer nada si ya está equipada
            Debug.Log("La skin ya está equipada. No se realiza ninguna acción.");
            return;
        }
        else if (actionButton.text == "Equipar")
        {
            // Cambiar a "Equipada" y actualizar PlayerPrefs
            Debug.Log($"Equipando la skin: {selectedSkin.skin_name}");
            PlayerPrefs.SetString("selectedSkin", selectedSkin.skin_name);
            PlayerPrefs.Save();

            // Guardar la skin seleccionada en la base de datos
            StartCoroutine(SaveSelectedSkinToDatabase(PlayerPrefs.GetString("nickname", null), selectedSkin.skin_name));

            // Actualizar el botón a "Equipada"
            actionButton.text = "Equipada";
            actionButton.style.backgroundColor = new StyleColor(Color.green);

            ShowSkin(currentSkinIndex); // Refrescar la UI
        }
        else if (actionButton.text == "Comprar")
        {
            // Verificar si hay suficientes coins
            int coins = PlayerPrefs.GetInt("coins", 0);
            if (coins >= Mathf.CeilToInt(selectedSkin.price)) // Redondear precio si es necesario
            {
                // Restar el precio de los coins y guardar en PlayerPrefs
                coins -= Mathf.CeilToInt(selectedSkin.price);
                PlayerPrefs.SetInt("coins", coins);
                PlayerPrefs.Save();

                Debug.Log($"Skin comprada: {selectedSkin.skin_name}. Coins restantes: {coins}");

                // Actualizar la UI de coins y base de datos
                UpdateCoinsUI();
                StartCoroutine(UpdateCoinsInDatabase(PlayerPrefs.GetString("nickname", null), coins));

                // Llamar al método para guardar la skin en la base de datos
                StartCoroutine(SendSkinToDatabase(PlayerPrefs.GetString("nickname", null), selectedSkin.skin_name));
            }
            else
            {
                Debug.LogError("No tienes suficientes coins para comprar esta skin.");
            }
        }
    }
    private IEnumerator SendSkinToDatabase(string nickname, string skinName)
    {   
        Debug.Log($" ADDSKIN: nickname: {nickname}, skinName: {skinName}");
        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(skinName))
        {
            Debug.LogError("El nickname o el nombre de la skin están vacíos.");
            yield break;
        }

        // Crear un objeto JSON con la información necesaria
        string jsonData = "{\"nickname\":\"" + nickname + "\", \"skin\":\""+ skinName+"\"}";
        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        // Configurar la solicitud POST
        using (UnityWebRequest www = new UnityWebRequest(addNewSkins, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Enviar la solicitud
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Skin guardada en la base de datos con éxito.");
                Debug.Log("Respuesta del servidor: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Error al guardar la skin en la base de datos: {www.error}");
            }
        }
        // Actualizar la lista de skins compradas
        purchasedSkins.Add(skinName);
        Debug.Log($"Skin {skinName} añadida a la lista de skins compradas.");
        // Actualizar el botón de acción
        var root = shopUIDocument.rootVisualElement;
        var actionButton = root.Q<Button>("action-button"); 
        UpdateActionButton(skinName, actionButton);

    }   

    // NUEVO: Actualiza las coins en la base de datos usando el endpoint dado
    private IEnumerator UpdateCoinsInDatabase(string nickname, int coins)
    {
        Debug.Log($"Actualizando coins en la base de datos: nickname: {nickname}, coins: {coins}");
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("El nickname está vacío.");
            yield break;
        }

        // Crear JSON para el body
        string jsonData = "{\"nickname\":\"" + nickname + "\", \"coins\":" + coins + "}";
        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        // Configurar la solicitud PUT
        using (UnityWebRequest www = new UnityWebRequest(updateCoinsEndpoint, "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Enviar la solicitud
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Coins actualizadas en la base de datos con éxito.");
                Debug.Log("Respuesta del servidor: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Error al actualizar las coins en la base de datos: {www.error}");
            }
        }
    }

    private void UpdateCoinsUI()
    {
        var root = shopUIDocument.rootVisualElement;
        var coinsAmountLabel = root.Q<Label>("coins-amount-label");
        var coinsLabel = root.Q<Label>("coins-label"); // solo si tienes ambos en el UI

        int coins = PlayerPrefs.GetInt("coins", 0);

        if (coinsAmountLabel != null)
            coinsAmountLabel.text = coins.ToString();

        if (coinsLabel != null)
            coinsLabel.text = $"Coins: {coins}";
    }

    private IEnumerator LoadTextureFromUrl(string url, System.Action<Texture2D> callback)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                callback(texture);
            }
            else
            {
                Debug.LogError("Error al descargar la textura desde la URL: " + url + " - " + request.error);
                callback(null);
            }
        }
    }
}