using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

public class BarraVida : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image healthBar;
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private int playerId = 1;
    private string nickname ="playerOne";  // ID del jugador en la base de datos

    // Referencia al sistema de part�culas
    public ParticleSystem healingEffect; // Referencia al sistema de part�culas
    public AudioClip healSound;  // Referencia al sonido de curaci�n
    private AudioSource audioSource;  // Componente de AudioSource para reproducir el sonido

    void Start()
    {
        currentHealth = maxHealth;
        ActualizarBarraVida(currentHealth, maxHealth);

        // Obtener el componente AudioSource en el GameObject del jugador (John)
        audioSource = GetComponent<AudioSource>();  // Aseg�rate de que tienes un AudioSource en el GameObject
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(UseHealthPotion());
        }
    }

    // M�todo que se llama cuando el jugador usa una poci�n
    IEnumerator UseHealthPotion()
    {
        // Paso 1: Verificar si el jugador tiene pociones disponibles (buscar pociones en el inventario del jugador)
        string url = $"http://localhost:3000/inventario/{nickname}";  // URL para obtener el inventario del jugador
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

            if (inventory.Contains("\"id_item\":2"))  // Verifica si el inventario tiene el item con ID 2 (poci�n)
            {
                // Si se encuentra la poci�n, procedemos a eliminarla y curar al jugador
                Debug.Log("Poci�n encontrada, procediendo a curar...");
                yield return StartCoroutine(RemovePotionAndHeal());
            }
            else
            {
                Debug.LogWarning("No tienes pociones de vida en el inventario.");
            }
        }
        else
        {
            Debug.LogWarning("Inventario vac�o o no encontrado.");
        }
    }

    // M�todo para eliminar la poci�n y curar al jugador
    IEnumerator RemovePotionAndHeal()
    {
        string url = "http://localhost:3000/eliminarItemInventario";

        // Paso 2: Crear objeto JsonData para enviar los datos
        JsonData1 data = new JsonData1(playerId, 2, 1);  // Aseg�rate de que JsonData est� definida correctamente
        string jsonData1 = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(url, "DELETE");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData1);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error al eliminar la poci�n: " + request.error);
        }
        else
        {
            // Si la poci�n fue eliminada correctamente, procedemos a curar al jugador
            Debug.Log("Poci�n eliminada. Curando 50 de vida...");
            HealPlayer(50);
        }
    }

    // M�todo para curar al jugador, activar el efecto de curaci�n y reproducir el sonido
    void HealPlayer(int amount)
    {
        // Activar el efecto de curaci�n
        if (healingEffect != null)
        {
            healingEffect.Play(); // Reproduce el efecto de curaci�n
        }

        // Reproducir el sonido de curaci�n
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);  // Reproduce el sonido una vez
        }

        // Curar al jugador
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        ActualizarBarraVida(currentHealth, maxHealth);
        Debug.Log("Vida actual: " + currentHealth);
    }

    // M�todo para actualizar la barra de vida
    public void ActualizarBarraVida(int vidaActual, int vidaMaxima)
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)vidaActual / vidaMaxima;
        }
    }
}

[System.Serializable]
public class JsonData1
{
    public int player_id;
    public int item_id;
    public int quantity;

    // Constructor que toma los valores y los asigna a las propiedades
    public JsonData1(int id, int itemID, int number)
    {
        player_id = id;
        item_id = itemID;
        quantity = number;
    }
}
