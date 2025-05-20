using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class LadderController : MonoBehaviour
{
    [Header("Configuración básica")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float topOffset = 1.2f; // Distancia por encima de la escalera para posicionar al jugador al terminar
    [SerializeField] private AudioClip climbSound;
    [SerializeField] private bool allowHorizontalMovement = false; // Si se permite moverse horizontalmente en la escalera

    // Referencias privadas
    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;
    private AudioSource audioSource;
    private bool playerInTrigger = false;
    private bool isClimbing = false;
    private float originalGravityScale;
    private Coroutine climbSoundCoroutine;
    private float verticalInput = 0f;

    // Constantes para animación
    private static readonly int IsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int ClimbSpeed = Animator.StringToHash("ClimbSpeed");

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && climbSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.7f;
            audioSource.loop = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Jugador en rango de escalera");
            playerInTrigger = true;
            playerController = collision.GetComponent<PlayerController>();
            playerRb = collision.GetComponent<Rigidbody2D>();
            playerAnimator = collision.GetComponent<Animator>();

            // Opcional: Mostrar una indicación visual
            // ShowLadderPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Jugador fuera de rango de escalera");
            playerInTrigger = false;
            
            if (isClimbing)
            {
                StopClimbing();
            }
            
            // Opcional: Ocultar la indicación visual
            // HideLadderPrompt();
        }
    }

    private void Update()
    {
        if (!playerInTrigger || playerController == null) return;

        // Esta versión del código soporta tanto Input System como el Input Manager tradicional
        if (Gamepad.current != null || Keyboard.current != null)
        {
            // Usando Input System
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                verticalInput = 1.0f;
            }
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                verticalInput = -1.0f;
            }
            else
            {
                verticalInput = 0f;
            }

            // Si usas un gamepad
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (Mathf.Abs(stick.y) > 0.1f)
                {
                    verticalInput = stick.y;
                }
            }
        }
        else
        {
            // Fallback al Input Manager tradicional
            verticalInput = Input.GetAxis("Vertical");
        }

        // Iniciar escalada cuando se presiona hacia arriba
        if (verticalInput > 0 && !isClimbing)
        {
            StartClimbing();
        }
        
        // Durante la escalada
        if (isClimbing)
        {
            HandleClimbing(verticalInput);
            
            // Soltar la escalera al presionar salto
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame || 
                (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                StopClimbing();
            }
            
            // Alcanzar el tope de la escalera
            if (playerRb.transform.position.y >= transform.position.y + (GetComponent<Collider2D>().bounds.size.y / 2) - topOffset)
            {
                FinishClimbingAtTop();
            }

            // Soltar la escalera por la parte inferior
            if (playerRb.transform.position.y <= transform.position.y - (GetComponent<Collider2D>().bounds.size.y / 2) + 0.5f)
            {
                StopClimbing();
            }
        }
    }

    private void StartClimbing()
    {
        if (playerController == null || playerRb == null) return;
        
        Debug.Log("Iniciando escalada");
        isClimbing = true;
        
        // Guardar gravedad original y desactivarla durante escalada
        originalGravityScale = playerRb.gravityScale;
        playerRb.gravityScale = 0;
        
        // Centrar al jugador en la escalera
        Vector3 newPosition = playerRb.transform.position;
        newPosition.x = transform.position.x;
        playerRb.transform.position = newPosition;
        
        // Detener velocidad existente
        playerRb.linearVelocity = Vector2.zero;
        
        // Activar animación de escalada si existe
        if (playerAnimator != null)
        {
            // Añadir comprobación para el parámetro IsClimbing
            if (IsParameterPresent(playerAnimator, "IsClimbing"))
            {
                playerAnimator.SetBool(IsClimbing, true);
            }
        }
        
        // Iniciar sonido de escalada
        if (audioSource != null && climbSound != null)
        {
            if (climbSoundCoroutine != null)
                StopCoroutine(climbSoundCoroutine);
                
            climbSoundCoroutine = StartCoroutine(PlayClimbSoundRepeatedly());
        }

        // Desactivar temporalmente ciertos comportamientos del jugador durante el escalado
        if (playerController != null)
        {
            // Aquí puedes añadir código para desactivar temporalmente controles o comportamientos
            // Por ejemplo, desactivar el salto o ataques
        }
    }

    private void HandleClimbing(float verticalInput)
    {
        if (playerRb == null) return;

        float horizontalInput = 0f;
        
        if (allowHorizontalMovement)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    horizontalInput = -1.0f;
                }
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    horizontalInput = 1.0f;
                }
            }
            
            if (Gamepad.current != null)
            {
                horizontalInput = Gamepad.current.leftStick.ReadValue().x;
            }
        }
        
        // Mover al jugador verticalmente (y horizontalmente si está permitido)
        Vector2 climbVelocity = new Vector2(horizontalInput * climbSpeed * 0.5f, verticalInput * climbSpeed);
        playerRb.linearVelocity = climbVelocity;
        
        // Actualizar animación con velocidad de escalada
        if (playerAnimator != null)
        {
            if (IsParameterPresent(playerAnimator, "ClimbSpeed"))
            {
                playerAnimator.SetFloat(ClimbSpeed, Mathf.Abs(verticalInput));
            }
        }
        
        // Pausar/reanudar sonido según si está en movimiento
        if (audioSource != null && climbSound != null)
        {
            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else
            {
                audioSource.Pause();
            }
        }
    }

    private void StopClimbing()
    {
        if (playerRb == null) return;
        
        Debug.Log("Deteniendo escalada");
        isClimbing = false;
        
        // Restaurar gravedad
        playerRb.gravityScale = originalGravityScale;
        
        // Desactivar estado de escalada en animador
        if (playerAnimator != null)
        {
            if (IsParameterPresent(playerAnimator, "IsClimbing"))
            {
                playerAnimator.SetBool(IsClimbing, false);
            }
        }
        
        // Detener sonido
        if (audioSource != null)
        {
            audioSource.Stop();
            
            if (climbSoundCoroutine != null)
            {
                StopCoroutine(climbSoundCoroutine);
                climbSoundCoroutine = null;
            }
        }

        // Reactivar comportamientos del jugador
        if (playerController != null)
        {
            // Aquí puedes añadir código para reactivar controles o comportamientos
            // Que hayas desactivado en StartClimbing
        }
    }

    private void FinishClimbingAtTop()
    {
        if (playerRb == null) return;
        
        Debug.Log("Llegando al tope de la escalera");
        
        // Posicionar al jugador en la parte superior con un pequeño empuje hacia adelante
        Vector3 topPosition = new Vector3(
            transform.position.x,
            transform.position.y + (GetComponent<Collider2D>().bounds.size.y / 2) + (playerRb.GetComponent<Collider2D>().bounds.size.y / 2),
            playerRb.transform.position.z
        );
        
        playerRb.transform.position = topPosition;
        
        // Aplicar un pequeño impulso horizontal para ayudar a salir de la escalera
        playerRb.AddForce(Vector2.right * 3f, ForceMode2D.Impulse);
        
        // Detener escalada
        StopClimbing();
    }

    private IEnumerator PlayClimbSoundRepeatedly()
    {
        while (isClimbing)
        {
            if (Mathf.Abs(playerRb.linearVelocity.y) > 0.1f && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(climbSound);
                yield return new WaitForSeconds(climbSound.length * 0.7f); // Solapar un poco el sonido
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // Método de utilidad para verificar si existe un parámetro en el Animator
    private bool IsParameterPresent(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}