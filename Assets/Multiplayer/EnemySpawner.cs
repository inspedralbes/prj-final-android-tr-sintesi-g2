using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject goblinPrefab;  // Prefab del goblin
    [SerializeField] private GameObject slimePrefab;   // Prefab del slime
    [SerializeField] private GameObject orcPrefab;     // Prefab del orco

    [Header("Posiciones de Spawn")]
    [SerializeField] private Vector2[] goblinSpawnPositions;  // Posiciones de spawn para goblins
    [SerializeField] private Vector2[] slimeSpawnPositions;   // Posiciones de spawn para slimes
    [SerializeField] private Vector2[] orcSpawnPositions;     // Posiciones de spawn para orcos

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Instanciamos Goblins
        foreach (var position in goblinSpawnPositions)
        {
            SpawnEnemy(goblinPrefab, position);
        }

        // Instanciamos Slimes
        foreach (var position in slimeSpawnPositions)
        {
            SpawnEnemy(slimePrefab, position);
        }

        // Instanciamos Orcs
        foreach (var position in orcSpawnPositions)
        {
            SpawnEnemy(orcPrefab, position);
        }
    }

    // Método para instanciar enemigos
    private void SpawnEnemy(GameObject enemyPrefab, Vector2 position)
    {
        if (enemyPrefab != null)
        {
            // En 2D, aseguramos que Z=0
            Vector3 spawnPos = new Vector3(position.x, position.y, 0f);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // Asegúrate de que el prefab tenga un NetworkIdentity
            if (enemy.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError("El prefab de enemigo no tiene un NetworkIdentity.");
                return;
            }

            // Spawn en la red
            NetworkServer.Spawn(enemy);
        }
    }
}
