using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class DifficultyManager : NetworkBehaviour
{
    // Singleton para fácil acceso
    public static DifficultyManager Instance { get; private set; }
    
    [Header("Configuración de Dificultad")]
    [SerializeField] private float healthMultiplier = 0.1f;      // Incremento del 10% por nivel
    [SerializeField] private float damageMultiplier = 0.1f;      // Incremento del 10% por nivel
    [SerializeField] private float speedMultiplier = 0.05f;      // Incremento del 5% por nivel
    
    // Contador de ciclos completados (SyncVar para sincronizar en todos los clientes)
    [SyncVar] private int difficultyLevel = 0;
    
    // Evento para notificar cuando se completa un ciclo (aumento de dificultad)
    public delegate void CycleCompletedHandler(int newLevel);
    public event CycleCompletedHandler OnCycleCompleted;
    
    // Lista de bosses en la escena actual
    private List<BossTutorialMultiplayer> allBosses = new List<BossTutorialMultiplayer>();
    
    // Estadísticas base originales de cada boss para cálculos posteriores
    private Dictionary<int, BossBaseStats> originalBossStats = new Dictionary<int, BossBaseStats>();
    
    private void Awake()
    {
        // Configuración del singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"[SERVER] DifficultyManager inicializado en nivel {difficultyLevel}");
    }
    
    [Server]
    public void RegisterBoss(BossTutorialMultiplayer boss)
    {
        if (!allBosses.Contains(boss))
        {
            // Obtener un ID único para este boss
            int bossID = boss.GetInstanceID();
            
            // Registrar el boss
            allBosses.Add(boss);
            
            // Guardar las estadísticas originales si no existen
            if (!originalBossStats.ContainsKey(bossID))
            {
                originalBossStats[bossID] = new BossBaseStats
                {
                    MaxHealth = boss.GetMaxHealth(),
                    Attack1Damage = boss.GetAttack1Damage(),
                    Attack2Damage = boss.GetAttack2Damage(),
                    MoveSpeed = boss.GetMoveSpeed()
                };
                
                Debug.Log($"[SERVER] Estadísticas base registradas para boss ID {bossID}");
            }
            
            // Aplicar modificadores de dificultad si no es el primer ciclo
            if (difficultyLevel > 0)
            {
                ApplyDifficultyModifiers(boss, bossID);
            }
            
            Debug.Log($"[SERVER] Boss registrado con DifficultyManager: {boss.name}");
        }
    }
    
    [Server]
    public void IncreaseDifficulty()
    {
        difficultyLevel++;
        Debug.Log($"[SERVER] Nivel de dificultad aumentado a {difficultyLevel}");
        
        // Notificar a todos los clientes del nuevo nivel
        RpcUpdateDifficultyUI(difficultyLevel);
        
        // Aplicar los modificadores a todos los bosses
        foreach (var boss in allBosses)
        {
            if (boss != null && !boss.IsDead())
            {
                int bossID = boss.GetInstanceID();
                ApplyDifficultyModifiers(boss, bossID);
            }
        }
        
        // Disparar evento de ciclo completado
        OnCycleCompleted?.Invoke(difficultyLevel);
        
        // Notificar al GameManager si existe
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDifficultyIncreased(difficultyLevel);
        }
    }
    
    [Server]
    private void ApplyDifficultyModifiers(BossTutorialMultiplayer boss, int bossID)
    {
        if (!originalBossStats.ContainsKey(bossID))
        {
            Debug.LogError($"[SERVER] No se encontraron estadísticas originales para boss ID {bossID}");
            return;
        }
        
        // Obtener estadísticas base
        BossBaseStats baseStats = originalBossStats[bossID];
        
        // Calcular nuevas estadísticas con los multiplicadores
        int newMaxHealth = Mathf.RoundToInt(baseStats.MaxHealth * (1 + (healthMultiplier * difficultyLevel)));
        int newAttack1Damage = Mathf.RoundToInt(baseStats.Attack1Damage * (1 + (damageMultiplier * difficultyLevel)));
        int newAttack2Damage = Mathf.RoundToInt(baseStats.Attack2Damage * (1 + (damageMultiplier * difficultyLevel)));
        float newMoveSpeed = baseStats.MoveSpeed * (1 + (speedMultiplier * difficultyLevel));
        
        // Aplicar las nuevas estadísticas
        boss.SetMaxHealth(newMaxHealth);
        boss.SetAttack1Damage(newAttack1Damage);
        boss.SetAttack2Damage(newAttack2Damage);
        boss.SetMoveSpeed(newMoveSpeed);
        
        // Restaurar la salud al máximo
        boss.ResetHealth();
        
        Debug.Log($"[SERVER] Aplicados modificadores nivel {difficultyLevel} a boss {boss.name}: " +
                 $"Salud={newMaxHealth}, Ataque1={newAttack1Damage}, Ataque2={newAttack2Damage}, Velocidad={newMoveSpeed}");
    }
    
    [ClientRpc]
    private void RpcUpdateDifficultyUI(int newLevel)
    {
        // Código para actualizar la UI si implementas un indicador de nivel
        Debug.Log($"[CLIENT] Nivel de dificultad actualizado a {newLevel}");
        
        // Aquí podrías mostrar algún mensaje o actualizar un texto en la UI
        // Por ejemplo:
        // difficultyLevelText.text = $"Nivel: {newLevel}";
    }
    
    public int GetDifficultyLevel()
    {
        return difficultyLevel;
    }
}

// Clase para almacenar las estadísticas base de cada boss
public class BossBaseStats
{
    public int MaxHealth;
    public int Attack1Damage;
    public int Attack2Damage;
    public float MoveSpeed;
}