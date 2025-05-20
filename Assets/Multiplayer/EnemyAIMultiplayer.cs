using UnityEngine;
using Mirror;
using System.Collections.Generic;

public abstract class EnemyAIMultiplayer : NetworkBehaviour
{
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected int enemyMaxHealth = 50;
    [SerializeField] protected float followRange = 10f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected int hitDamage = 25;

    protected Animator animator;
    protected Transform targetPlayer;

    [SyncVar] protected int enemyHealth;
    [SyncVar] private bool isDead = false;

    [SyncVar(hook = nameof(OnFlipChanged))]
    protected bool isFlipped;    // Solo mantener la sincronización del flip

    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Die = Animator.StringToHash("Die");

    protected List<PlayerControllerMulti> allPlayers = new List<PlayerControllerMulti>();
    protected SpriteRenderer spriteRenderer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        enemyHealth = enemyMaxHealth;
        Debug.Log($"[SERVER] Enemy initialized with {enemyHealth} HP");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[CLIENT] EnemyAIMultiplayer initialized on client.");
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        InitEnemy();

        if (isServer)
        {
            RefreshPlayersList();
            Debug.Log("[SERVER] Player list initialized at Start()");
        }
    }

    // Actualizar el flip en cada frame basado en SyncVar
    private void LateUpdate()
    {
        if (isClient && spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlipped;
        }
    }

    private void Update()
    {
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

        float distance = Vector2.Distance(transform.position, targetPlayer.position);

        if (distance <= attackRange)
        {
            RpcSetAnimationBool(IsMoving, false);
            Attack();
        }
        else if (distance <= followRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            RpcSetAnimationBool(IsMoving, false);
        }
    }

    [Server]
    void RefreshPlayersList()
    {
        allPlayers.Clear();
        PlayerControllerMulti[] players = FindObjectsOfType<PlayerControllerMulti>();
        foreach (var player in players)
        {
            allPlayers.Add(player);
            Debug.Log($"[SERVER] Player found and added: {player.gameObject.name}");
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
            if (distanceToPlayer < closestDistance && distanceToPlayer <= followRange)
            {
                closestDistance = distanceToPlayer;
                closestPlayer = player.transform;
            }
        }

        targetPlayer = closestPlayer;
    }

    [Server]
    protected virtual void MoveTowardsPlayer()
    {
        if (isDead || targetPlayer == null) return;

        RpcSetAnimationBool(IsMoving, true);

        Vector2 direction = (targetPlayer.position - transform.position).normalized;
        // transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + (Vector3)direction * moveSpeed * Time.deltaTime;



        // Enviar la nueva posición a los clientes
        RpcSetPosition(newPosition);
        // El flip ahora se sincroniza automáticamente con el hook
        bool shouldFlip = direction.x < 0;
        if (isFlipped != shouldFlip)
        {
            isFlipped = shouldFlip; // Esto activará el hook automáticamente en todos los clientes
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
    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        enemyHealth -= damage;
        Debug.Log($"[SERVER] Enemy took {damage} damage. Remaining HP: {enemyHealth}");

        if (enemyHealth <= 0)
        {
            HandleDeath();
        }
        else
        {
            RpcTriggerAnimation(Hit);
        }
    }

    [Server]
    protected virtual void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[SERVER] Enemy died.");
        RpcTriggerAnimation(Die);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        RpcSetAnimationBool(IsMoving, false);
        Invoke(nameof(DeactivateEnemy), GetAnimationClipLength("Die"));
    }

    [Server]
    private void DeactivateEnemy()
    {
        Debug.Log("[SERVER] Enemy deactivated after death.");
        gameObject.SetActive(false);
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
            Debug.LogError("[CLIENT] AnimatorTrigger is null!");
        }
    }

    [ClientRpc]
    protected void RpcSetAnimationBool(int hash, bool value)
    {
        if (animator != null)
        {
            Debug.Log($"Moviendo el culete");
            animator.SetBool(hash, value);
        }
        else
        {
            Debug.LogError("[CLIENT] Animator is null!");
        }
    }

    protected virtual void InitEnemy() { }
    protected abstract void Attack();

    private float GetAnimationClipLength(string clipName)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips)
        {
            if (clip.name == clipName)
                return clip.length;
        }
        return 1f;
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"[SERVER] Enemy triggered with player: {collision.name}");
            TakeDamage(hitDamage);
        }
    }

    public bool IsDead() => isDead;
    public int GetHealth() => enemyHealth;

    [Server]
    public void RegisterPlayer(PlayerControllerMulti player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
            Debug.Log($"[SERVER] Player registered: {player.gameObject.name}");
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerControllerMulti player)
    {
        if (allPlayers.Contains(player))
        {
            allPlayers.Remove(player);
            Debug.Log($"[SERVER] Player unregistered: {player.gameObject.name}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[ANY] Enemy collided with: {collision.gameObject.name} Tag: {collision.gameObject.tag}");
    }
}