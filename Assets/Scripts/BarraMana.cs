using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

public class BarraMana : MonoBehaviour
{
    [SerializeField] private Image barraImagen; // La imagen que representa la barra de maná
    [SerializeField] private Color manaLleno = new Color(0.2f, 0.4f, 1f); // Color cuando el maná está lleno
    [SerializeField] private Color manaVacio = new Color(0.1f, 0.2f, 0.5f); // Color cuando el maná está vacío
    [SerializeField] private float velocidadTransicion = 2f; // Velocidad de la transición de la barra
    [SerializeField] private int maxMana = 100;
    private int currentMana;
    private int playerId;
    private string nickname;
    
    // Referencia al PlayerController
    private PlayerController playerController;

    // Referencia al sistema de partículas
    public ParticleSystem manaRestorationEffect; // Referencia al sistema de partículas
    public AudioClip manaRestoreSound;  // Referencia al sonido de restauración de maná
    private AudioSource audioSource;  // Componente de AudioSource para reproducir el sonido
    
    private float manaObjetivo;
    private float manaNormalizado;
    
    void Awake()
    {
        // Si no se ha asignado la imagen en el inspector, intentar encontrarla
        if (barraImagen == null)
        {
            barraImagen = GetComponent<Image>();
            
            if (barraImagen == null)
            {
                Debug.LogError("No se encontró ningún componente Image para la barra de maná");
            }
        }
        
        currentMana = maxMana;
        ActualizarBarraMana(currentMana, maxMana); // Inicializar al 100%
    }
    
    void Start()
    {
        // Obtener el componente AudioSource en el GameObject del jugador
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            // Si no existe un AudioSource, lo añadimos
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Obtener datos reales del jugador desde PlayerPrefs
        playerId = PlayerPrefs.GetInt("player_id");
        nickname = PlayerPrefs.GetString("nickname");
        
        // Encontrar el PlayerController en la escena
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("No se encontró el PlayerController en la escena. La poción de maná no funcionará correctamente.");
        }
    }
    
    void Update()
    {
        // Hacer que la barra se actualice suavemente
        if (manaNormalizado != manaObjetivo)
        {
            manaNormalizado = Mathf.MoveTowards(manaNormalizado, manaObjetivo, velocidadTransicion * Time.deltaTime);
            barraImagen.fillAmount = manaNormalizado;
        }
        
        // Detector de tecla para usar poción de maná (tecla 2)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(UseManaPotion());
        }
    }
    
    // Método público para actualizar la barra de maná
    public void ActualizarBarraMana(int manaActual, int manaMaximo)
    {
        // Actualizar el valor actual del maná
        currentMana = manaActual;
        
        // Calcular el porcentaje de maná normalizado
        manaObjetivo = (float)manaActual / manaMaximo;
        
        // Actualizar inmediatamente si es la primera vez
        if (barraImagen.fillAmount == 0)
        {
            manaNormalizado = manaObjetivo;
            barraImagen.fillAmount = manaNormalizado;
        }
        
        // Cambiar el color de la barra según el nivel de maná
        barraImagen.color = Color.Lerp(manaVacio, manaLleno, manaObjetivo);
    }
    
    // Método que se llama cuando el jugador usa una poción de maná
    IEnumerator UseManaPotion()
    {
        // Verificar si el jugador tiene pociones de maná disponibles
        string url = $"http://localhost:3003/inventory/{nickname}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error al obtener el inventario: " + request.error);
            yield break;
        }

        if (!string.IsNullOrEmpty(request.downloadHandler.text))
        {
            // Suponiendo que el servidor devuelve un listado de objetos en el inventario en formato JSON
            string inventory = request.downloadHandler.text;
            Debug.Log("Inventario recibido: " + inventory);

            if (inventory.Contains("\"id_item\":3"))  // Verifica si el inventario tiene el item con ID 3 (poción de maná)
            {
                // Si se encuentra la poción, procedemos a eliminarla y restaurar maná al jugador
                Debug.Log("Poción de maná encontrada, procediendo a restaurar maná...");
                yield return StartCoroutine(RemoveManaPotion());
            }
            else
            {
                Debug.LogWarning("No tienes pociones de maná en el inventario.");
            }
        }
        else
        {
            Debug.LogWarning("Inventario vacío o no encontrado.");
        }
    }

    // Método para eliminar la poción de maná y restaurar maná al jugador
    IEnumerator RemoveManaPotion()
    {
        string url = "http://localhost:3003/removeItem";

        // Crear objeto JsonData para enviar los datos
        JsonDataMana data = new JsonDataMana(playerId, 3, 1);  // ID 3 para poción de maná
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(url, "DELETE");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error al eliminar la poción de maná: " + request.error);
        }
        else
        {
            // Si la poción fue eliminada correctamente, procedemos a restaurar el maná del jugador
            Debug.Log("Poción de maná eliminada. Restaurando 50 de maná...");
            RestoreMana(50);
        }
    }

    // Método para restaurar maná al jugador, activar el efecto visual y reproducir el sonido
    void RestoreMana(int amount)
    {
        // Activar el efecto de restauración de maná
        if (manaRestorationEffect != null)
        {
            manaRestorationEffect.Play();
        }

        // Reproducir el sonido de restauración de maná
        if (audioSource != null && manaRestoreSound != null)
        {
            audioSource.PlayOneShot(manaRestoreSound);
        }

        // Restaurar el maná del jugador
        currentMana += amount;
        if (currentMana > maxMana)
        {
            currentMana = maxMana;
        }

        ActualizarBarraMana(currentMana, maxMana);
        Debug.Log("Maná actual: " + currentMana);
        
        // Actualizar el maná en el PlayerController
        if (playerController != null)
        {
            playerController.RefillMana(currentMana);
            Debug.Log("Se ha actualizado el maná en el PlayerController: " + currentMana);
        }
        else
        {
            Debug.LogError("No se pudo actualizar el maná en el PlayerController. La referencia es nula.");
        }
    }
    
    // Método para obtener el maná actual
    public int GetCurrentMana()
    {
        return currentMana;
    }
    
    // Método para obtener el maná máximo
    public int GetMaxMana()
    {
        return maxMana;
    }
}

[System.Serializable]
public class JsonDataMana
{
    public int player_id;
    public int item_id;
    public int quantity;

    // Constructor que toma los valores y los asigna a las propiedades
    public JsonDataMana(int id, int itemID, int number)
    {
        player_id = id;
        item_id = itemID;
        quantity = number;
    }
}