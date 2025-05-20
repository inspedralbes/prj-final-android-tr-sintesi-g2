using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

[Serializable]
public class EnemyDeathData
{
    public string enemy_name;
    public string bossName;
    public string nickname;
    // No necesitamos enviar death_time, el servidor lo generará
}

public class EnemyDeathService : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:3010/enemyDeaths";

    private static EnemyDeathService _instance;
    public static EnemyDeathService Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject serviceObject = new GameObject("EnemyDeathService");
                _instance = serviceObject.AddComponent<EnemyDeathService>();
                DontDestroyOnLoad(serviceObject);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Registra la muerte de un enemigo común
    /// </summary>
    /// <param name="enemyName">Nombre del enemigo</param>
    /// <param name="playerNickname">Nickname del jugador</param>
    public void RegisterEnemyDeath(string enemyName, string playerNickname)
    {
        EnemyDeathData deathData = new EnemyDeathData
        {
            enemy_name = enemyName,
            bossName = null,
            nickname = playerNickname
        };

        StartCoroutine(SendDeathData(deathData));
        Debug.Log($"Registrando muerte del enemigo: {enemyName} por {playerNickname}");
    }

    /// <summary>
    /// Registra la muerte de un jefe (boss)
    /// </summary>
    /// <param name="bossName">Nombre del jefe</param>
    /// <param name="playerNickname">Nickname del jugador</param>
    public void RegisterBossDeath(string bossName, string playerNickname)
    {
        EnemyDeathData deathData = new EnemyDeathData
        {
            enemy_name = null,
            bossName = bossName,
            nickname = playerNickname
        };

        StartCoroutine(SendDeathData(deathData));
        Debug.Log($"Registrando muerte del jefe: {bossName} por {playerNickname}");
    }

    private IEnumerator SendDeathData(EnemyDeathData deathData)
    {
        // Convertir los datos a JSON
        string jsonData = JsonUtility.ToJson(deathData);

        // Crear la petición web
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Enviar la petición
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al registrar la muerte: " + www.error);
            }
            else
            {
                Debug.Log("Muerte registrada correctamente en el servidor");
            }
        }
    }
}