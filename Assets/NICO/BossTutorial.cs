using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossTutorial : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int bossMaxHealth = 200;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attack1Damage = 30;
    [SerializeField] private int attack2Damage = 15;
    [SerializeField] private float visionRange = 30;  // Rango de visión del jefe

    private Transform player;
    private PlayerController playerController;
    private Animator animator;
    private Rigidbody2D rb;
    public int bossHealth;
    private bool isDead = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    [SerializeField] private Image healthBarBackground;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text bossNameText;
    [SerializeField] private string bossName = "Valthorr";

    [SerializeField] private GameObject staffHitbox;

    [Header("Boss Music")]
    [SerializeField] private AudioSource bossMusic; // 🎵 Música del jefe

    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Attack1 = Animator.StringToHash("Attack1");
    private static readonly int Attack2 = Animator.StringToHash("Attack2");
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int Hit = Animator.StringToHash("Hit");

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerController = player.GetComponent<PlayerController>();
        bossHealth = bossMaxHealth;
        SetAttack(false);

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
            healthBarBackground.gameObject.SetActive(false); // Ocultar barra de vida al inicio
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossName;
            bossNameText.color = Color.red; // Nombre rojo al inicio
            bossNameText.gameObject.SetActive(false);
        }

        if (bossMusic != null)
        {
            bossMusic.Stop(); // Asegurarse de que la música no esté reproduciéndose al inicio
        }
    }

    private void UpdateBossNameColor()
    {
        if (bossNameText != null)
        {
            float healthPercent = (float)bossHealth / bossMaxHealth;
            bossNameText.color = Color.Lerp(Color.gray, Color.red, healthPercent);
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)bossHealth / bossMaxHealth;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Si el jugador está en el rango de visión, mostrar UI y reproducir música
        if (distanceToPlayer <= visionRange)
        {
            healthBarBackground.gameObject.SetActive(true);
            if (bossNameText != null) bossNameText.gameObject.SetActive(true);
            MoveTowardsPlayer();

            // 🎵 Reproducir música si aún no está sonando
            if (bossMusic != null && !bossMusic.isPlaying)
            {
                bossMusic.Play();
            }

            // Si el jugador está dentro del rango de ataque, atacar
            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                Attack();
            }
        }
        else
        {
            healthBarBackground.gameObject.SetActive(false);
            if (bossNameText != null) bossNameText.gameObject.SetActive(false);
            animator.SetBool(IsMoving, false);

            // 🎵 Detener la música si el jugador se aleja
            if (bossMusic != null && bossMusic.isPlaying)
            {
                bossMusic.Stop();
            }
        }

        UpdateBossNameColor();
    }

    private void MoveTowardsPlayer()
    {
        if (isAttacking || isDead) return;

        animator.SetBool(IsMoving, true);
        Vector2 direction = (player.position - transform.position).normalized;

        // Asegúrate de que solo se mueve en el eje X
        rb.MovePosition(new Vector2(rb.position.x + direction.x * moveSpeed * Time.fixedDeltaTime, rb.position.y));

        // Orienta al boss correctamente
        GetComponent<SpriteRenderer>().flipX = direction.x < 0;
    }

    private void Attack()
    {
        SetAttack(true);
        lastAttackTime = Time.time;

        if (bossHealth > bossMaxHealth / 2)
        {
            animator.SetTrigger(Attack1);
            Invoke(nameof(DelayedAttack1Damage), 0.5f);
        }
        else
        {
            animator.SetTrigger(Attack2);
            Invoke(nameof(DelayedAttack2Damage), 0.7f);
        }

        Invoke(nameof(EndAttack), 1.0f);
    }

    private void DelayedAttack1Damage()
    {
        DealDamage(attack1Damage);
    }

    private void DelayedAttack2Damage()
    {
        DealDamage(attack2Damage);
    }

    private void DealDamage(int damage)
    {
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            playerController.TakeDamage(damage);
            Vector2 direction = player.position.x < transform.position.x ? Vector2.left : Vector2.right;
            playerController.PushPlayer(direction.normalized, 25);
        }
    }

    private void EndAttack()
    {
        SetAttack(false);
    }

    private void SetAttack(bool isOn)
    {
        isAttacking = isOn;
        animator.SetBool(IsMoving, !isOn);
        staffHitbox.SetActive(isOn);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        bossHealth -= damage;
        animator.SetTrigger(Hit);

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)bossHealth / bossMaxHealth;
        }

        if (bossHealth <= 0)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        isDead = true;

        animator.SetBool(IsMoving, false);
        animator.SetTrigger(Die);
        animator.SetBool(Hit, false);
        SetAttack(false);

        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        rb.gravityScale = 0;

        healthBarBackground.gameObject.SetActive(false);
        if (bossNameText != null) bossNameText.gameObject.SetActive(false);

        // 🎵 Detener la música al morir
        if (bossMusic != null && bossMusic.isPlaying)
        {
            bossMusic.Stop();
        }

        StartCoroutine(Disintegrate());
    }
    public void ResetHealth()
    {
        bossHealth = bossMaxHealth; // Restaurar la salud al máximo
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f; // Actualizar la barra de vida
        }
        Debug.Log("El jefe ha restaurado su salud completamente.");
    }

    private IEnumerator Disintegrate()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float elapsedTime = 0f;
        float disintegrationTime = 2f;

        while (elapsedTime < disintegrationTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - (elapsedTime / disintegrationTime);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }


private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player.gameObject)
        {
            // Determina el daño según el ataque
            int damage = bossHealth > bossMaxHealth / 2 ? attack1Damage : attack2Damage;
            playerController.TakeDamage(damage);
            Vector2 direction = player.position.x < transform.position.x ? Vector2.left : Vector2.right;
            playerController.PushPlayer(direction.normalized, 25);
            Debug.Log("💥 El jefe golpeó al jugador. Daño: " + damage);
        }
    }
}
