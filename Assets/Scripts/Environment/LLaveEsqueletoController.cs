using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LlaveEsqueletoController : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string escenaASaltar = "NombreDeLaEscena"; // Nombre de la escena a cargar

    public int llaveId = 6; // ID del ítem "Llave Esqueleto" en la base de datos
    public int player_id; // ID del jugador
    public int quantity = 1; // Cantidad a añadir al inventario
    public GameObject pickupUI;
    public Animator llaveAnimator;  // Animator para la animación de desaparición
    public AudioClip pickUpItem;    // Sonido al recoger la llave
    private AudioSource audioSource; // Componente de audio para reproducir sonidos

    public bool isPlayerNearby = false;
    public bool hasTouchedGround = false; // Verificar si la llave tocó el suelo
    private Rigidbody2D rb;
    private Collider2D col;

    private void Start()
    {
        player_id = PlayerPrefs.GetInt("player_id");
// Ocultar el mensaje de recoger al inicio

        // Configura el componente AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Obtener el Rigidbody2D para controlar la física
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("No se encontró un Rigidbody2D en el objeto de la llave.");
        }

        // Obtener el Collider2D
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("No se encontró un Collider2D en el objeto de la llave.");
        }
    }

    private void Update()
    {
        // Detectar si el jugador presiona la tecla "E" para recoger la llave
        if (isPlayerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (player_id != 0)
            {
                StartCoroutine(AddLlaveToInventory(player_id, llaveId, quantity));
            }
            else
            {
                Debug.LogError("playerId es nulo o cero. No se puede agregar la llave al inventario.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            pickupUI.SetActive(true); // Mostrar el mensaje de recoger
        }

        if (other.CompareTag("Ground") && !hasTouchedGround)
        {
            Debug.Log("La llave ha tocado el suelo: " + other.name);
            hasTouchedGround = true;
            StopLlaveMovement(); // Detener el movimiento de la llave al tocar el suelo
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            pickupUI.SetActive(false); // Ocultar el mensaje de recoger
        }
    }

    private void StopLlaveMovement()
    {
        if (rb != null)
        {
            Debug.Log("Deteniendo el movimiento de la llave.");
            rb.linearVelocity = Vector2.zero;   // Detener la velocidad de la llave
            rb.angularVelocity = 0f;     // Detener la rotación
            rb.isKinematic = true;       // Desactivar la física para que la llave no se mueva más
        }

        if (col != null)
        {
            col.isTrigger = false; // Desactivar el trigger para que interactúe físicamente con el suelo
        }
    }

    IEnumerator AddLlaveToInventory(int player_id, int itemId, int quantity)
    {
        if (player_id == 0 || itemId == 0 || quantity <= 0)
        {
            Debug.LogError("Los valores de player_id, itemId o quantity no son válidos.");
            yield break;
        }

        // Reproducir sonido al recoger la llave
        if (pickUpItem != null && audioSource != null)
        {
            audioSource.clip = pickUpItem;
            audioSource.Play(); // Reproduce el sonido completo
            yield return new WaitForSeconds(pickUpItem.length); // Esperar a que termine el sonido
        }

        string url = "http://localhost:3003/addItem"; // URL de la API para agregar el ítem al inventario
        JsonData jsonData = new JsonData(player_id, itemId, quantity);
        string jsonString = JsonUtility.ToJson(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonString);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al agregar la llave al inventario: " + www.error);
            }
            else
            {
                Debug.Log("Llave añadida al inventario: " + www.downloadHandler.text);

                // Llamar a la animación de desaparición
                if (llaveAnimator != null)
                {
                    llaveAnimator.SetTrigger("Disappear");
                    yield return new WaitForSeconds(1f); // Esperar a que la animación termine
                }

                Destroy(gameObject); // Destruir el objeto después de recogerlo

                // Cargar la escena especificada
                if (!string.IsNullOrEmpty(escenaASaltar))
                {
                    
                    SceneManager.LoadScene(escenaASaltar);
                }
                else
                {
                    Debug.LogError("No se ha especificado una escena para cargar.");
                }
            }
        }
    }
}

[System.Serializable]
public class JsonData
{
    public int player_id;
    public int item_id;
    public int quantity;

    public JsonData(int id, int itemID, int number)
    {
        player_id = id;
        item_id = itemID;
        quantity = number;
    }
}