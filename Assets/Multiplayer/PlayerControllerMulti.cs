using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UIElements;
using Mirror;
using System;
using TMPro;

public class PlayerControllerMulti : NetworkBehaviour
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
    [SerializeField] private BarraVidaMulti barraVida;
    [SerializeField] private TMPro.TextMeshProUGUI coinText;
    [SerializeField] private GameObject swordHitboxRight;
    [SerializeField] private GameObject swordHitboxLeft;
    [SerializeField] private float attackCooldown = 1f;

    // **Sistema de bolas de fuego y manÃ¡**
    [Header("Sistema de Magia")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 15f;
    [SerializeField] private int fireballDamage = 25;
    [SerializeField] private float fireballCooldown = 0.5f;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private Transform fireballSpawnPoint;

    [Header("Sistema de ManÃ¡")]
    [SerializeField] private int maxMana = 100;
    [SerializeField] private int fireballManaCost = 30;
    [SerializeField] private float manaRegenRate = 10f;
    [SerializeField] private BarraManaMulti barraMana;

    [Header("Sistema de vista HUD")]
    [SerializeField] private GameObject HUDPlayerHealth;
    [SerializeField] private GameObject HUDPlayerMana;
    [SerializeField] private GameObject HUDPlayer;
    [SerializeField] private TMP_Text nameText;

    // **Dash Variables**
    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private GameObject dashEffect;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    [SyncVar(hook = nameof(OnManaChanged))]
    private int currentMana;

    [SyncVar(hook = nameof(OnCoinsChanged))]
    private int totalCoins = 0;

    [SyncVar(hook = nameof(OnIsDeadChanged))]
    private bool isDead = false;

    [SyncVar(hook = nameof(OnFlipChanged))]
    private bool isFlipped = false;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    // **Private Variables**
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

    #region Unity Lifecycle Methods

    private void Awake()
    {
        Debug.Log("PlayerController Awake iniciado");

        // Inicializaciones de componentes basicos 
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Inicializacion de UIs
        if (deathUIDocument != null)
        {
            deathUI = deathUIDocument.rootVisualElement.Q<VisualElement>("DeathScreen");
            if (deathUI != null)
            {
                deathUI.style.display = DisplayStyle.None;
            }
        }

        Debug.Log("PlayerController Awake completado");
    }

     public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Debug.Log($"Jugador local: {NetworkClient.localPlayer}");
        Debug.Log("OnStartLocalPlayer llamado: soy el jugador local.");

        // Llamar al servidor para establecer el nombre
        string nickname = PlayerPrefs.GetString("nickname", "Jugador");

        CmdSetPlayerName(nickname);
        Debug.Log($"Nombre del jugador local: {playerName}");

        // Desactivar HUDs para el jugador local
        HUDPlayerHealth.SetActive(false);
        HUDPlayerMana.SetActive(false);
        SetupInputActions();
        inputActions.FindActionMap("Player").Enable();

        // Actualizar interfaz solo del jugador local
        if (barraVida == null)
        {
            barraVida = GetComponentInChildren<BarraVidaMulti>();
        }

        

        // Inicializar las barras correctamente
        if (barraVida != null && barraMana != null && isLocalPlayer)
        {
            barraVida.InicializarVida();
            barraMana.InicializarMana();
            currentMana = maxMana;
            currentHealth = maxHealth;
            barraVida.ActualizarBarraVida(currentHealth, maxHealth);
            barraMana.ActualizarBarraMana(currentMana, maxMana);
        } 
        else
        {
            Debug.LogWarning("No se encontró la barra de vida en los hijos.");
        }
    }

    [Command]
    void CmdSetPlayerName(string nickname)
    {
        // string nickname = PlayerPrefs.GetString("Nickname");
        playerName = nickname;
    }

    void OnNameChanged(string oldName, string newName)
    {
    if (nameText != null)
    {
        nameText.text = newName;
    }
    else
    {
        Debug.LogWarning("nameText es null en OnNameChanged");
    }
}


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer && HUDPlayer != null)
        {
            HUDPlayer.SetActive(false);
            Debug.Log("Desactivando barras del jugador remoto");
        }
    }
    // 2. Agregar callback de cambio de estado de muerte
    void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[MUERTE] OnIsDeadChanged: {oldValue} -> {newValue}");
        // Aplicar el estado de muerte visualmente a todos los clientes
        if (newValue && !oldValue)
        {
            animator.SetTrigger("Die");
            Debug.Log("El personaje ha muerto en ONISDEATHCHANGE.");
            rb.linearVelocity = Vector2.zero;

            // Manejo de muerte adicional específico para el jugador local
            if (isLocalPlayer)
            {
                // UI y manejo de controles para el jugador local
                DesactivarControles();
                if (deathUI != null)
                {
                    deathUI.style.display = DisplayStyle.Flex;
                }
                PlaySound(deathSound);

                RpcDie();
                Debug.Log("El personaje ha muerto en el cliente.");
            }
        }
    }
    // 5. MÃ©todo auxiliar para desactivar controles
    private void DesactivarControles()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
        fireballAction?.Disable();
        dashAction?.Disable();
    }

    [TargetRpc]
    void TargetSyncValues(NetworkConnection target, int health, int maxHp, int mana, int maxMn)
    {
        Debug.Log($"Cliente recibió valores sincronizados: HP={health}/{maxHp}, Mana={mana}/{maxMn} por el targetRCP");

        // Actualizar elementos de UI
        if (barraVida != null)
        {
            barraVida.ActualizarBarraVida(health, maxHp);
            Debug.Log($"Barra de vida sincronizada: {health}/{maxHp}");
        }

        if (barraMana != null)
        {
            barraMana.ActualizarBarraMana(mana, maxMn);
        }
    }
    private void SetupInputActions()
    {
        Debug.Log("Configurando Input Actions...");


        // Verificar si el InputActionAsset estÃ¡ asignado
        if (inputActions == null)
        {
            Debug.LogError("Input Actions Asset no estÃ¡ asignado en el Inspector!");
            return;
        }

        try
        {
            // Encontrar el action map "Player"
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap == null)
            {
                Debug.LogError("No se encontrÃ³ el Action Map 'Player'!");
                return;
            }

            // Asignar las acciones
            moveAction = playerActionMap.FindAction("Move");
            if (moveAction == null) Debug.LogError("No se encontrÃ³ la acciÃ³n 'Move'!");

            jumpAction = playerActionMap.FindAction("Jump");
            if (jumpAction == null) Debug.LogError("No se encontrÃ³ la acciÃ³n 'Jump'!");

            attackAction = playerActionMap.FindAction("Attack");
            if (attackAction == null) Debug.LogError("No se encontrÃ³ la acciÃ³n 'Attack'!");

            // Buscar la acciÃ³n de bola de fuego
            fireballAction = playerActionMap.FindAction("Fireball");
            if (fireballAction == null)
            {
                Debug.LogWarning("No se encontrÃ³ la acciÃ³n 'Fireball'. AsegÃºrate de aÃ±adirla a tu Input Action Asset.");
            }

            // Buscar la acciÃ³n de dash
            dashAction = playerActionMap.FindAction("Dash");
            if (dashAction == null)
            {
                Debug.LogWarning("No se encontrÃ³ la acciÃ³n 'Dash'. AsegÃºrate de aÃ±adirla a tu Input Action Asset.");
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
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al configurar Input Actions: " + e.Message);
        }
    }

    private void OnEnable()
    {
        if (!isLocalPlayer) return;

        Debug.Log("PlayerController OnEnable iniciado");

        try
        {
            // Habilitar las acciones si estÃ¡n definidas
            if (moveAction != null) moveAction.Enable();
            if (jumpAction != null) jumpAction.Enable();
            if (attackAction != null) attackAction.Enable();
            if (fireballAction != null) fireballAction.Enable();
            if (dashAction != null) dashAction.Enable();

            Debug.Log("Acciones de input habilitadas");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al habilitar Input Actions: " + e.Message);
        }
    }

    private void OnDisable()
    {
        if (!isLocalPlayer) return;

        // Deshabilitar las acciones si estÃ¡n definidas
        moveAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
        fireballAction?.Disable();
        dashAction?.Disable();
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer) return;

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

    #endregion

    #region Input Handlers
    private void Start()
    {
        // Asegurémonos de que el estado inicial sea consistente
        if (isLocalPlayer)
        {
            CmdUpdateInitialFlipState(spriteRenderer.flipX);
        }
    }

     private void OnMove(InputAction.CallbackContext context)
{
    Debug.Log("Jugador local: " + isLocalPlayer);
    if (isDead || !isLocalPlayer) return;
    
    // Leer la dirección de movimiento
    float moveInput = context.ReadValue<Vector2>().x;
    
    // Enviar comando de movimiento al servidor
    CmdMove(moveInput);
    
    // Control del audio local (para responsividad)
    bool isMoving = Mathf.Abs(moveInput) > 0 && isGrounded;
    if (isMoving && isLocalPlayer)
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

[Command]
private void CmdMove(float moveInput)
{
    // Validación en el servidor
    if (isDead) return;
    
    // Guardar la dirección de movimiento
    moveDirection = moveInput;
    
    // Actualizar el flip del sprite según la dirección
    bool shouldFlip = moveDirection < 0;
    Debug.Log($"[Server] Dirección de movimiento: {moveDirection}, Flip solicitado: {shouldFlip}");
    
    // Propagar el cambio a todos los clientes
    RpcUpdateMovement(moveDirection, shouldFlip);
}

[ClientRpc]
private void RpcUpdateMovement(float direction, bool flipX)
{
    // Actualizar la dirección de movimiento en todos los clientes
    moveDirection = direction;
    
    // Actualizar el flip del sprite en todos los clientes
    spriteRenderer.flipX = flipX;
    
    // Actualizar el estado de las animaciones
    UpdateAnimationState();
}


    [Command]
    private void CmdFlipSprite(bool flipX)
    {
        isFlipped = flipX;
    }

    private void UpdateAnimationState()
    {
        if (isDashing) return;

        bool isMoving = Mathf.Abs(moveDirection) > 0;
        animator.SetBool(IsMoving, isMoving);
    }

        private void OnFlipChanged(bool oldValue, bool newValue)
    {
        if(isLocalPlayer) return;
        spriteRenderer.flipX = newValue;
    }

    [Command]
    private void CmdUpdateInitialFlipState(bool flipState)
    {
        isFlipped = flipState;
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (isGrounded && !animator.GetBool(IsJumping) && !isDead && !isDashing)
        {
            CmdJump();
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || isDead || isDashing || Time.time - lastAttackTime < attackCooldown) return;

        CmdAttack();
    }

    private void OnFireball(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || isDead || isDashing || Time.time - lastFireballTime < fireballCooldown) return;

        if (currentMana < fireballManaCost)
        {
            Debug.Log("No hay suficiente manÃ¡ para lanzar una bola de fuego");
            return;
        }

        CmdFireball();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || isDead || isDashing || Time.time - lastDashTime < dashCooldown || !isGrounded) return;

        CmdDash();
    }

    #endregion

    #region Command Methods (Network)


    [Command]
    private void CmdJump()
    {
        if (isGrounded && !animator.GetBool(IsJumping) && !isDead && !isDashing)
        {
            RpcPerformJump();
            TargetAnimationsSound(connectionToClient, "Jump");
        }
    }

    [ClientRpc]
    private void RpcPerformJump()
    {
        _hasToJump = true;
        animator.SetBool(IsJumping, true);
        Debug.Log("Saltar: IsJumping activado");
    }

    [TargetRpc]
    private void TargetAnimationsSound(NetworkConnection target, string soundName)
    {
        switch (soundName)
        {
            case "Jump":
                PlaySound(jumpSound);
                break;
            case "Attack":
                PlaySound(attackSound);
                break;
            case "Fireball":
                PlaySound(fireballSound);
                break;
            case "Hit":
                PlaySound(hitSound);
                break;
            case "Death":
                PlaySound(deathSound);
                break;
            case "Run":
                PlaySound(runSound);
                break;
            case "Dash":
                PlaySound(dashSound);
                break;
            default:
                Debug.LogWarning("Sonido no reconocido: " + soundName);
                break;
        }
    }


    [Command]
    private void CmdAttack()
    {
        lastAttackTime = Time.time;
        RpcPerformAttack();
        TargetAnimationsSound(connectionToClient, "Attack");
    }

    [ClientRpc]
    private void RpcPerformAttack()
    {
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

        Invoke(nameof(DisableSwordHitboxes), 0.2f);
        StartCoroutine(DealDamageAfterDelay(0.3f));
    }


    [Command]
    private void CmdDash()
    {
        RpcPerformDash();
    }

    [ClientRpc]
    private void RpcPerformDash()
    {
        StartCoroutine(PerformDash());
    }

    // 4. VersiÃ³n del comando de recibir daÃ±o arreglada
    [Command(requiresAuthority = true)]
    private void CmdTakeDamage(int damage)
    {
        Debug.Log($"Servidor recibió CmdTakeDamage: {damage}");
        // Llamar al método de daño regular, que solo ejecuta lógica del lado del servidor
        TakeDamage(damage);
        TargetSyncValues(connectionToClient, currentHealth, maxHealth, currentMana, maxMana);
    }

     [ClientRpc]
    private void RpcHit()
    {
        animator.SetTrigger(Hit);

        if (isLocalPlayer)
        {   
            // Opcional - notificar a la barra de vida para efectos visuales
            if (barraVida != null)
            {
                barraVida.MostrarDanoRecibido();
            }

            if (isGrounded && Mathf.Abs(moveDirection) > 0)
            {
                Invoke(nameof(ResumeRunningSound), 0.2f);
            }
        }
    }

    [ClientRpc]
    private void RpcDie()
    {
        if (!isDead) return;

        isDead = true;
        animator.SetTrigger("Die");
        Debug.Log(animator.GetCurrentAnimatorStateInfo(0).IsName("Die"));
        rb.linearVelocity = Vector2.zero;

        if (isLocalPlayer)
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            attackAction?.Disable();
            fireballAction?.Disable();
            dashAction?.Disable();

            PlaySound(deathSound);

            if (deathUI != null)
            {
                deathUI.style.display = DisplayStyle.Flex;
            }
        }

        Debug.Log("El personaje ha muerto.");

        animator.SetBool(IsMoving, false);
        animator.SetBool(IsJumping, false);
        animator.SetBool(IsFalling, false);
        animator.SetFloat(AirSpeed, 0);

        // if (isLocalPlayer)
        // {
        //     Invoke(nameof(Respawn), 3f);
        // }
    }

    [Command]
    private void CmdRespawn()
    {
        Debug.Log("Servidor mansejando solicitud de reapariciÃ³n");
        currentHealth = maxHealth;
        currentMana = maxMana;
        isDead = false;  // Esto activarÃ¡ OnIsDeadChanged
        RpcRespawnPlayer();
    }

    [ClientRpc]
    private void RpcRespawnPlayer()
    {
        Debug.Log("Cliente manejando reapariciÃ³n");

        if (isLocalPlayer)
        {
            // Restaurar posiciÃ³n
            float x = PlayerPrefs.GetFloat("position_x", transform.position.x);
            float y = PlayerPrefs.GetFloat("position_y", transform.position.y);
            transform.position = new Vector2(x, y);

            // Reactivar controles
            moveAction?.Enable();
            jumpAction?.Enable();
            attackAction?.Enable();
            fireballAction?.Enable();
            dashAction?.Enable();

            // Ocultar UI de muerte
            if (deathUI != null)
            {
                deathUI.style.display = DisplayStyle.None;
            }
        }

        // Reiniciar todos los estados de animaciÃ³n
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

        Debug.Log("Jugador reaparecido exitosamente");
    }
    [Command]
    private void CmdAddCoins(int amount)
    {
        totalCoins += amount;
        Debug.Log($"Monedas obtenidas: {amount} | Total: {totalCoins}");
    }

    #endregion

    #region SyncVar Hooks

    // 1. Callback de cambio de salud mejorado
// Callback para cambios de salud
 void OnHealthChanged(int oldValue, int newValue)
{
    Debug.Log($"[SALUD] OnHealthChanged: {oldValue} -> {newValue} | Local: {isLocalPlayer} | Servidor: {isServer}");
    
    // Update local player's UI directly
    if (isLocalPlayer && barraVida != null)
    {
        Debug.Log($"[isLocalPlayer] Actualizando barra de vida: {newValue}/{maxHealth}");
        barraVida.ActualizarBarraVida(newValue, maxHealth);
        Debug.Log($"Barra de vida actualizada: {newValue}/{maxHealth} en onHealthChanged");
        Debug.Log("Current Health: " + currentHealth);
        Debug.Log("SFBFWSBIFBSUBFSJUBFOUSB");

    } else if (isServer && !isLocalPlayer)
    {	
        RpcUpdateHealthOnClient(newValue);
        Debug.Log("Current Health: " + currentHealth);
    }

}



[ClientRpc]
private void RpcUpdateHealthOnClient(int newHealth)
{
        
    // Update other clients' UI
    UpdateHealthBar(newHealth);
    Debug.Log($"Barra de vida actualizada: {newHealth}/{maxHealth} en RpcUpdateHealthOnClient");
}

private void UpdateHealthBar(int health)
{
    if (barraVida != null && isLocalPlayer)
    {
        Debug.Log("Actualizandoooooooo");
        barraVida.ActualizarBarraVida(health, maxHealth);
    }
}
    private void OnCoinsChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer && coinText != null)
        {
            coinText.text = "" + newValue;
        }
    }


    #endregion

    #region Helper Methods

    private IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        // Activar la animaciÃ³n de dash
        animator.SetBool(IsDashing, true);

        // Guardar velocidad original para restaurarla despuÃ©s
        Vector2 originalVelocity = rb.linearVelocity;

        // DirecciÃ³n del dash basada en hacia dÃ³nde mira el personaje
        float dashDirection = spriteRenderer.flipX ? -1f : 1f;

        // Si no hay direcciÃ³n de movimiento (jugador quieto), usar direcciÃ³n de sprite
        if (moveDirection == 0)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashForce, 0);
        }
        else
        {
            // Si hay movimiento, usar esa direcciÃ³n para el dash
            rb.linearVelocity = new Vector2(moveDirection * dashForce, 0);
        }

        // Reproducir sonido de dash
        if (isLocalPlayer)
        {
            PlaySound(dashSound);
        }

        // Activar efecto visual si existe
        if (dashEffect != null)
        {
            dashEffect.SetActive(true);
        }

        // Esperar la duraciÃ³n del dash
        yield return new WaitForSeconds(dashDuration);

        // Desactivar animaciÃ³n de dash
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


    private void OnManaChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer && barraMana != null)
        {
            barraMana.ActualizarBarraMana(newValue, maxMana);
        }
    }

    [Command]
    private void CmdFireball()
    {
        if (currentMana >= fireballManaCost)
        {
            UseMana(fireballManaCost);
            lastFireballTime = Time.time;
            RpcPerformFireball();
            TargetAnimationsSound(connectionToClient, "Fireball");
        }
    }

    [ClientRpc]
    private void RpcPerformFireball()
    {
        animator.SetBool(IsCasting, true);
        ShootFireball();

        Invoke(nameof(FinishCasting), 0.3f);
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

        // Solo el servidor instancia objetos para mantener sincronizados
        if (isServer)
        {
            GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(fireball);

            FireballMultiplayer fireballScript = fireball.GetComponent<FireballMultiplayer>();
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
        }

        Debug.Log("Bola de fuego disparada");
    }

    private void FinishCasting()
    {
        animator.SetBool(IsCasting, false);
    }

    private void UseMana(int amount)
    {
        if (isServer)
        {
            currentMana = Mathf.Max(0, currentMana - amount);
            Debug.Log($"ManÃ¡ usado: {amount} | ManÃ¡ actual: {currentMana}");
        }
    }

    private void RegenerateMana()
    {
        if (!isServer || !isRegeneratingMana || isDead) return;

        if (currentMana < maxMana)
        {
            float manaToRegenerate = manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Min(maxMana, currentMana + Mathf.RoundToInt(manaToRegenerate));
        }
    }

    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isServer) yield break;

        float attackRange = 1f;
        int tilesToBreak = 3;
        float tileSize = 1f;

        Vector2 attackDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 boxCenter = (Vector2)transform.position + (attackDirection * tileSize / 2);
        Vector2 boxSize = new Vector2(tileSize, tileSize * tilesToBreak);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (var hit in hitEnemies)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(playerDamage);
                    Debug.Log("Â¡Enemigo golpeado!");
                }
                else
                {
                    Debug.LogWarning("El enemigo no tiene el componente EnemyAI");
                }
            }
            else if (hit.CompareTag("Boss"))
            {
                BossTutorialMultiplayer boss = hit.GetComponent<BossTutorialMultiplayer>();
                if (boss != null)
                {
                    boss.TakeDamage(playerDamage);
                    Debug.Log("Â¡Jefe golpeado!");
                }
                else
                {
                    Debug.LogWarning("El jefe no tiene el componente BossTutorial");
                }
            }
        }

        RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCenter, boxSize, 0, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("FalseWall"))
            {
                hit.collider.GetComponent<FalseWall>()?.TakeDamage(hit.point);
                Debug.Log("Â¡Bloque destruido!");
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


    // 3. RecepciÃ³n de daÃ±o arreglada - Â¡esto es clave!
     // Método de tomar daño - lado del servidor
    public void TakeDamage(int damage)
    {
        Debug.Log($"TakeDamage llamado con {damage} de daño | Local: {isLocalPlayer} | Servidor: {isServer} del metodo TAKE DAMAGE");

        // Solo el servidor debe procesar el daño
        if (!isServer) 
        {
            // Si no es servidor, enviar comando al servidor para manejar el daño
            CmdTakeDamage(damage);
            Debug.Log("Cliente solicitando al servidor aplicar daño");
            return;
        }
        // Aplicación de daño del lado del servidor
        if (isDead || isDashing) return;

        Debug.Log($"Servidor aplicando daño: {damage} | Salud actual: {currentHealth}");

        // Aplicar daño
        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"Salud después del daño: {currentHealth}");

        // Verificar muerte
        if (currentHealth <= 0)
        {
            isDead = true;  // Esto activa el hook OnIsDeadChanged en todos los clientes
            return;
        }

        // Para daño no fatal, activar animación de golpe
        RpcHit();
        TargetAnimationsSound(connectionToClient, "Hit");
    }

    private void ResumeRunningSound()
    {
        if (isLocalPlayer && isGrounded && Mathf.Abs(moveDirection) > 0 && audioSource.clip != runSound)
        {
            PlaySound(runSound);
        }
    }

    private void Respawn()
    {
        if (isLocalPlayer)
        {
            Debug.Log("Esperando 3 segundos para respawn...");
            // yield return new WaitForSeconds(3f);
            // Debug.Log($"Jugador local reapareciendo");

            CmdRespawn();
        }
    }

    public void AddCoins(int amount)
    {
        if (isLocalPlayer)
        {
            CmdAddCoins(amount);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && isLocalPlayer)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
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

    #endregion

    #region Unity Update Methods

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isDashing) return;
        if (!isLocalPlayer) return;

        if (!isStunned)
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        Debug.Log("MoveDirection: " + moveDirection);


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

        // Para el servidor, regenerar manÃ¡
        if (isServer)
        {
            RegenerateMana();
        }

        // Para todos los clientes, actualizar animaciones
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

        // Solo el jugador local reproduce sonidos
        if (isLocalPlayer && isGrounded && !wasGrounded && Mathf.Abs(moveDirection) > 0 && !isDashing)
        {
            PlaySound(runSound);
        }
    }
  
private void OnTriggerEnter2D(Collider2D collision)
{
    if (!isLocalPlayer) return;

    if (collision.CompareTag("Spikes"))
    {
        Debug.Log("Jugador golpeó pinchos");
        TakeDamage(100);
    }

    // Verificar colisión con enemigo durante ataque
    if (collision.CompareTag("Enemy") && animator.GetBool(IsAttacking))
    {
        // Siempre queremos que el servidor maneje el daño
        CmdDamageEnemy(collision.gameObject.GetComponent<NetworkIdentity>().netId, playerDamage);
        Debug.Log("Enviando solicitud de daño al enemigo al servidor");
    }
    
    // Verificar colisión con jefe durante ataque
    if (collision.CompareTag("Boss") && animator.GetBool(IsAttacking))
    {
        // Siempre queremos que el servidor maneje el daño
        CmdDamageBoss(collision.gameObject.GetComponent<NetworkIdentity>().netId, playerDamage);
        Debug.Log("Enviando solicitud de daño al jefe al servidor");
    }
}

[Command]
private void CmdDamageBoss(uint bossNetId, int damage)
{
    Debug.Log($"Servidor intentando dañar jefe {bossNetId} con {damage} de daño");

    // Encontrar al jefe por ID de red
    if (NetworkServer.spawned.TryGetValue(bossNetId, out NetworkIdentity identity))
    {
        var boss = identity.GetComponent<BossTutorialMultiplayer>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            Debug.Log($"Jefe {bossNetId} dañado por {damage}");
        }
        else
        {
            Debug.LogWarning($"Componente de jefe no encontrado en NetworkIdentity {bossNetId}");
        }
    }
    else
    {
        Debug.LogWarning($"NetworkIdentity {bossNetId} no encontrado entre objetos spawneados");
    }
}

    [Command]
    private void CmdDamageEnemy(uint enemyNetId, int damage)
    {
        Debug.Log($"Servidor intentando daÃ±ar enemigo {enemyNetId} con {damage} de daÃ±o");

        // Encontrar al enemigo por ID de red
        if (NetworkServer.spawned.TryGetValue(enemyNetId, out NetworkIdentity identity))
        {
            var enemy = identity.GetComponent<EnemyAIMultiplayer>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Enemigo {enemyNetId} daÃ±ado por {damage}");
            }
            else
            {
                Debug.LogWarning($"Componente de enemigo no encontrado en NetworkIdentity {enemyNetId}");
            }
        }
        else
        {
            Debug.LogWarning($"NetworkIdentity {enemyNetId} no encontrado entre objetos spawneados");
        }
    }

    [Command]
    private void CmdAttackEnemy(GameObject enemyObject)
    {
        // Este mÃ©todo se ejecuta en el servidor
        EnemyAI enemy = enemyObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(playerDamage);
            Debug.Log("Â¡Enemigo daÃ±ado en el servidor! DaÃ±o: " + playerDamage);
        }
    }

    [Command]
    private void CmdAttackBoss(GameObject bossObject)
    {
        // Este mÃ©todo se ejecuta en el servidor
        BossTutorialMultiplayer boss = bossObject.GetComponent<BossTutorialMultiplayer>();
        if (boss != null)
        {
            boss.TakeDamage(playerDamage);
            Debug.Log("Â¡Jefe daÃ±ado en el servidor! DaÃ±o: " + playerDamage);
        }
    }
    #endregion
}