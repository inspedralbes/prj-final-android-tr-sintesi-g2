using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Configuración de Daño")]
    [SerializeField] private int damage = 25;

    [Header("Configuración de Escala")]
    [SerializeField] private float scaleFactor = 1.5f; // Puedes ajustar este valor en el inspector

    [Header("Configuración de Movimiento")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    [Header("Efectos")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private GameObject impactEffectPrefab;

    private int direction = 1; // 1 para derecha, -1 para izquierda
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Obtener o agregar componentes necesarios
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Configuración del Rigidbody2D
        rb.gravityScale = 0; // Sin gravedad para movimiento lineal
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Mejor detección de colisiones
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Movimiento más suave
        rb.bodyType = RigidbodyType2D.Kinematic; // Kinematic para controlar manualmente

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Verificar si hay un BoxCollider2D y agregarlo si no existe
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f); // Tamaño del collider
            collider.isTrigger = true; // Usamos trigger para detectar colisiones sin física de colisión
        }

        // Destruir después del tiempo de vida
        Destroy(gameObject, lifetime);
    }

    // Inicializar la bola de fuego con dirección del player
    public void Initialize(int dir, float spd = -1, int dmg = -1)
    {
        direction = dir;

        // Usar valores proporcionados o los predeterminados
        if (spd > 0) speed = spd;
        if (dmg > 0) damage = dmg;

        // Aplicar velocidad en la dirección correcta
        rb.linearVelocity = new Vector2(direction * speed, 0);

        // Ajustar el tamaño y hacer flip si dispara a la izquierda
        transform.localScale = new Vector3(direction * scaleFactor, scaleFactor, 1);
    }

    // Este método se puede llamar desde el PlayerController
    public static Fireball ShootFireball(GameObject fireballPrefab, Transform spawnPoint, bool isFacingRight)
    {
        if (fireballPrefab == null || spawnPoint == null) return null;

        // Crear instancia de la bola de fuego en la posición del punto de spawn
        GameObject fireballObj = Instantiate(fireballPrefab, spawnPoint.position, Quaternion.identity);
        Fireball fireball = fireballObj.GetComponent<Fireball>();

        // Inicializar con la dirección correcta basada en hacia dónde mira el jugador
        int direction = isFacingRight ? 1 : -1;
        fireball.Initialize(direction);

        return fireball;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verificar si golpeó un objeto con el tag "Enemy"
        if (collision.CompareTag("Enemy"))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            else
            {
                EnemyAI enemy = collision.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }

            DestroyFireball(collision.transform.position);
        }
        // Verificar si golpeó un objeto con el tag "Boss"
        else if (collision.CompareTag("Boss"))
        {
            BossTutorial boss = collision.GetComponent<BossTutorial>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }
            DestroyFireball(collision.transform.position);
        }
        // Verificar si golpeó un objeto con el tag "Ground"
        else if (collision.CompareTag("Ground"))
        {
            DestroyFireball(collision.ClosestPoint(transform.position));
        }
        // Si golpea cualquier otra cosa que no sea el jugador o un trigger
        else if (!collision.CompareTag("Player") && !collision.isTrigger)
        {
            DestroyFireball(collision.ClosestPoint(transform.position));
        }
    }

    private void DestroyFireball(Vector3 impactPosition)
    {
        // Detener el movimiento antes de destruir
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Reproducir efecto de impacto si existe
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, impactPosition, Quaternion.identity);
        }

        // Reproducir sonido de impacto si existe
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, impactPosition);
        }

        // Destruir la bola de fuego
        Destroy(gameObject);
    }
}

// Interfaz opcional para manejo de daño
public interface IDamageable
{
    void TakeDamage(int damage);
}