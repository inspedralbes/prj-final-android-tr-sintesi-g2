using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.UIElements; 

public class PlayerController : MonoBehaviour
{
    // **Serialized Fields**
    [SerializeField] private UIDocument deathUIDocument;
    private VisualElement deathUI;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 21f;
    [SerializeField] private Sensor_HeroKnight groundSensor;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int playerDamage = 50;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip runSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private BarraVida barraVida;
    [SerializeField] private TMPro.TextMeshProUGUI coinText;
    [SerializeField] private GameObject swordHitboxRight;
    [SerializeField] private GameObject swordHitboxLeft;
    [SerializeField] private float attackCooldown = 1f;

    // **Sistema de bolas de fuego y maná**
    [Header("Sistema de Magia")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 15f;
    [SerializeField] private int fireballDamage = 25;
    [SerializeField] private float fireballCooldown = 0.5f;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private Transform fireballSpawnPoint;

    [Header("Sistema de Maná")]
    [SerializeField] private int maxMana = 100;
    [SerializeField] private int fireballManaCost = 30;
    [SerializeField] private float manaRegenRate = 10f;
    [SerializeField] private BarraMana barraMana;

    // **Dash Variables**
    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private GameObject dashEffect;

    // **Private Variables**
    private int currentHealth;
    private int currentMana;
    private int totalCoins = 0;
    private bool isDead = false;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction fireballAction;
    private InputAction dashAction;
    private float moveDirection;
    private bool isJumping;
    private bool isGrounded;
    private bool isStunned;
    private bool isDashing = false;
    private float lastDashTime = 5f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool _hasToJump = false;
    private float lastAttackTime = 0f;
    private float lastFireballTime = 0f;
    private AudioSource audioSource;
    private bool isRegeneratingMana = true;

    // **Animation Parameters**
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int IsCasting = Animator.StringToHash("IsCasting");
    private static readonly int AirSpeed = Animator.StringToHash("AirSpeed");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int IsDead = Animator.StringToHash("IsDead");
    private static readonly int IsDashing = Animator.StringToHash("IsDashing");

    private void Awake()
    {
        Debug.Log("PlayerController Awake iniciado");

        // Inicializaciones básicas
        currentHealth = maxHealth;
        currentMana = maxMana;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Configuración del AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurar Input System
        SetupInputActions();

        // Inicializar UIs
        if (barraVida != null)
        {
            barraVida.ActualizarBarraVida(currentHealth, maxHealth);
        }

        if (barraMana != null)
        {
            barraMana.ActualizarBarraMana(currentMana, maxMana);
        }

        if (deathUIDocument != null)
        {
            deathUI = deathUIDocument.rootVisualElement.Q<VisualElement>("DeathScreen");
            if (deathUI != null)
            {
                deathUI.style.display = DisplayStyle.None;
            }
        }

        PlayerPrefs.GetFloat("position_x", transform.position.x);

        Debug.Log("PlayerController Awake completado");
        

    }

    private void SetupInputActions()
    {
        Debug.Log("Configurando Input Actions...");
        
        // Verificar si el InputActionAsset está asignado
        if (inputActions == null)
        {
            Debug.LogError("Input Actions Asset no está asignado en el Inspector!");
            return;
        }
        
        try {
            // Encontrar el action map "Player"
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap == null)
            {
                Debug.LogError("No se encontró el Action Map 'Player'!");
                return;
            }
            
            // Asignar las acciones
            moveAction = playerActionMap.FindAction("Move");
            if (moveAction == null) Debug.LogError("No se encontró la acción 'Move'!");
            
            jumpAction = playerActionMap.FindAction("Jump");
            if (jumpAction == null) Debug.LogError("No se encontró la acción 'Jump'!");
            
            attackAction = playerActionMap.FindAction("Attack");
            if (attackAction == null) Debug.LogError("No se encontró la acción 'Attack'!");
            
            // Buscar la acción de bola de fuego
            fireballAction = playerActionMap.FindAction("Fireball");
            if (fireballAction == null)
            {
                Debug.LogWarning("No se encontró la acción 'Fireball'. Asegúrate de añadirla a tu Input Action Asset.");
            }
            
            // Buscar la acción de dash
            dashAction = playerActionMap.FindAction("Dash");
            if (dashAction == null)
            {
                Debug.LogWarning("No se encontró la acción 'Dash'. Asegúrate de añadirla a tu Input Action Asset.");
            }
            
            // Suscribirse a los eventos SOLO si las acciones existen
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }
            
            if (jumpAction != null)
            {
                jumpAction.performed += OnJump;
            }
            
            if (attackAction != null)
            {
                attackAction.performed += OnAttack;
            }
            
            if (fireballAction != null)
            {
                fireballAction.performed += OnFireball;
            }
            
            if (dashAction != null)
            {
                dashAction.performed += OnDash;
            }
            
            Debug.Log("Input Actions configuradas correctamente");
        } catch (System.Exception e) {
            Debug.LogError("Error al configurar Input Actions: " + e.Message);
        }
    }

    private void OnEnable()
    {
        Debug.Log("PlayerController OnEnable iniciado");
        
        try {
            // Habilitar las acciones si están definidas
            if (moveAction != null) moveAction.Enable();
            if (jumpAction != null) jumpAction.Enable();
            if (attackAction != null) attackAction.Enable();
            if (fireballAction != null) fireballAction.Enable();
            if (dashAction != null) dashAction.Enable();
            
            Debug.Log("Acciones de input habilitadas");
        } catch (System.Exception e) {
            Debug.LogError("Error al habilitar Input Actions: " + e.Message);
        }
    }

    private void OnDisable()
    {
        // Deshabilitar las acciones si están definidas
        moveAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
        fireballAction?.Disable();
        dashAction?.Disable();
    }

    private void OnDestroy()
    {
        // Desuscribirse de los eventos para evitar memory leaks
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }
        
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
        }
        
        if (attackAction != null)
        {
            attackAction.performed -= OnAttack;
        }
        
        if (fireballAction != null)
        {
            fireballAction.performed -= OnFireball;
        }
        
        if (dashAction != null)
        {
            dashAction.performed -= OnDash;
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (isDead) return;

        moveDirection = context.ReadValue<Vector2>().x;
        UpdateAnimationState();
        FlipSprite();

        bool isMoving = Mathf.Abs(moveDirection) > 0 && isGrounded;

        if (isMoving)
        {
            if (!audioSource.isPlaying)
            {
                PlaySound(runSound);
            }
        }
        else
        {
            audioSource.Stop();
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && !animator.GetBool(IsJumping) && !isDead && !isDashing)
        {
            _hasToJump = true;
            animator.SetBool(IsJumping, true);
            Debug.Log("Saltar: IsJumping activado");

            audioSource.Stop();
            PlaySound(jumpSound);
        }
    }

    // Método para rellenar el maná al máximo o a una cantidad específica
    public void RefillMana(int amount)
    {
        // Restaurar el maná al valor especificado o al máximo si es mayor
        currentMana = Mathf.Min(maxMana, amount);
        
        // Actualizar la barra de maná visual
        if (barraMana != null)
        {
            barraMana.ActualizarBarraMana(currentMana, maxMana);
        }
        
        Debug.Log($"Maná del jugador recargado: {currentMana}/{maxMana}");
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (isDead || isDashing || Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        animator.SetBool(IsAttacking, true);
        Debug.Log("Atacar: IsAttacking activado");

        if (spriteRenderer.flipX)
        {
            swordHitboxLeft.SetActive(true);
            swordHitboxRight.SetActive(false);
        }
        else
        {
            swordHitboxLeft.SetActive(false);
            swordHitboxRight.SetActive(true);
        }
        Invoke(nameof(FinishAttack), 0.5f); 
        Invoke(nameof(DisableSwordHitboxes), 0.2f);
        StartCoroutine(DealDamageAfterDelay(0.3f));
        PlaySound(attackSound);
    }

    private void OnFireball(InputAction.CallbackContext context)
    {
        if (isDead || isDashing || Time.time - lastFireballTime < fireballCooldown) return;

        if (currentMana < fireballManaCost)
        {
            Debug.Log("No hay suficiente maná para lanzar una bola de fuego");
            return;
        }

        UseMana(fireballManaCost);
        lastFireballTime = Time.time;
        animator.SetBool(IsCasting, true);
        ShootFireball();
        
        if (fireballSound != null)
        {
            PlaySound(fireballSound);
        }
        
        Invoke(nameof(FinishCasting), 0.3f);
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (isDead || isDashing || Time.time - lastDashTime < dashCooldown || !isGrounded) return;

        StartCoroutine(PerformDash());
    }

    private IEnumerator PerformDash()
{
    isDashing = true;
    lastDashTime = Time.time;
    
    // Activar la animación de dash
    animator.SetBool(IsDashing, true);
    
    // Guardar velocidad original para restaurarla después
    Vector2 originalVelocity = rb.linearVelocity;
    
    // Dirección del dash basada en hacia dónde mira el personaje
    float dashDirection = spriteRenderer.flipX ? -1f : 1f;
    
    // Si no hay dirección de movimiento (jugador quieto), usar dirección de sprite
    if (moveDirection == 0) 
    {
        rb.linearVelocity = new Vector2(dashDirection * dashForce, 0);
    }
    else 
    {
        // Si hay movimiento, usar esa dirección para el dash
        rb.linearVelocity = new Vector2(moveDirection * dashForce, 0);
    }
    
    // Reproducir sonido de dash
    PlaySound(dashSound);
    
    // Activar efecto visual si existe
    if (dashEffect != null)
    {
        dashEffect.SetActive(true);
    }
    
    // Esperar la duración del dash
    yield return new WaitForSeconds(dashDuration);
    
    // Desactivar animación de dash
    animator.SetBool(IsDashing, false);
    
    // Desactivar efecto visual si existe
    if (dashEffect != null)
    {
        dashEffect.SetActive(false);
    }
    
    // Restaurar la velocidad (con la componente Y original para no interrumpir saltos)
    rb.linearVelocity = new Vector2(originalVelocity.x, rb.linearVelocity.y);
    
    isDashing = false;
}

    private void ShootFireball()
    {
        if (fireballPrefab == null)
        {
            Debug.LogError("No se ha asignado el prefab de la bola de fuego");
            return;
        }

        Vector3 spawnPosition;
        if (fireballSpawnPoint != null)
        {
            spawnPosition = fireballSpawnPoint.position;
        }
        else
        {
            float offsetX = spriteRenderer.flipX ? -1f : 1f;
            spawnPosition = transform.position + new Vector3(offsetX, 0.5f, 0);
        }

        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        
        Fireball fireballScript = fireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            int direction = spriteRenderer.flipX ? -1 : 1;
            fireballScript.Initialize(direction, fireballSpeed, fireballDamage);
        }
        else
        {
            Rigidbody2D fireballRb = fireball.GetComponent<Rigidbody2D>();
            if (fireballRb == null)
            {
                fireballRb = fireball.AddComponent<Rigidbody2D>();
                fireballRb.gravityScale = 0;
            }
            
            float direction = spriteRenderer.flipX ? -1 : 1;
            fireballRb.linearVelocity = new Vector2(direction * fireballSpeed, 0);
            Destroy(fireball, 3f);
        }
        
        Debug.Log("Bola de fuego disparada");
    }

    private void FinishCasting()
    {
        animator.SetBool(IsCasting, false);
    }

    private void UseMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        
        if (barraMana != null)
        {
            barraMana.ActualizarBarraMana(currentMana, maxMana);
        }
        
        Debug.Log($"Maná usado: {amount} | Maná actual: {currentMana}");
    }

    private void RegenerateMana()
    {
        if (!isRegeneratingMana || isDead || isDashing) return;
        
        if (currentMana < maxMana)
        {
            float manaToRegenerate = manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Min(maxMana, currentMana + Mathf.RoundToInt(manaToRegenerate));
            
            if (barraMana != null)
            {
                barraMana.ActualizarBarraMana(currentMana, maxMana);
            }
        }
    }

    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        float attackRange = 1f;
        int tilesToBreak = 3;
        float tileSize = 1f;

        Vector2 attackDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 boxCenter = (Vector2)transform.position + (attackDirection * tileSize / 2);
        Vector2 boxSize = new Vector2(tileSize, tileSize * tilesToBreak);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);

        /* foreach (var hit in hitEnemies)
         {
             if (hit.CompareTag("Enemy"))
             {
                 hit.GetComponent<EnemyAI>()?.TakeDamage(playerDamage);
                 Debug.Log("¡Enemigo golpeado!");
             }
             else if (hit.CompareTag("Boss"))
             {
                 hit.GetComponent<BossTutorial>()?.TakeDamage(playerDamage);
                 Debug.Log("¡Jefe golpeado!");
             }
         }*/

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
        if (isDead) return;

        if (isDashing) return;

        if (!isStunned)
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        if (_hasToJump && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _hasToJump = false;
        }

        float airSpeed = rb.linearVelocity.y;
        animator.SetFloat(AirSpeed, airSpeed);
        animator.SetBool(IsFalling, !isGrounded);

        if (!isGrounded && animator.GetBool(IsJumping))
        {
            animator.SetBool(IsJumping, false);
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Regenerar maná cada frame
        RegenerateMana();

        bool wasGrounded = isGrounded;
        isGrounded = groundSensor.IsGrounded();

        animator.SetBool("Grounded", isGrounded);
        
        if (!isDashing)
        {
            animator.SetBool(IsMoving, Mathf.Abs(moveDirection) > 0);
            animator.SetBool(IsJumping, !isGrounded && rb.linearVelocity.y > 0);
            animator.SetBool(IsFalling, !isGrounded && rb.linearVelocity.y < 0);
            animator.SetFloat(AirSpeed, rb.linearVelocity.y);
        }

        if (isGrounded && !wasGrounded && Mathf.Abs(moveDirection) > 0 && !isDashing)
        {
            PlaySound(runSound);
        }
    }

    private void UpdateAnimationState()
    {
        if (isDashing) return;
        
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
        if (isDead || isDashing) return;

        currentHealth -= damage;
        Debug.Log($"¡Jugador fue golpeado! Daño recibido: {damage} | Salud actual: {currentHealth}");

        animator.SetTrigger(Hit);

        if (barraVida != null)
        {
            barraVida.ActualizarBarraVida(currentHealth, maxHealth);
        }

        PlaySound(hitSound);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (isGrounded && Mathf.Abs(moveDirection) > 0)
        {
            Invoke(nameof(ResumeRunningSound), 0.2f);
        }
    }

    private void ResumeRunningSound()
    {
        if (isGrounded && Mathf.Abs(moveDirection) > 0 && audioSource.clip != runSound)
        {
            PlaySound(runSound);
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        animator.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;

        moveAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
        fireballAction?.Disable();
        dashAction?.Disable();

        PlaySound(deathSound);

        Debug.Log("El personaje ha muerto.");

        animator.SetBool(IsMoving, false);
        animator.SetBool(IsJumping, false);
        animator.SetBool(IsFalling, false);
        animator.SetFloat(AirSpeed, 0);
        
        if (deathUI != null)
        {
            deathUI.style.display = DisplayStyle.Flex;
        }

        Invoke(nameof(Respawn), 3f);
    }

    private void Respawn()
    {
        float x = PlayerPrefs.GetFloat("position_x", transform.position.x);
        float y = PlayerPrefs.GetFloat("position_y", transform.position.y);
        Debug.Log($"Reapareciendo en: {x}, {y}");
        transform.position = new Vector2(x, y);

        currentHealth = PlayerPrefs.GetInt("health", maxHealth);
        barraVida?.ActualizarBarraVida(currentHealth, maxHealth);
        
        // Restaurar maná al respawnear
        currentMana = maxMana;
        barraMana?.ActualizarBarraMana(currentMana, maxMana);

        isDead = false;
        isDashing = false;
        animator.ResetTrigger("Die");
        animator.Play("idle", 0, 0);

        animator.SetBool(IsMoving, false);
        animator.SetBool(IsJumping, false);
        animator.SetBool(IsFalling, false);
        animator.SetBool(IsCasting, false);
        animator.SetFloat(AirSpeed, 0);

        moveDirection = 0;
        rb.linearVelocity = Vector2.zero;

        moveAction?.Enable();
        jumpAction?.Enable();
        attackAction?.Enable();
        fireballAction?.Enable();
        dashAction?.Enable();

        Debug.Log("El personaje ha reaparecido correctamente.");

        BossTutorial boss = FindObjectOfType<BossTutorial>();
        if (boss != null)
        {
            boss.ResetHealth();
            Debug.Log("El jefe ha recuperado toda su vida.");
        }

        if (deathUI != null)
        {
            deathUI.style.display = DisplayStyle.None;
        }
    }

    // Métodos para obtener información
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
        barraVida?.ActualizarBarraVida(currentHealth, maxHealth);
    }

    private void Start()
    {
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        Debug.Log($"Monedas obtenidas: {amount} | Total: {totalCoins}");
        UpdateCoinUI();
        PlayerPrefs.SetInt("temp_coins", totalCoins);
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = "" + totalCoins;
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
            TakeDamage(100);
        }

        Debug.Log(collision.gameObject.name);

        if (collision.CompareTag("Boss") && (animator.GetBool(IsAttacking)))
        {
            BossTutorial boss = collision.GetComponent<BossTutorial>();
            if (boss != null)
            {
                boss.TakeDamage(playerDamage);
                Debug.Log("🔥 El jugador golpeó al jefe. Daño: " + playerDamage);
            }
        }

        if (collision.CompareTag("Enemy") && (animator.GetBool(IsAttacking)))
        {
            EnemyAI enemy = collision.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(playerDamage);
                Debug.Log("🔥 El jugador golpeó al enemigo. Daño: " + playerDamage);
            }
        }
    }

    public void PushPlayer(Vector2 pushDirection, int forceImpulse)
    {
        if (isDashing) return;
        
        isStunned = true;
        rb.AddForce(pushDirection * forceImpulse, ForceMode2D.Impulse);
        Invoke("Unstun", 0.5f);
    }

    private void Unstun()
    {
        isStunned = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
    }
}