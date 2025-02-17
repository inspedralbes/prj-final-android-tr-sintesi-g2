using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Reflection.Emit;

public class InventoryManager : MonoBehaviour
{
    private VisualElement root;
    private VisualElement inventoryContainer;
    private VisualElement potionsContainer;
    private VisualElement todoElement; // Contenedor del inventario completo
    private const string baseUrl = "http://localhost:3000/inventario/";

    private const int MaxInventorySlots = 6;
    private const int MaxPotionSlots = 2;
    private int currentInventorySlot = 0;
    private int currentPotionSlot = 0;

    private bool isInventoryVisible = false;

    void Start()
{
    var uiDocument = GetComponent<UIDocument>();
    root = uiDocument.rootVisualElement;

    inventoryContainer = root.Q<VisualElement>("inventario");
    potionsContainer = root.Q<VisualElement>("pocionesSlots");
    todoElement = root.Q<VisualElement>("todo");

    if (todoElement != null)
    {
        todoElement.style.display = DisplayStyle.None;
    }

    string nickname = PlayerPrefs.GetString("nickname", "");
    if (!string.IsNullOrEmpty(nickname))
    {
        StartCoroutine(FetchInventoryData(nickname));
    }
    else
    {
        Debug.LogError("No se encontró el nickname en PlayerPrefs.");
    }
}

IEnumerator FetchInventoryData(string nickname)
{
    string apiUrl = baseUrl + UnityWebRequest.EscapeURL(nickname);
    UnityWebRequest request = UnityWebRequest.Get(apiUrl);
    yield return request.SendWebRequest();

    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("Error al obtener el inventario: " + request.error);
    }
    else
    {
        string jsonResponse = request.downloadHandler.text;
        InventoryItem[] inventoryItems = JsonHelper.FromJson<InventoryItem>(jsonResponse);
        Debug.Log("Inventario cargado con éxito.");
        PopulateInventoryUI(inventoryItems);
    }
}

    void Update()
    {
        // Detectar cuando se presiona la tecla "Esc"
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventoryVisibility();
        }
    }

    private void ToggleInventoryVisibility()
    {
        if (todoElement != null)
        {
            isInventoryVisible = !isInventoryVisible;
            todoElement.style.display = isInventoryVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    

    private void PopulateInventoryUI(InventoryItem[] inventoryItems)
    {
        Dictionary<int, int> itemCounts = new Dictionary<int, int>();  // Almacenar la cantidad de cada ítem por su ID

        foreach (var item in inventoryItems)
        {
            if (itemCounts.ContainsKey(item.id_item))
            {
                itemCounts[item.id_item] += item.quantity;
            }
            else
            {
                itemCounts[item.id_item] = item.quantity;
            }

            if (item.item_type == "llave" && currentInventorySlot < MaxInventorySlots)
            {
                Button itemButton = inventoryContainer.Q<Button>((currentInventorySlot + 1).ToString());
                if (itemButton != null)
                {
                    string imageUrl = "http://localhost:3000" + item.item_image;
                    StartCoroutine(LoadItemImage(imageUrl, itemButton));
                    AddQuantityLabel(itemButton, itemCounts[item.id_item]);  // Añadir etiqueta con cantidad
                    currentInventorySlot++;
                }
            }
            else if (item.item_type == "pocion" && currentPotionSlot < MaxPotionSlots)
            {
                Button potionButton = potionsContainer.Q<Button>("poc" + (currentPotionSlot + 1));
                if (potionButton != null)
                {
                    string imageUrl = "http://localhost:3000" + item.item_image;
                    StartCoroutine(LoadItemImage(imageUrl, potionButton));
                    AddQuantityLabel(potionButton, itemCounts[item.id_item]);  // Añadir etiqueta con cantidad
                    currentPotionSlot++;
                }
            }
        }
    }

    // Método para añadir la etiqueta con la cantidad de ítems
    private void AddQuantityLabel(Button itemButton, int quantity)
    {
        UnityEngine.UIElements.Label quantityLabel = new UnityEngine.UIElements.Label
        {
            text = quantity.ToString(),
            name = "quantityLabel"
        };

        quantityLabel.style.position = Position.Absolute;
        quantityLabel.style.right = 5;
        quantityLabel.style.bottom = 5;

        // Ajusta estas propiedades:
        quantityLabel.style.color = Color.yellow;             // Cambiar a un color más visible
        quantityLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        quantityLabel.style.fontSize = 18;                    // Aumenta el tamaño de fuente a 18 o más
        quantityLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f); // Más opacidad para que el texto destaque
        quantityLabel.style.paddingRight = 4;                 // Ajusta el padding para mayor espacio interno
        quantityLabel.style.paddingLeft = 4;
        quantityLabel.style.paddingTop = 2;
        quantityLabel.style.paddingBottom = 2;

        if (itemButton.Q<UnityEngine.UIElements.Label>("quantityLabel") == null)
        {
            itemButton.Add(quantityLabel);
        }
    }


    private IEnumerator LoadItemImage(string url, Button itemButton)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Error al cargar la imagen: " + request.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            itemButton.style.backgroundImage = new StyleBackground(texture);
            itemButton.style.backgroundColor = new StyleColor(Color.clear);

            itemButton.style.width = 125;
            itemButton.style.height = 125;

            if (inventoryContainer.Contains(itemButton))
            {
                itemButton.style.marginLeft = 20;
            }
        }
    }
}

// Clase que representa un ítem del inventario
[System.Serializable]
public class InventoryItem
{
    public int id_item;
    public string item_name;
    public string item_description;
    public string item_type;
    public int value;
    public string rarity;
    public string item_image;
    public int quantity;
}

// Clase auxiliar para deserializar el JSON
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}