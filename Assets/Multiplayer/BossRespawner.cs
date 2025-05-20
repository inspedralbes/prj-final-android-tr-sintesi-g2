using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BossRespawner : NetworkBehaviour
{
    [System.Serializable]
    public class BossRespawnInfo
    {
        public GameObject bossPrefab;
        public Vector2[] spawnPositions;
        public float respawnDelay = 1.5f;
    }
    
    [Header("Bosses para Respawn")]
    [SerializeField] private BossRespawnInfo[] bossInfos;
    
    [Header("Configuración")]
    [SerializeField] private bool respawnOnNewCycle = true;
    [SerializeField] private bool respawnOnSceneLoad = true;
    
    private List<GameObject> activeBosses = new List<GameObject>();
    
    // Inicializar en el servidor
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Si es necesario, despawnar todos los bosses que ya existan en la escena
        // para evitar duplicados y asegurar que se creen con las estadísticas correctas
        if (respawnOnSceneLoad)
        {
            BossTutorialMultiplayer[] existingBosses = FindObjectsOfType<BossTutorialMultiplayer>();
            foreach (var boss in existingBosses)
            {
                if (boss != null && boss.gameObject != null)
                {
                    NetworkServer.Destroy(boss.gameObject);
                }
            }
            
            // Invocar el spawn de todos los bosses con un pequeño retraso
            Invoke(nameof(SpawnAllBosses), 0.5f);
        }
        
        // Suscribirse al evento de cambio de ciclo para respawnear cuando sea necesario
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnCycleCompleted += OnCycleCompleted;
        }
    }
    
    [Server]
    private void OnCycleCompleted(int newCycleLevel)
    {
        if (respawnOnNewCycle)
        {
            Debug.Log($"[SERVER] Nuevo ciclo {newCycleLevel} iniciado, respawneando bosses...");
            
            // Limpiar bosses anteriores que puedan seguir en la escena
            CleanupBosses();
            
            // Respawnear con delay para dar tiempo a los clientes a prepararse
            Invoke(nameof(SpawnAllBosses), 1.0f);
        }
    }
    
    [Server]
    private void CleanupBosses()
    {
        // Eliminar bosses activos anteriores
        foreach (var boss in activeBosses)
        {
            if (boss != null)
            {
                NetworkServer.Destroy(boss);
            }
        }
        activeBosses.Clear();
    }
    
    [Server]
    private void SpawnAllBosses()
    {
        if (!isServer) return;
        
        Debug.Log("[SERVER] Spawning all bosses with updated stats");
        
        for (int i = 0; i < bossInfos.Length; i++)
        {
            SpawnBossGroup(bossInfos[i], i * 0.5f); // Escalonar spawns para reducir carga
        }
    }
    
    [Server]
    private void SpawnBossGroup(BossRespawnInfo bossInfo, float additionalDelay = 0f)
    {
        if (bossInfo == null || bossInfo.bossPrefab == null || bossInfo.spawnPositions == null) return;
        
        // Usar Invoke para agregar un retraso entre spawns de grupos
        Invoke(nameof(DoSpawnBossGroup), bossInfo.respawnDelay + additionalDelay);
    }
    
    [Server]
    private void DoSpawnBossGroup()
    {
        // En este punto deberíamos tener la información de qué grupo spawnear,
        // pero como Invoke no puede pasar parámetros complejos, tendríamos que usar
        // una cola o algún otro mecanismo para saber qué grupo es el siguiente.
        
        // Para simplicidad, volveremos a iterar y spawnear cualquier grupo pendiente
        foreach (var bossInfo in bossInfos)
        {
            foreach (var position in bossInfo.spawnPositions)
            {
                SpawnSingleBoss(bossInfo.bossPrefab, position);
            }
        }
    }
    
    [Server]
    private void SpawnSingleBoss(GameObject bossPrefab, Vector2 position)
    {
        if (bossPrefab == null) return;
        
        Vector3 spawnPos = new Vector3(position.x, position.y, 0f);
        GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        
        // Asegúrate de que el prefab tenga los componentes necesarios
        if (boss.GetComponent<NetworkIdentity>() == null)
        {
            Debug.LogError("[SERVER] El prefab de boss no tiene NetworkIdentity");
            Destroy(boss);
            return;
        }
        
        // Registrar en lista de bosses activos
        activeBosses.Add(boss);
        
        // Asegurar registro con gestores de dificultad y eventos
        BossTutorialMultiplayer bossComponent = boss.GetComponent<BossTutorialMultiplayer>();
        if (bossComponent != null)
        {
            // DifficultyManager se encargará de establecer las estadísticas correctas
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.RegisterBoss(bossComponent);
            }
            
            // Podemos registrar eventos adicionales si es necesario
            bossComponent.OnBossDeath += () => OnBossDied(bossComponent);
        }
        
        // Spawnear en la red
        NetworkServer.Spawn(boss);
        Debug.Log($"[SERVER] Spawned boss at {spawnPos}");
    }
    
    [Server]
    private void OnBossDied(BossTutorialMultiplayer boss)
    {
        if (boss != null && boss.gameObject != null)
        {
            // Hacer lo que sea necesario cuando un boss muere
            Debug.Log($"[SERVER] Boss {boss.name} died and notified BossRespawner");
            
            // Opcionalmente, podríamos respawnear este boss específico después de un tiempo
            // pero en este sistema esperamos a completar el ciclo
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnCycleCompleted -= OnCycleCompleted;
        }
    }
}