using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    [Header("Enemy Config")]
    [SerializeField] private string enemyName = "DefaultEnemy"; // Se puede personalizar en el Inspector
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected int enemyMaxHealth = 50;
    [SerializeField] protected float followRange = 10f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected int hitDamage = 25;

    protected Transform player;
    protected Animator animator;
    protected int enemyHealth;
    private bool isDead = false;

    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Die = Animator.StringToHash("Die");

    public virtual void SetStats(EnemyData data)
    {
        moveSpeed = data.move_speed;
        enemyMaxHealth = data.enemy_max_health;
        followRange = data.follow_range;
        attackRange = data.attack_range;
        hitDamage = data.hit_damage;

        enemyHealth = enemyMaxHealth;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        enemyHealth = enemyMaxHealth;

        InitEnemy();

        if (string.IsNullOrEmpty(enemyName) || enemyName == "DefaultEnemy")
        {
            Debug.LogWarning($"Enemy '{gameObject.name}' tiene un nombre por defecto. Asigna 'enemyName' en el Inspector.");
        }
    }

    private void Update()
    {
        if (isDead) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            Attack();
        }
        else if (distanceToPlayer <= followRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            animator.SetBool(IsMoving, false);
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (isDead) return;

        animator.SetBool(IsMoving, true);
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    protected abstract void Attack();

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        Debug.Log("EnemyAI: TakeDamage");
        enemyHealth -= damage;

        if (enemyHealth <= 0)
        {
            HandleDeath();
        }
        else
        {
            animator.SetTrigger(Hit);
        }
    }

    protected virtual void HandleDeath()
    {
        if (isDead) return;

        isDead = true;
        animator.SetTrigger(Die);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        animator.SetBool(IsMoving, false);

        // Registrar muerte con EnemyDeathService
        string playerNickname = PlayerPrefs.GetString("nickname", "Desconocido");
        EnemyDeathService.Instance.RegisterEnemyDeath(enemyName, playerNickname);

        Invoke(nameof(DeactivateEnemy), GetAnimationClipLength("Die"));
    }

    private void DeactivateEnemy()
    {
        gameObject.SetActive(false);
    }

    protected virtual void InitEnemy() { }

    private float GetAnimationClipLength(string clipName)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }
        return 1f;
    }

    public void ResetEnemy()
    {
        Debug.Log("Resetting enemy...");

        gameObject.SetActive(true);
        enemyHealth = enemyMaxHealth;
        isDead = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = true;

        animator.SetBool(IsMoving, false);

        Debug.Log($"Enemigo reseteado: Salud = {enemyHealth}, Estado muerto = {isDead}");
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int GetHealth()
    {
        return enemyHealth;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(hitDamage);
        }
    }
}
