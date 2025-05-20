using UnityEngine;
using System.Collections;

public class RejaController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BossTutorialMultiplayer bossReference; // Referencia al jefe
    
    [Header("Configuración")]
    [SerializeField] private float fadeTime = 2f; // Tiempo de desvanecimiento
    [SerializeField] private GameObject rejaCollider; // Collider de la reja (puede ser un hijo con BoxCollider2D)
    [SerializeField] private AudioClip disolveSound; // Sonido al desvanecerse
    
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool hasDisappeared = false;

    private void Awake()
    {
        // Obtener componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Si no tiene un AudioSource, añadirlo
        if (audioSource == null && disolveSound != null)
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
            bossReference = FindObjectOfType<BossTutorialMultiplayer>();
            if (bossReference == null)
            {
                Debug.LogError("No se encontró ninguna referencia al jefe. Asegúrate de asignar el BossTutorialMultiplayer en el inspector o que exista en la escena.");
                return;
            }
        }
        
        // Suscribirse al evento de muerte del jefe
        bossReference.OnBossDeath += OnBossDeath;
        
        // Asegurarse de que el collider esté activado al inicio
        if (rejaCollider != null)
        {
            rejaCollider.SetActive(true);
        }
        else
        {
            // Si no se asignó un collider específico, usar el del propio GameObject
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }

    private void OnBossDeath()
    {
        if (!hasDisappeared)
        {
            StartCoroutine(DisappearEffect());
            hasDisappeared = true;
        }
    }

    private IEnumerator DisappearEffect()
    {
        // Reproducir sonido si existe
        if (audioSource != null && disolveSound != null)
        {
            audioSource.clip = disolveSound;
            audioSource.Play();
        }
        
        // Desactivar el collider para permitir al jugador pasar
        if (rejaCollider != null)
        {
            rejaCollider.SetActive(false);
        }
        else
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        
        // Efecto de desvanecimiento
        float elapsedTime = 0f;
        Color originalColor = spriteRenderer.color;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - (elapsedTime / fadeTime);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // Opcionalmente, desactivar el GameObject cuando se haya desvanecido completamente
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Asegurarse de desuscribirse del evento para evitar errores
        if (bossReference != null)
        {
            bossReference.OnBossDeath -= OnBossDeath;
        }
    }

    // Para pruebas: Función para forzar la desaparición de la reja
    public void ForceDisappear()
    {
        if (!hasDisappeared)
        {
            StartCoroutine(DisappearEffect());
            hasDisappeared = true;
        }
    }

    // Para pruebas: Función para restaurar la reja
    public void RestoreGate()
    {
        if (hasDisappeared)
        {
            StopAllCoroutines();
            
            // Restaurar visibilidad
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
            }
            
            // Restaurar colisión
            if (rejaCollider != null)
            {
                rejaCollider.SetActive(true);
            }
            else
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            
            hasDisappeared = false;
            gameObject.SetActive(true);
        }
    }
}