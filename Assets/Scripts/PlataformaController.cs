using UnityEngine;
using System.Collections;

public class PlataformaController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BossTutorial bossReference; // Referencia al jefe
    
    [Header("Configuración de Aparición")]
    [SerializeField] private float appearTime = 1.5f; // Tiempo de aparición gradual
    [SerializeField] private AudioClip appearSound; // Sonido al aparecer
    [SerializeField] private GameObject appearEffect; // Efecto visual al aparecer (opcional)
    [SerializeField] private Color finalColor = Color.white; // Color final de la plataforma
    [SerializeField] private float bounceHeight = 0.2f; // Altura del efecto rebote al aparecer
    [SerializeField] private float bounceSpeed = 2f; // Velocidad del efecto rebote
    
    [Header("Propiedades de Plataforma")]
    [SerializeField] private bool isOneWayPlatform = true; // Si es plataforma de un solo sentido (se puede saltar a través desde abajo)
    [SerializeField] private float playerBounce = 0f; // Cantidad de rebote que da al jugador (0 = normal, >0 = rebote)
    [SerializeField] private AudioClip landingSound; // Sonido cuando el jugador cae sobre la plataforma
    
    // Componentes
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D platformCollider;
    private AudioSource audioSource;
    private Vector3 originalPosition;
    private bool hasAppeared = false;
    private PlatformEffector2D platformEffector;

    private void Awake()
    {
        // Obtener componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.position;
        
        // Si no tiene un BoxCollider2D, añadirlo
        if (platformCollider == null)
        {
            platformCollider = gameObject.AddComponent<BoxCollider2D>();
            platformCollider.size = spriteRenderer.bounds.size;
        }
        
        // Configurar el PlatformEffector2D si es una plataforma de un solo sentido
        if (isOneWayPlatform)
        {
            platformEffector = GetComponent<PlatformEffector2D>();
            if (platformEffector == null)
            {
                platformEffector = gameObject.AddComponent<PlatformEffector2D>();
            }
            platformEffector.surfaceArc = 180f;
            platformEffector.useOneWay = true;
        }
        
        // Si no tiene un AudioSource, añadirlo
        if (audioSource == null && (appearSound != null || landingSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Si no hay referencia al jefe, intentar buscarla
        if (bossReference == null)
        {
            bossReference = FindObjectOfType<BossTutorial>();
            if (bossReference == null)
            {
                Debug.LogError("No se encontró ninguna referencia al jefe. Asegúrate de asignar el BossTutorial en el inspector o que exista en la escena.");
                return;
            }
        }
        
        // Suscribirse al evento de muerte del jefe
        bossReference.OnBossDeath += OnBossDeath;
        
        // Hacer invisible la plataforma al inicio
        if (spriteRenderer != null)
        {
            Color startColor = finalColor;
            startColor.a = 0f;
            spriteRenderer.color = startColor;
        }
        
        // Desactivar el collider al inicio
        if (platformCollider != null)
        {
            platformCollider.enabled = false;
        }
        
        // Si tiene efecto de plataforma, desactivarlo
        if (platformEffector != null)
        {
            platformEffector.enabled = false;
        }
    }

    private void OnBossDeath()
    {
        if (!hasAppeared)
        {
            StartCoroutine(AppearEffect());
            hasAppeared = true;
        }
    }

    private IEnumerator AppearEffect()
    {
        // Reproducir sonido si existe
        if (audioSource != null && appearSound != null)
        {
            audioSource.clip = appearSound;
            audioSource.Play();
        }

        // Mostrar efecto visual si está configurado
        if (appearEffect != null)
        {
            GameObject effect = Instantiate(appearEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f); // Destruir después de 3 segundos
        }
        
        // Efecto de aparición gradual
        float elapsedTime = 0f;
        Color startColor = finalColor;
        startColor.a = 0f;
        
        while (elapsedTime < appearTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / appearTime;
            spriteRenderer.color = Color.Lerp(startColor, finalColor, alpha);
            
            // Efecto de rebote durante la aparición
            float bounce = Mathf.Sin(alpha * bounceSpeed * Mathf.PI) * bounceHeight * (1 - alpha);
            transform.position = new Vector3(originalPosition.x, originalPosition.y + bounce, originalPosition.z);
            
            yield return null;
        }
        
        // Asegurar que el color es exactamente el final
        spriteRenderer.color = finalColor;
        transform.position = originalPosition;
        
        // Activar el collider y el effector
        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }
        
        if (platformEffector != null)
        {
            platformEffector.enabled = true;
        }
        
        Debug.Log("Plataforma completamente visible y activa para interacción física");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Comprobar si el jugador está aterrizando desde arriba
            float playerBottom = collision.collider.bounds.min.y;
            float platformTop = platformCollider.bounds.max.y;
            bool playerLanding = playerBottom >= platformTop - 0.2f; // Tolerancia de 0.2 unidades
            
            if (playerLanding)
            {
                // Reproducir sonido de aterrizaje
                if (audioSource != null && landingSound != null)
                {
                    audioSource.clip = landingSound;
                    audioSource.Play();
                }
                
                // Aplicar efecto de rebote si está configurado
                if (playerBounce > 0)
                {
                    Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        // Aplicar fuerza hacia arriba para rebotar
                        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, playerBounce);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Asegurarse de desuscribirse del evento para evitar errores
        if (bossReference != null)
        {
            bossReference.OnBossDeath -= OnBossDeath;
        }
    }

    // Para pruebas: Función para forzar la aparición
    public void ForceAppear()
    {
        if (!hasAppeared)
        {
            StartCoroutine(AppearEffect());
            hasAppeared = true;
        }
    }

    // Para pruebas: Función para ocultar la plataforma nuevamente
    public void Hide()
    {
        if (hasAppeared)
        {
            StopAllCoroutines();
            
            // Restaurar invisibilidad
            if (spriteRenderer != null)
            {
                Color color = finalColor;
                color.a = 0f;
                spriteRenderer.color = color;
            }
            
            // Desactivar collider y effector
            if (platformCollider != null)
            {
                platformCollider.enabled = false;
            }
            
            if (platformEffector != null)
            {
                platformEffector.enabled = false;
            }
            
            hasAppeared = false;
        }
    }
}