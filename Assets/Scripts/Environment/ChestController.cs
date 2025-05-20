using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System.Collections;

public class ChestController : MonoBehaviour
{
    public GameObject itemPrefab;       // Prefab para el ítem que se dropea
    public AudioClip chestSound;        // Sonido de apertura del cofre
    private AudioSource audioSource;
    private Animator chestAnimator;
    private bool isOpen = false;
    private bool isPlayerNear = false;

    private int playerId = 1;  // Suponemos que el ID del jugador es 1 (esto lo puedes obtener de tu lógica de juego)

    // Evento para notificar cuando el ítem haya sido dropeado
    public delegate void ItemDropped();
    public static event ItemDropped OnItemDropped;

    private void Start()
    {
        chestAnimator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Verificamos si el AudioSource tiene el clip de sonido
        if (audioSource != null && chestSound != null)
        {
            audioSource.clip = chestSound;
            audioSource.playOnAwake = false;  // Evitamos que suene automáticamente al inicio
        }
    }

    private void Update()
    {
        // Si el jugador está cerca y presiona la tecla "E", y el cofre no está abierto
        if (isPlayerNear && Keyboard.current.eKey.wasPressedThisFrame && !isOpen)
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        if (isOpen) return;  // Evita que el cofre se abra más de una vez

        // Activa la animación de apertura
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("OpenChest");
        }

        // Reproduce el sonido de apertura del cofre
        if (audioSource != null && chestSound != null)
        {
            audioSource.Play();
        }

        isOpen = true;  // Marcamos el cofre como abierto

        // Inicia la corutina para dropear el ítem después de un breve retraso (sincronizado con la animación)
        StartCoroutine(HandleItemDrop());
    }

    private IEnumerator HandleItemDrop()
    {
        yield return new WaitForSeconds(1f);  // Espera 1 segundo para que termine la animación (ajústalo según tu animación)
        DropItem();
        UnityEngine.Debug.Log("Item dropeado.");
    }

    private void DropItem()
    {
        // Creamos el ítem en el mundo (en el suelo) con física
        StartCoroutine(GetItemFromDatabase(2));  // Obtener el ítem desde la base de datos (ID 2: Poción de Vida)
    }

    private IEnumerator GetItemFromDatabase(int itemId)
    {
        string url = $"http://localhost:3003/item/{itemId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Item item = JsonUtility.FromJson<Item>(jsonResponse);
            UnityEngine.Debug.Log("Item ID: " + item.id_item);  // Verifica que este ID no sea nulo o inválido
            if (item.id_item == 0)
            {
                UnityEngine.Debug.LogError("Error: El ID del ítem es 0.");
                yield break;
            }
            InstantiateItem(item);

            // Disparamos el evento de que el ítem fue dropeado
            OnItemDropped?.Invoke();
        }
        else
        {
            UnityEngine.Debug.LogError("Error al obtener el ítem desde la base de datos: " + request.error);
        }
    }

    private void InstantiateItem(Item item)
    {
        if (itemPrefab != null)
        {
            // Creamos la poción a cierta distancia hacia arriba para simular que es "escupida" desde el cofre
            GameObject droppedItem = Instantiate(itemPrefab, transform.position + new Vector3(0, 1, 0), Quaternion.identity);

            // Agregamos física para que la poción caiga
            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Debug.Log("drop item force");
                // Añadir una fuerza aleatoria más fuerte para que la poción se "escupa" más fuerte y hacia un lado aleatorio
                float forceX = UnityEngine.Random.Range(-5f, 5f);  // Dirección aleatoria en el eje X
                float forceY = UnityEngine.Random.Range(8f, 12f);  // Fuerza significativa en el eje Y para mayor altura

                rb.AddForce(new Vector2(forceX, forceY), ForceMode2D.Impulse);  // Aplicamos la fuerza
               
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            UnityEngine.Debug.Log("Jugador cerca del cofre.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            UnityEngine.Debug.Log("Jugador ha salido del área del cofre.");
        }
    }
}

[System.Serializable]
public class Item
{
    public int id_item;
    public string item_name;
    public string item_description;
    public string item_type;
    public int value;
    public string rarity;
    public string item_image;
}
