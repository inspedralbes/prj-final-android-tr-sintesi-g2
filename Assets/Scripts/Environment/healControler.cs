using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private int playerId = 1;  // Cambia esto seg�n el ID del jugador en tu base de datos.

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(UseHealthPotion());
        }
    }

    IEnumerator UseHealthPotion()
    {
        string url = "http://localhost:3001/item/2";  // Endpoint para obtener el �tem con id_item = 2

        // Obtener el �tem (poci�n) del backend
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error al obtener la poci�n: " + request.error);
            yield break;
        }

        // Verificar si el jugador tiene la poci�n
        string jsonResponse = request.downloadHandler.text;
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            Debug.Log("Poci�n encontrada. Usando poci�n...");
            yield return StartCoroutine(RemovePotionAndHeal());
        }
        else
        {
            Debug.LogWarning("No tienes pociones de vida.");
        }
    }

    IEnumerator RemovePotionAndHeal()
    {
        string url = "http://localhost:3003/removeItem";  // Endpoint para eliminar el �tem del inventario
        WWWForm form = new WWWForm();
        form.AddField("player_id", 1);  // Cambia esto seg�n el ID del jugador
        form.AddField("item_id", 2);    // ID de la poci�n
        form.AddField("quantity", 1);   // Usar solo una poci�n

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error al eliminar la poci�n: " + request.error);
        }
        else
        {
            Debug.Log("Poci�n eliminada. Curando 50 de vida...");
            HealPlayer(50);
        }
    }

    void HealPlayer(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        Debug.Log("Vida actual: " + currentHealth);
    }
}
