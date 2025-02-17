using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // **Serialized Fields**
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 21f;
    [SerializeField] private Sensor_HeroKnight groundSensor;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int playerDamage = 50;
    [SerializeField] private AudioClip jumpSound;       // Sonido de salto
    [SerializeField] private AudioClip attackSound;     // Sonido de ataque
    [SerializeField] private AudioClip hitSound;        // Sonido al recibir daño
    [SerializeField] private AudioClip deathSound;      // Sonido de muerte
    [SerializeField] private AudioClip runSound;        // Sonido de correr
    //[SerializeField] private AudioClip fallSound;       // Sonido de caer // Eliminado
    [SerializeField] private BarraVida barraVida;  // Referencia al script BarraVida
    [SerializeField] private TMPro.TextMeshProUGUI coinText;
    [SerializeField] private GameObject swordHitboxRight;
    [SerializeField] private GameObject swordHitboxLeft; // Nueva hitbox para el lado izquierdo
    [SerializeField] private float attackCooldown = 1f;  // Tiempo de cooldown entre ataques

    // **Private Variables**
    private int currentHealth;
    private int totalCoins = 0; // Monedas actuales
    private bool isDead = false;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private float moveDirection;
    private bool isJumping;
    private bool isGrounded;
    private bool isStunned;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool _hasToJump = false;
    private float lastAttackTime = 0f;  // Tiempo en el que se realizó el último ataque
    private AudioSource audioSource;      // Componente AudioSource

    // **Animation Parameters**
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int AirSpeed = Animator.StringToHash("AirSpeed");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int IsDead = Animator.StringToHash("IsDead");

    private void Awake()
    {
        currentHealth = maxHealth;  // Inicializa la salud
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>(); // Obtener el componente AudioSource

        if (audioSource == null)
        {
            // Si no hay AudioSource, añadir uno
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        var playerActionMap = inputActions.FindActionMap("Player");
        moveAction = playerActionMap.FindAction("Move");
        jumpAction = playerActionMap.FindAction("Jump");
        attackAction = playerActionMap.FindAction("Attack");

        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        jumpAction.performed += OnJump;
        attackAction.performed += OnAttack;

        // Inicializa la barra de vida
        if (barraVida != null)
        {
            barraVida.ActualizarBarraVida(currentHealth, maxHealth);
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (isDead) return; // No permitir movimiento si está muerto

        moveDirection = context.ReadValue<Vector2>().x;
        UpdateAnimationState();
        FlipSprite();

        bool isMoving = Mathf.Abs(moveDirection) > 0 && isGrounded;

        if (isMoving)
        {
            if (!audioSource.isPlaying) // Solo reproducir si no está sonando
            {
                PlaySound(runSound);
            }
        }
        else
        {
            audioSource.Stop(); // Detener el sonido cuando se deja de mover
        }
    }


    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && !animator.GetBool(IsJumping) && !isDead)
        {
            _hasToJump = true;
            animator.SetBool(IsJumping, true);
            Debug.Log("Saltar: IsJumping activado");

            audioSource.Stop(); // Detener cualquier sonido actual antes de saltar
            PlaySound(jumpSound); // Reproducir sonido de salto
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (isDead || Time.time - lastAttackTime < attackCooldown) return; // Si está muerto o no ha pasado el cooldown, no realizar ataque

        lastAttackTime = Time.time; // Actualizar el tiempo del último ataque

        animator.SetBool(IsAttacking, true);
        Debug.Log("Atacar: IsAttacking activado");

        // 🔹 Activar la hitbox de la espada dependiendo de la dirección del jugador
        if (spriteRenderer.flipX)
        {
            swordHitboxLeft.SetActive(true);  // Si el jugador está mirando hacia la izquierda
            swordHitboxRight.SetActive(false);
        }
        else
        {
            swordHitboxLeft.SetActive(false);
            swordHitboxRight.SetActive(true);  // Si el jugador está mirando hacia la derecha
        }

        Invoke(nameof(DisableSwordHitboxes), 0.2f); // Desactivar ambas hitboxes después de 0.2s

        // Iniciar la Coroutine para aplicar el daño después de 0.3 segundos
        StartCoroutine(DealDamageAfterDelay(0.3f)); // El daño se aplica después de 0.3 segundos

        PlaySound(attackSound); // Reproducir sonido de ataque
    }

    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Espera el tiempo de retraso

        // Código para realizar el daño
        float attackRange = 1f; // Rango del ataque para enemigos
        int tilesToBreak = 3; // Número de bloques a romper en el eje Y
        float tileSize = 1f;  // Tamaño de cada tile

        Vector2 attackDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 boxCenter = (Vector2)transform.position + (attackDirection * tileSize / 2); // Centrar el box
        Vector2 boxSize = new Vector2(tileSize, tileSize * tilesToBreak); // Área de impacto en altura

        // Buscar enemigos dentro del rango de ataque
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);

        /* foreach (var hit in hitEnemies)
         {
             if (hit.CompareTag("Enemy")) // Verifica si es enemigo normal
             {
                 hit.GetComponent<EnemyAI>()?.TakeDamage(playerDamage); // Usa el daño serializado del jugador
                 Debug.Log("¡Enemigo golpeado!");
             }
             else if (hit.CompareTag("Boss")) // Verifica si es el jefe
             {
                 hit.GetComponent<BossTutorial>()?.TakeDamage(playerDamage); // Usa el daño serializado del jugador
                 Debug.Log("¡Jefe golpeado!");
             }
         }*/

        // Buscar y romper bloques en un área vertical
        RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCenter, boxSize, 0, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("FalseWall"))
            {
                hit.collider.GetComponent<FalseWall>()?.TakeDamage(hit.point);
                Debug.Log("¡Bloque destruido!");
            }
        }
    }

    // 🔹 Método para desactivar las hitboxes después del ataque
    private void DisableSwordHitboxes()
    {
        swordHitboxLeft.SetActive(false);
        swordHitboxRight.SetActive(false);
    }

    public void FinishAttack()
    {
        animator.SetBool(IsAttacking, false);
        Debug.Log("Ataque finalizado, IsAttacking desactivado");
    }

    private void FixedUpdate()
    {
        if (isDead) return; // No realizar lógica si está muerto

        if (!isStunned)
            // Mover al personaje
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        // Si se ha marcado el salto, realizarlo solo si está en el suelo
        if (_hasToJump && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _hasToJump = false;
        }

        // Actualizar el parámetro AirSpeed en base a la velocidad en el eje Y
        float airSpeed = rb.linearVelocity.y;

        animator.SetFloat(AirSpeed, airSpeed);

        animator.SetBool(IsFalling, !isGrounded);

        // Desactivar IsJumping cuando el personaje esté en el aire
        if (!isGrounded && animator.GetBool(IsJumping))
        {
            animator.SetBool(IsJumping, false);
        }
    }

    private void Update()
    {
        if (isDead) return; // No realizar lógica si está muerto

        // Detectar si el personaje está en el suelo utilizando el sensor
        bool wasGrounded = isGrounded; // Guardar estado anterior
        isGrounded = groundSensor.IsGrounded();

        animator.SetBool("Grounded", isGrounded); // Actualizar el parámetro Grounded
        animator.SetBool(IsMoving, Mathf.Abs(moveDirection) > 0);
        animator.SetBool(IsJumping, !isGrounded && rb.linearVelocity.y > 0);
        animator.SetBool(IsFalling, !isGrounded && rb.linearVelocity.y < 0);
        animator.SetFloat(AirSpeed, rb.linearVelocity.y);

        // Si acaba de aterrizar y sigue moviéndose, reproducir sonido de correr
        if (isGrounded && !wasGrounded && Mathf.Abs(moveDirection) > 0)
        {
            PlaySound(runSound);
        }
    }

    private void UpdateAnimationState()
    {
        bool isMoving = Mathf.Abs(moveDirection) > 0;
        animator.SetBool(IsMoving, isMoving);
    }

    private void FlipSprite()
    {
        if (moveDirection != 0)
            spriteRenderer.flipX = moveDirection < 0;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // No recibir daño si está muerto

        currentHealth -= damage;
        Debug.Log($"¡Jugador fue golpeado! Daño recibido: {damage} | Salud actual: {currentHealth}");

        animator.SetTrigger(Hit);

        // Actualizar la barra de vida
        if (barraVida != null)
        {
            barraVida.ActualizarBarraVida(currentHealth, maxHealth);
        }

        PlaySound(hitSound); // Reproducir sonido al recibir daño

        if (currentHealth <= 0)
        {
            Die();
            return; // Evita ejecutar el código siguiente si el jugador muere
        }

        // Si el jugador sigue en el suelo y moviéndose, reproducir sonido de correr
        if (isGrounded && Mathf.Abs(moveDirection) > 0)
        {
            Invoke(nameof(ResumeRunningSound), 0.2f); // Pequeño delay para evitar cortes bruscos
        }
    }

    // Método para reanudar el sonido de correr si el jugador sigue moviéndose
    private void ResumeRunningSound()
    {
        if (isGrounded && Mathf.Abs(moveDirection) > 0 && audioSource.clip != runSound)
        {
            PlaySound(runSound);
        }
    }

    private void Die()
    {
        if (isDead) return; // Evitar llamar varias veces

        isDead = true;
        animator.SetTrigger("Die"); // Usa SetTrigger en lugar de SetBool
        rb.linearVelocity = Vector2.zero; // Detener el movimiento
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();

        PlaySound(deathSound); // Reproducir sonido de muerte

        Debug.Log("El personaje ha muerto.");
        Invoke(nameof(Respawn), 6f); // Esperar antes de respawnear
    }

    private void Respawn()
    {
        // Restaurar la posición del jugador desde PlayerPrefs o usar valores predeterminados
        float x = PlayerPrefs.GetFloat("position_x", transform.position.x);
        float y = PlayerPrefs.GetFloat("position_y", transform.position.y);
        transform.position = new Vector2(x, y);

        // Restaurar la salud
        currentHealth = PlayerPrefs.GetInt("health", maxHealth);
        barraVida.ActualizarBarraVida(currentHealth, maxHealth);

        // Restablecer estado
        isDead = false;

        // **Reiniciar el trigger de muerte**
        animator.ResetTrigger("Die");

        // **Forzar la animación inicial inmediatamente**
        animator.Play("idle", 0, 0); // "Idle" debe coincidir con el nombre de la animación en tu Animator

        // Restablecer otros estados de animación
        animator.SetBool(IsMoving, false);
        animator.SetBool(IsJumping, false);
        animator.SetBool(IsFalling, false);
        animator.SetFloat(AirSpeed, 0);

        // Reactivar controles
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();

        Debug.Log("El personaje ha reaparecido correctamente.");
        BossTutorial boss = FindObjectOfType<BossTutorial>(); // Buscar la instancia del jefe
        if (boss != null)
        {
            boss.ResetHealth();
            Debug.Log("El jefe ha recuperado toda su vida.");
        }
    }

    // Método para obtener la salud máxima
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public int GetTotalCoins()
    {
        return totalCoins;
    }

    public void SetCurrentHealth(int health)
    {
        currentHealth = health;

        // Actualizar la barra de vida visualmente
        if (barraVida != null)
        {
            barraVida.ActualizarBarraVida(currentHealth, maxHealth);
        }
    }

    private void Start()
    {
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        Debug.Log($"Monedas obtenidas: {amount} | Total: {totalCoins}");
        UpdateCoinUI(); // Actualizar la UI cuando se obtienen monedas
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = "" + totalCoins; // Actualiza el texto en pantalla
        }
        else
        {
            Debug.LogWarning("¡No se ha asignado CoinText en el inspector!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Spikes"))
        {
            Debug.Log("El personaje tocó los pinchos y ha muerto.");
            TakeDamage(100); // Llama al método de muerte del personaje
        }

        // Muestra el nombre del objeto con el que ha colisionado
        Debug.Log(collision.gameObject.name);

        // Si el jugador golpea al jefe
        if (collision.CompareTag("Boss") && animator.GetBool(IsAttacking)) // Solo daña si está atacando
        {

            BossTutorial boss = collision.GetComponent<BossTutorial>();
            if (boss != null)
            {
                boss.TakeDamage(playerDamage);  // Usa el daño serializado del jugador
                Debug.Log("🔥 El jugador golpeó al jefe. Daño: " + playerDamage);
            }


        }

        // Si el jugador golpea un enemigo
        if (collision.CompareTag("Enemy") && animator.GetBool(IsAttacking)) // Solo daña si está atacando
        {

            EnemyAI enemy = collision.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(playerDamage); // Usa el daño serializado del jugador
                Debug.Log("🔥 El jugador golpeó al enemigo. Daño: " + playerDamage);
            }
        }

    }
    public void PushPlayer(Vector2 pushDirection, int forceImpulse)
    {
        isStunned = true;
        rb.AddForce(pushDirection * forceImpulse, ForceMode2D.Impulse);
        Invoke("Unstun", 0.5f);
    }

    private void Unstun()
    {
        isStunned = false;
    }

    // Método para reproducir los sonidos
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.Stop(); // Detener el sonido actual
            audioSource.PlayOneShot(clip);
        }
    }
}
