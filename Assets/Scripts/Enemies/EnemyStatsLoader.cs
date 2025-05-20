using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyStatsLoader : MonoBehaviour
{
    public int enemyId = 1; // ID que coincide con tu base de datos

    private void Start()
    {
        StartCoroutine(LoadStatsFromServer(enemyId));
    }

    IEnumerator LoadStatsFromServer(int id)
    {
        string url = $"http://localhost:3007/enemies/{id}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("ERROR al obtener datos del enemigo: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("Datos recibidos del servidor: " + json);

            EnemyData data = JsonUtility.FromJson<EnemyData>(json);

            var enemy = GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.SetStats(data);
                Debug.Log("Se aplicaron stats desde la base de datos.");
            }
        }
    }
}
