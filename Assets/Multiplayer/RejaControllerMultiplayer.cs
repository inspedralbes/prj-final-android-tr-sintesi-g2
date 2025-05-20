using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class RejaControllerMultiplayer : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BossTutorialMultiplayer bossReference; // Referencia directa al jefe (opcional)
    
    [Header("Configuración")]
    [SerializeField] private float fadeTime = 2f; // Tiempo de desvanecimiento
    [SerializeField] private GameObject rejaCollider; // Collider de la reja (puede ser un hijo con BoxCollider2D)
    [SerializeField] private AudioClip disolveSound; // Sonido al desvanecerse
    [SerializeField] private bool requireAllBossesDead = false; // ¿Requiere que todos los jefes mueran?
    
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isSubscribed = false;
    
    // Lista de jefes registrados con esta reja
    private List<BossTutorialMultiplayer> registeredBosses = new List<BossTutorialMultiplayer>();
    
    // Variable sincronizada en red para el estado de la reja
    [SyncVar(hook = nameof(OnDisappearedChanged))]
    private bool hasDisappeared = false;

    private void Awake()
    {
        // Obtener componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Si no tiene un AudioSource, añadirlo
        if (audioSource == null && disolveSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Aplicar el estado actual cuando un cliente se conecta
        if (hasDisappeared)
        {
            ApplyDisappearedState(true);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[SERVER] RejaController started on server");
        
        // Intentar encontrar y suscribirse al jefe si hay una referencia directa
        if (bossReference != null)
        {
            RegisterBoss(bossReference);
        }
        
        // El resto de jefes deberían ser registrados por el BossSpawner
    }

    private void Start()
    {
        // Asegurarse de que el collider esté activado al inicio si la reja no ha desaparecido
        if (!hasDisappeared)
        {
            if (rejaCollider != null)
            {
                rejaCollider.SetActive(true);
            }
            else
            {
                // Si no se asignó un collider específico, usar el del propio GameObject
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
        }
    }
    
    [Server]
    // Método público para que el BossSpawner registre jefes con esta reja
    public void RegisterBoss(BossTutorialMultiplayer boss)
    {
        if (boss != null && !registeredBosses.Contains(boss))
        {
            // Añadir a la lista de jefes registrados
            registeredBosses.Add(boss);
            
            // Suscribirse al evento de muerte del jefe
            boss.OnBossDeath += OnBossDeath;
            
            Debug.Log($"[SERVER] Gate {name} registered boss: {boss.name}");
            
            // Si el jefe ya está muerto, comprobar si debemos desaparecer
            if (boss.IsDead())
            {
                Debug.Log("[SERVER] Newly registered boss is already dead");
                CheckBossConditions();
            }
        }
    }
    
    // Método para que el spawner notifique cuando un jefe muere
    [Server]
    public void NotifyBossDeath(BossTutorialMultiplayer deadBoss)
    {
        Debug.Log($"[SERVER] Gate {name} notified about boss death: {deadBoss.name}");
        
        // Verificar si este jefe está en nuestra lista
        if (registeredBosses.Contains(deadBoss))
        {
            // No es necesario llamar a OnBossDeath porque ya debería estar suscrito al evento
            // Pero verificamos las condiciones de todos modos
            CheckBossConditions();
        }
    }
    
    // Método que puede ser llamado externamente cuando un jefe muere
    // (útil cuando hay problemas con eventos)
    [Server]
    public void OnExternalBossDeath()
    {
        Debug.Log($"[SERVER] Gate {name} received external boss death notification");
        CheckBossConditions();
    }

    [Server]
    private void OnBossDeath()
    {
        Debug.Log("[SERVER] OnBossDeath event received in RejaController");
        CheckBossConditions();
    }
    
    [Server]
    private void CheckBossConditions()
    {
        // Si ya desapareció, no hacer nada
        if (hasDisappeared) return;
        
        // Si no hay bosses registrados y no se requiere que todos mueran, desaparecer
        if (registeredBosses.Count == 0 && !requireAllBossesDead)
        {
            DisappearGate();
            return;
        }
        
        // Si requiere que todos los jefes mueran
        if (requireAllBossesDead)
        {
            bool allBossesDead = true;
            
            // Verificar si todos los jefes están muertos
            foreach (var boss in registeredBosses)
            {
                if (boss != null && !boss.IsDead())
                {
                    allBossesDead = false;
                    break;
                }
            }
            
            // Si todos están muertos, desaparecer
            if (allBossesDead && registeredBosses.Count > 0)
            {
                Debug.Log("[SERVER] All registered bosses are dead, gate will disappear");
                DisappearGate();
            }
        }
        else
        {
            // Si cualquier jefe muere es suficiente
            Debug.Log("[SERVER] A boss died, gate will disappear");
            DisappearGate();
        }
    }

    // Comando que puede ser llamado desde un cliente para desaparecer la reja
    [Command(requiresAuthority = false)]
    public void CmdDisappearGate()
    {
        Debug.Log("[SERVER] CmdDisappearGate received from client");
        DisappearGate();
    }
    
    // Método interno que ejecuta la lógica de desaparición en el servidor
    [Server]
    private void DisappearGate()
    {
        // Solo ejecutar si no ha desaparecido ya
        if (!hasDisappeared)
        {
            Debug.Log("[SERVER] Setting gate to disappeared state");
            // Cambiar la variable sincronizada
            hasDisappeared = true;
            
            // Llamar al RPC para reproducir efectos sonoros en todos los clientes
            RpcPlayDisolveSound();
        }
    }

    // Hook que se llama cuando el valor de hasDisappeared cambia
    void OnDisappearedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[CLIENT] Reja disappeared state changed: {oldValue} -> {newValue}");
        // Aplicar los cambios de estado visualmente en todos los clientes
        ApplyDisappearedState(newValue);
    }

    // Aplica el estado visual y físico según el valor de desaparición
    void ApplyDisappearedState(bool disappeared)
    {
        if (disappeared)
        {
            Debug.Log("[CLIENT] Applying disappeared state to gate");
            // Desactivar collider inmediatamente
            if (rejaCollider != null)
            {
                rejaCollider.SetActive(false);
            }
            else
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
            
            // Iniciar la animación de desvanecimiento solo visualmente
            StartCoroutine(DisappearVisualEffect());
        }
        else
        {
            Debug.Log("[CLIENT] Restoring gate visibility");
            // Restaurar visibilidad
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
                spriteRenderer.enabled = true;
            }
            
            // Restaurar colisión
            if (rejaCollider != null)
            {
                rejaCollider.SetActive(true);
            }
            else
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            
            gameObject.SetActive(true);
        }
    }

    // RPC para reproducir el sonido en todos los clientes
    [ClientRpc]
    void RpcPlayDisolveSound()
    {
        Debug.Log("[CLIENT] Playing dissolve sound effect");
        // Reproducir sonido si existe
        if (audioSource != null && disolveSound != null)
        {
            audioSource.clip = disolveSound;
            audioSource.Play();
        }
    }

    // Coroutine para el efecto visual de desvanecimiento (solo local)
    private IEnumerator DisappearVisualEffect()
    {
        Debug.Log("[CLIENT] Starting visual disappearance effect");
        // Efecto de desvanecimiento
        float elapsedTime = 0f;
        Color originalColor = spriteRenderer.color;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1.0f - (elapsedTime / fadeTime);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // Ocultar el sprite cuando se haya desvanecido completamente
        Debug.Log("[CLIENT] Finished fading out gate");
        spriteRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        // Desuscribirse de todos los eventos de los jefes
        foreach (var boss in registeredBosses)
        {
            if (boss != null)
            {
                boss.OnBossDeath -= OnBossDeath;
            }
        }
    }

    // Para pruebas: Función para forzar la desaparición de la reja manualmente
    public void ForceDisappear()
    {
        if (isServer)
        {
            Debug.Log("[SERVER] Forcing gate disappearance");
            DisappearGate();
        }
        else
        {
            Debug.Log("[CLIENT] Requesting server to force gate disappearance");
            CmdDisappearGate();
        }
    }

    // Para pruebas: Comando para restaurar la reja
    [Command(requiresAuthority = false)]
    public void CmdRestoreGate()
    {
        RestoreGate();
    }
    
    // Método interno para restaurar la reja en el servidor
    [Server]
    private void RestoreGate()
    {
        if (hasDisappeared)
        {
            Debug.Log("[SERVER] Restoring gate");
            hasDisappeared = false;
        }
    }
    
    // Método para establecer la referencia directa al jefe
    public void SetBossReference(BossTutorialMultiplayer boss)
    {
        if (isServer)
        {
            RegisterBoss(boss);
        }
    }
}