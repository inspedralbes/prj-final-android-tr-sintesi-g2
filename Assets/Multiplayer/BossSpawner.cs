using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BossSpawner : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject boss1Prefab;  // Prefab del goblin
    [SerializeField] private GameObject boss2Prefab;   // Prefab del slime
    [SerializeField] private GameObject boss3Prefab;     // Prefab del orco
    
    [Header("Posiciones de Spawn")]
    [SerializeField] private Vector2[] boss1SpawnPositions;  // Posiciones de spawn para goblins
    [SerializeField] private Vector2[] boss2SpawnPositions;   // Posiciones de spawn para slimes
    [SerializeField] private Vector2[] boss3SpawnPositions;     // Posiciones de spawn para orcos
    
    // Lista para mantener referencias a todos los jefes instanciados
    private List<BossTutorialMultiplayer> spawnedBosses = new List<BossTutorialMultiplayer>();
    
    // Diccionario para mantener la relación entre boss y reja
    private Dictionary<BossTutorialMultiplayer, RejaControllerMultiplayer> bossGateMapping = new Dictionary<BossTutorialMultiplayer, RejaControllerMultiplayer>();
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Instanciamos Goblins
        foreach (var position in boss1SpawnPositions)
        {
            SpawnEnemy(boss1Prefab, position);
        }
        
        // Instanciamos Slimes
        foreach (var position in boss2SpawnPositions)
        {
            SpawnEnemy(boss2Prefab, position);
        }
        
        // Instanciamos Orcs
        foreach (var position in boss3SpawnPositions)
        {
            SpawnEnemy(boss3Prefab, position);
        }
        
        // Esperar un breve momento y luego conectar los jefes a las rejas
        Invoke(nameof(AssignBossesToGates), 0.5f);
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
            
            // Comprobar si es un jefe
            BossTutorialMultiplayer bossComponent = enemy.GetComponent<BossTutorialMultiplayer>();
            if (bossComponent != null)
            {
                // Añadir a nuestra lista de jefes
                spawnedBosses.Add(bossComponent);
                
                // Suscribirse al evento de muerte del jefe
                bossComponent.OnBossDeath += () => HandleBossDeath(bossComponent);
                
                Debug.Log($"[SERVER] Boss spawned and tracked: {enemy.name}");
            }
            
            // Spawn en la red
            NetworkServer.Spawn(enemy);
        }
    }
    
    [Server]
    private void HandleBossDeath(BossTutorialMultiplayer boss)
    {
        Debug.Log($"[SERVER] Boss death handled by spawner: {boss.name}");
        
        // Notificar solo a la reja específica asignada a este boss
        if (bossGateMapping.TryGetValue(boss, out RejaControllerMultiplayer gate))
        {
            Debug.Log($"[SERVER] Notifying gate {gate.name} about death of boss {boss.name}");
            gate.NotifyBossDeath(boss);
        }
    }
    
    [Server]
    private void AssignBossesToGates()
    {
        // Encontrar todas las rejas en la escena
        RejaControllerMultiplayer[] allGates = FindObjectsOfType<RejaControllerMultiplayer>();
        
        if (allGates.Length == 0)
        {
            Debug.LogWarning("[SERVER] No gates found in scene!");
            return;
        }
        
        Debug.Log($"[SERVER] Found {allGates.Length} gates to connect with {spawnedBosses.Count} bosses");
        
        // Asignar un boss por reja, hasta que se acaben los bosses o las rejas
        int assignedCount = Mathf.Min(spawnedBosses.Count, allGates.Length);
        
        for (int i = 0; i < assignedCount; i++)
        {
            BossTutorialMultiplayer boss = spawnedBosses[i];
            RejaControllerMultiplayer gate = allGates[i];
            
            // Asociar este boss con esta reja específica
            bossGateMapping[boss] = gate;
            
            // Registrar solo este boss con esta reja
            gate.RegisterBoss(boss);
            
            Debug.Log($"[SERVER] Registered boss {boss.name} with gate {gate.name} (one-to-one assignment)");
        }
        
        // Informar sobre bosses sin reja asignada
        if (spawnedBosses.Count > allGates.Length)
        {
            Debug.LogWarning($"[SERVER] {spawnedBosses.Count - allGates.Length} bosses have no gate assigned!");
        }
    }
}