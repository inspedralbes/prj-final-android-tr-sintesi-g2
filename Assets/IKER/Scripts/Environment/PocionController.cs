using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

public class PocionController : MonoBehaviour
{
    public int potionId = 2;
    public int playerId = 1;
    public int quantity = 1;
    public GameObject pickupUI;
    public Animator potionAnimator;  // Referencia al Animator para la animación de desaparición
    public AudioClip pickUpItem;     // Referencia al sonido de la poción
    private AudioSource audioSource; // Referencia al componente AudioSource

    public bool isPlayerNearby = false;
    public bool hasTouchedGround = false;
    private Rigidbody2D rb;

    // Variable para saber cuándo el ítem se ha dropeado
    private bool isDropped = false;

    // Tiempo de "respawn" de los ítems
    public float respawnTime = 5f; // Tiempo en segundos para que el ítem vuelva a aparecer

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pickupUI.SetActive(false);

        // Obtener o añadir un componente AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Suscribirse al evento OnItemDropped
        ChestController.OnItemDropped += OnItemDropped;
    }

    void OnDestroy()
    {
        // Desuscribirse del evento cuando el objeto se destruye
        ChestController.OnItemDropped -= OnItemDropped;
    }

    void Update()
    {
        if (isPlayerNearby && Keyboard.current.eKey.wasPressedThisFrame && !isDropped)
        {
            if (playerId != 0)
            {
                StartCoroutine(AddPotionToInventory(playerId, potionId, quantity));
                isDropped = true; // Marcamos que la poción ha sido recogida
            }
            else
            {
                Debug.LogError("playerId es nulo o cero. No se puede agregar la poción al inventario.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            pickupUI.SetActive(true);
        }

        if (other.CompareTag("Ground"))
        {/*
            Debug.Log(JsonUtility.ToJson(other));*/
           /* Debug.Log("CompareTag ground");
            if (isPlayerNearby)
            {*/
                Debug.Log("compare to ground" + other.tag +":"+other.name+":"+other.gameObject.name);
                hasTouchedGround = true;
                StopPotionMovement();  // Detener movimiento cuando toque el suelo
         /*   }*/
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            pickupUI.SetActive(false);
        }
    }

    private void StopPotionMovement()
    {
        if (rb != null)
        {
            Debug.Log("Stop Potion Movement.");
            rb.linearVelocity = Vector2.zero;  // Detener el movimiento de la poción
            rb.isKinematic = true;       // Desactivar la física para que la poción no se mueva más
        }
    }

    private void OnItemDropped()
    {
        // Aseguramos que la poción se haya lanzado antes de permitir la recogida
        Debug.Log("Item has been dropped, now the player can interact with it.");
        isDropped = false;  // Reiniciamos la variable isDropped para que el jugador pueda interactuar nuevamente
    }

    IEnumerator AddPotionToInventory(int playerId, int itemId, int quantity)
    {
        if (playerId == 0 || itemId == 0 || quantity <= 0)
        {
            Debug.LogError("Los valores de playerId, itemId o quantity no son válidos.");
            yield break;
        }

        // Reproducir sonido al recoger la poción
        if (pickUpItem != null && audioSource != null)
        {
            audioSource.clip = pickUpItem;
            audioSource.Play(); // Reproduce el sonido completo
            yield return new WaitForSeconds(pickUpItem.length);  // Espera a que termine el sonido
        }

        string url = "http://localhost:3000/agregarItemInventario";
        JsonData jsonData = new JsonData(playerId, itemId, quantity);
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
                Debug.LogError("Error al agregar el ítem al inventario: " + www.error);
            }
            else
            {
                Debug.Log("Ítem añadido al inventario: " + www.downloadHandler.text);

                // Llamamos a la animación de desaparición
                if (potionAnimator != null)
                {
                    potionAnimator.SetTrigger("Disappear");
                    yield return new WaitForSeconds(1f);  // Esperamos a que la animación termine (ajusta el tiempo si es necesario)
                }

                Destroy(gameObject);  // Destruye el objeto después de la animación

                // Respawn del ítem después de un tiempo (simula que el ítem aparece nuevamente)
                yield return new WaitForSeconds(respawnTime);

                // Aquí es donde podemos respawnear un nuevo ítem, o bien el cofre puede lanzar un nuevo ítem
                Debug.Log("El ítem respawnó.");
                RespawnItem();  // Método para crear un nuevo ítem
            }
        }
    }

    private void RespawnItem()
    {
        // Respawn del ítem en la misma posición donde se destruyó
        // Puedes ajustar la posición según sea necesario
        Instantiate(gameObject, transform.position, transform.rotation);
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
