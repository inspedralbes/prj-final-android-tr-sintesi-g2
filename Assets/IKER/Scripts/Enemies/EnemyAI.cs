using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
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

    private void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        enemyHealth = enemyMaxHealth;

        InitEnemy();
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

        // En lugar de cambiar la escala en X, usa FlipX
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0; // Voltea el sprite sin cambiar la escala
        }
    }

    protected abstract void Attack();

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;
        Debug.Log("enemyAI: Take Damage");
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

        // En lugar de destruir, simplemente lo desactivamos
        Invoke(nameof(DeactivateEnemy), GetAnimationClipLength("Die"));
    }

    private void DeactivateEnemy()
    {
        gameObject.SetActive(false); // Desactiva el enemigo
    }


    // Método opcional para inicializar lógica específica en subclases
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

    // Nuevo método para resetear los enemigos
    public void ResetEnemy()
    {
        Debug.Log("Resetting enemy...");

        gameObject.SetActive(true); // Reactivar el enemigo si estaba desactivado

        // Resetear salud y estado
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
        return isDead; // Retorna si el enemigo está muerto o no
    }

    public int GetHealth()
    {
        return enemyHealth; // Retorna la salud actual del enemigo
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(hitDamage);
        }
    }
}
