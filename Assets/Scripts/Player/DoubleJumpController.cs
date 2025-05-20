using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DoubleJumpController : MonoBehaviour
{
    [Header("Configuración de Doble Salto")]
    [SerializeField] private float doubleJumpForce = 15f;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private GameObject doubleJumpEffect;
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private InputActionReference jumpAction;
    
    // Referencias
    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;
    private Sensor_HeroKnight groundSensor;
    private AudioSource audioSource;
    
    // Estado del doble salto
    private bool canDoubleJump = false;
    private bool hasDoubleJumped = false;
    
    private void Awake()
    {
        // Buscar el componente de audio o añadirlo si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        // Encontrar el player en la escena
        playerController = FindObjectOfType<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("DoubleJumpController: No se encontró un PlayerController en la escena.");
            enabled = false;
            return;
        }
        
        // Obtener las referencias necesarias del player
        playerRb = playerController.GetComponent<Rigidbody2D>();
        playerAnimator = playerController.GetComponent<Animator>();
        groundSensor = playerController.GetComponentInChildren<Sensor_HeroKnight>();
        
        if (playerRb == null || playerAnimator == null || groundSensor == null)
        {
            Debug.LogError("DoubleJumpController: Faltan componentes necesarios en el Player.");
            enabled = false;
            return;
        }
        
        // Verificar si la acción de salto fue asignada
        if (jumpAction == null)
        {
            Debug.LogError("DoubleJumpController: No se asignó la acción de salto en el inspector.");
            enabled = false;
            return;
        }
        
        // Registrar el evento del botón de salto
        jumpAction.action.performed += OnJumpAction;
        
        Debug.Log("DoubleJumpController inicializado correctamente.");
    }
    
    private void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.action.performed -= OnJumpAction;
            jumpAction.action.Disable();
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (jumpAction != null)
        {
            jumpAction.action.performed -= OnJumpAction;
        }
    }
    
    private void Update()
    {
        // Verificar si el jugador está en el suelo para resetear el estado del doble salto
        bool isGrounded = groundSensor != null && groundSensor.IsGrounded();
        
        if (isGrounded)
        {
            hasDoubleJumped = false;
            canDoubleJump = false;
        }
        else if (!isGrounded && !hasDoubleJumped && !canDoubleJump)
        {
            // Activar la capacidad de doble salto cuando el jugador está en el aire
            // y no ha usado el doble salto todavía
            canDoubleJump = true;
        }
    }
    
    private void OnJumpAction(InputAction.CallbackContext context)
    {
        // Si el jugador está en el aire y puede hacer doble salto
        if (!groundSensor.IsGrounded() && canDoubleJump && !hasDoubleJumped)
        {
            PerformDoubleJump();
        }
    }
    
    private void PerformDoubleJump()
    {
        // Realizar el doble salto
        hasDoubleJumped = true;
        canDoubleJump = false;
        
        // Detener la velocidad vertical actual
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        
        // Aplicar la fuerza del doble salto
        playerRb.AddForce(Vector2.up * doubleJumpForce, ForceMode2D.Impulse);
        
        // Reproducir el sonido si está asignado
        if (doubleJumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(doubleJumpSound);
        }
        
        // Mostrar el efecto visual si está asignado
        if (doubleJumpEffect != null)
        {
            StartCoroutine(ShowDoubleJumpEffect());
        }
        
        // Resetear las animaciones para un salto limpio
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsJumping", true);
            playerAnimator.SetBool("IsFalling", false);
        }
        
        Debug.Log("Doble salto realizado con fuerza: " + doubleJumpForce);
    }
    
    private IEnumerator ShowDoubleJumpEffect()
    {
        // Instanciar el efecto de doble salto en la posición del jugador
        GameObject effect = Instantiate(doubleJumpEffect, playerRb.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(effectDuration);
        
        // Destruir el efecto después de la duración
        if (effect != null)
        {
            Destroy(effect);
        }
    }
    
    // Método público para verificar si el jugador ha usado el doble salto
    public bool HasUsedDoubleJump()
    {
        return hasDoubleJumped;
    }
    
    // Método público para recargar la capacidad de doble salto (útil para power-ups)
    public void ResetDoubleJump()
    {
        hasDoubleJumped = false;
        canDoubleJump = true;
    }
}