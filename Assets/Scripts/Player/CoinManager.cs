using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI coinText;
    [SerializeField] private string coinsUpdateEndpoint = "https://tuservidor.com/player/coins"; // Cambia por tu endpoint real

    private string playerNickname;
    private int totalCoins = 0;

    private void Awake()
    {
        playerNickname = PlayerPrefs.GetString("nickname", "");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Cargar el valor temporal desde PlayerPrefs
        totalCoins = PlayerPrefs.GetInt("temp_coins", 0);
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        UpdateCoinUI();
        PlayerPrefs.SetInt("temp_coins", totalCoins); // Guardar cada vez que sumas
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = totalCoins.ToString();
        }
        else
        {
            Debug.LogWarning("¡No se ha asignado CoinText en el inspector!");
        }
    }

    public int GetTotalCoins()
    {
        return totalCoins;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si la escena NO es un menú, guarda el valor de temp_coins en coins y en la base de datos
        if (!scene.name.ToLower().Contains("menu"))
        {
            SaveTempCoinsAsCoins();
        }
    }

    private void SaveTempCoinsAsCoins()
    {
        int tempCoins = PlayerPrefs.GetInt("temp_coins", 0);
        PlayerPrefs.SetInt("coins", tempCoins);      // Guardar el valor final en "coins"
        PlayerPrefs.Save();
        StartCoroutine(UpdateCoinsInDatabase(tempCoins));
    }

    private IEnumerator UpdateCoinsInDatabase(int coinsToSave)
    {
        if (string.IsNullOrEmpty(playerNickname))
        {
            Debug.LogWarning("El nickname del jugador no está asignado en PlayerPrefs.");
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new CoinUpdateRequest
        {
            nickname = playerNickname,
            coins = coinsToSave
        });

        UnityWebRequest request = new UnityWebRequest(coinsUpdateEndpoint, UnityWebRequest.kHttpVerbPUT);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(coinsToSave+playerNickname+"Monedas guardadas en la base de datos con éxito.");
        }
        else
        {
            Debug.LogError($"Error al guardar monedas en la base de datos: {request.error}");
        }
    }

    [System.Serializable]
    private class CoinUpdateRequest
    {
        public string nickname;
        public int coins;
    }
}