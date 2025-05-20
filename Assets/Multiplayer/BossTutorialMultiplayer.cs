using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using Mirror;
using System.Collections.Generic;

public class BossTutorialMultiplayer : NetworkBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int bossMaxHealth = 200;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attack1Damage = 30;
    [SerializeField] private int attack2Damage = 15;
    [SerializeField] private float visionRange = 30f;
    [SerializeField] private float disintegrationTime = 2f;

    [SyncVar] private int bossHealth;
    [SyncVar] private bool isDead = false;
    [SyncVar(hook = nameof(OnFlipChanged))] private bool isFlipped;

    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    
    // Evento para notificar la muerte del jefe, hacerlo público
    public event Action OnBossDeath;

    [SerializeField] private Image healthBarBackground;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text bossNameText;
    [SerializeField] private string bossName = "Valthorr";

    [SerializeField] private GameObject staffHitbox;

    [Header("Boss Music")]
    [SerializeField] private AudioSource bossMusic;

    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Attack1 = Animator.StringToHash("Attack1");
    private static readonly int Attack2 = Animator.StringToHash("Attack2");
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int Hit = Animator.StringToHash("Hit");

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    protected List<PlayerControllerMulti> allPlayers = new List<PlayerControllerMulti>();
    protected Transform targetPlayer;

    // Override de OnStartServer para inicialización en el servidor
    public override void OnStartServer()
    {
        base.OnStartServer();
        bossHealth = bossMaxHealth;
        isDead = false;
        Debug.Log($"[SERVER] Boss initialized with {bossHealth} HP");
        
        // Registrar este boss con el DifficultyManager
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.RegisterBoss(this);
        }
    }

    // Override de OnStartClient para inicialización en el cliente
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[CLIENT] BossTutorialMultiplayer initialized on client.");
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (isServer)
        {
            RefreshPlayersList();
            SetAttack(false);
            Debug.Log("[SERVER] Player list initialized at Start()");
        }

        if (isClient)
        {
            // Inicialización de UI solo en clientes
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = 1f;
                healthBarBackground.gameObject.SetActive(false);
            }

            if (bossNameText != null)
            {
                bossNameText.text = bossName;
                bossNameText.color = Color.red;
                bossNameText.gameObject.SetActive(false);
            }

            if (bossMusic != null)
            {
                bossMusic.Stop();
            }
        }
    }

    private void LateUpdate()
    {
        if (isClient && spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlipped;
        }
    }

    private void Update()
    {
        if (isClient)
        {
            // Actualización de UI en clientes
            UpdateClientUI();
        }

        if (!isServer || isDead) return;

        if (Time.frameCount % 60 == 0)
        {
            RefreshPlayersList();
        }

        FindNearestPlayer();

        if (targetPlayer == null)
        {
            RpcSetAnimationBool(IsMoving, false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        // Si el jugador está en el rango de visión
        if (distanceToPlayer <= visionRange)
        {
            RpcShowUI(true);
            MoveTowardsPlayer();

            // Si el jugador está dentro del rango de ataque
            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                Attack();
            }
        }
        else
        {
            RpcShowUI(false);
            RpcSetAnimationBool(IsMoving, false);
            RpcStopMusic();
        }
    }

    [ClientRpc]
    private void RpcShowUI(bool show)
    {
        if (healthBarBackground != null)
            healthBarBackground.gameObject.SetActive(show);
        
        if (bossNameText != null) 
            bossNameText.gameObject.SetActive(show);
        
        if (bossMusic != null)
        {
            if (show && !bossMusic.isPlaying)
                bossMusic.Play();
            else if (!show && bossMusic.isPlaying)
                bossMusic.Stop();
        }
    }

    [ClientRpc]
    private void RpcStopMusic()
    {
        if (bossMusic != null && bossMusic.isPlaying)
        {
            bossMusic.Stop();
        }
    }

    private void UpdateClientUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)bossHealth / bossMaxHealth;
        }

        UpdateBossNameColor();
    }

    private void UpdateBossNameColor()
    {
        if (bossNameText != null)
        {
            float healthPercent = (float)bossHealth / bossMaxHealth;
            bossNameText.color = Color.Lerp(Color.gray, Color.red, healthPercent);
        }
    }

    [Server]
    private void RefreshPlayersList()
    {
        allPlayers.Clear();
        PlayerControllerMulti[] players = FindObjectsOfType<PlayerControllerMulti>();
        foreach (var player in players)
        {
            allPlayers.Add(player);
        }
    }

    [Server]
    protected void FindNearestPlayer()
    {
        float closestDistance = float.MaxValue;
        Transform closestPlayer = null;

        foreach (var player in allPlayers)
        {
            if (player == null) continue;

            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            if (distanceToPlayer < closestDistance && distanceToPlayer <= visionRange)
            {
                closestDistance = distanceToPlayer;
                closestPlayer = player.transform;
            }
        }

        targetPlayer = closestPlayer;
    }

    [Server]
    private void MoveTowardsPlayer()
    {
        if (isAttacking || isDead || targetPlayer == null) return;

        RpcSetAnimationBool(IsMoving, true);
        Vector2 direction = (targetPlayer.position - transform.position).normalized;

        // Moverse solo en el eje X
        Vector3 newPosition = new Vector3(rb.position.x + direction.x * moveSpeed * Time.fixedDeltaTime, rb.position.y, 0);
        
        // Enviar la nueva posición a los clientes
        RpcSetPosition(newPosition);

        // Actualizar la orientación del sprite
        bool shouldFlip = direction.x < 0;
        if (isFlipped != shouldFlip)
        {
            isFlipped = shouldFlip;
        }
    }

    [ClientRpc]
    protected void RpcSetPosition(Vector3 position)
    {
        transform.position = position;
    }

    protected void OnFlipChanged(bool oldValue, bool newValue)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = newValue;
        }
        else
        {
            Debug.LogError("[CLIENT] SpriteRenderer is null!");
        }
    }

    [Server]
    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (bossHealth > bossMaxHealth / 2)
        {
            RpcTriggerAnimation(Attack1);
            Invoke(nameof(DelayedAttack1Damage), 0.5f);
        }
        else
        {
            RpcTriggerAnimation(Attack2);
            Invoke(nameof(DelayedAttack2Damage), 0.7f);
        }

        RpcSetStaffHitbox(true);
        Invoke(nameof(EndAttack), 1.0f);
    }

    [Server]
    private void DelayedAttack1Damage()
    {
        DealDamage(attack1Damage);
    }

    [Server]
    private void DelayedAttack2Damage()
    {
        DealDamage(attack2Damage);
    }

    [Server]
    private void DealDamage(int damage)
    {
        if (targetPlayer == null) return;

        if (Vector2.Distance(transform.position, targetPlayer.position) <= attackRange)
        {
            PlayerControllerMulti playerController = targetPlayer.GetComponent<PlayerControllerMulti>();
            if (playerController != null)
            {
                playerController.TakeDamage(damage);
                
                Vector2 direction = targetPlayer.position.x < transform.position.x ? Vector2.left : Vector2.right;
                playerController.PushPlayer(direction.normalized, 25);
            }
        }
    }

    [Server]
    private void EndAttack()
    {
        isAttacking = false;
        RpcSetAnimationBool(IsMoving, true);
        RpcSetStaffHitbox(false);
    }

    [ClientRpc]
    private void RpcSetStaffHitbox(bool isActive)
    {
        if (staffHitbox != null)
        {
            staffHitbox.SetActive(isActive);
        }
    }

    [Server]
    private void SetAttack(bool isOn)
    {
        isAttacking = isOn;
        RpcSetAnimationBool(IsMoving, !isOn);
        RpcSetStaffHitbox(isOn);
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        bossHealth -= damage;
        Debug.Log($"[SERVER] Boss took {damage} damage. Remaining HP: {bossHealth}");
        
        RpcTriggerAnimation(Hit);

        if (bossHealth <= 0)
        {
            HandleDeath();
        }
    }

    [Server]
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[SERVER] Boss died. Invoking OnBossDeath event!");
        RpcTriggerAnimation(Die);
        RpcShowUI(false);
        
        rb.simulated = false;
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        RpcSetAnimationBool(IsMoving, false);
        RpcBeginDisintegration();

        // Notificar a los suscriptores que el jefe ha muerto (solo en el servidor)
        OnBossDeath?.Invoke();
    }
    
    [ClientRpc]
    private void RpcBeginDisintegration()
    {
        StartCoroutine(Disintegrate());
    }

    [Server]
    public void ResetHealth()
    {
        bossHealth = bossMaxHealth;
        isDead = false;
        Debug.Log("[SERVER] The boss has fully restored its health.");
    }

    private IEnumerator Disintegrate()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float elapsedTime = 0f;

        while (elapsedTime < disintegrationTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - (elapsedTime / disintegrationTime);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            yield return null;
        }

        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ClientRpc]
    protected void RpcTriggerAnimation(int trigger)
    {
        if (animator != null)
        {
            animator.SetTrigger(trigger);
        }
        else
        {
            Debug.LogError("[CLIENT] Animator is null!");
        }
    }

    [ClientRpc]
    protected void RpcSetAnimationBool(int hash, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(hash, value);
        }
        else
        {
            Debug.Log("[CLIENT] Animator is null!");
        }
    }

    [Server]
    public void RegisterPlayer(PlayerControllerMulti player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
            Debug.Log($"[SERVER] Player registered with boss: {player.gameObject.name}");
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerControllerMulti player)
    {
        if (allPlayers.Contains(player))
        {
            allPlayers.Remove(player);
            Debug.Log($"[SERVER] Player unregistered from boss: {player.gameObject.name}");
        }
    }

    // Nuevos métodos para obtener/establecer valores de estadísticas (para DifficultyManager)
    
    [Server]
    public void SetMaxHealth(int newMaxHealth)
    {
        bossMaxHealth = newMaxHealth;
    }
    
    [Server]
    public void SetAttack1Damage(int newDamage)
    {
        attack1Damage = newDamage;
    }
    
    [Server]
    public void SetAttack2Damage(int newDamage)
    {
        attack2Damage = newDamage;
    }
    
    [Server]
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    // Getters para estadísticas
    public int GetMaxHealth() => bossMaxHealth;
    public int GetAttack1Damage() => attack1Damage;
    public int GetAttack2Damage() => attack2Damage;
    public float GetMoveSpeed() => moveSpeed;
    public bool IsDead() => isDead;
    public int GetHealth() => bossHealth;
}