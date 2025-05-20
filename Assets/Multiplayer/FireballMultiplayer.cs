using UnityEngine;
using Mirror;

public class FireballMultiplayer : NetworkBehaviour
{
    // Public properties for initialization
    [SyncVar]
    public int direction = 1;

    [SyncVar]
    public float speed = 15f;

    [SyncVar]
    public int damage = 25;

    // Optional visual effects
    [SerializeField] private ParticleSystem trailEffect;
    [SerializeField] private ParticleSystem impactEffect;
    [SerializeField] private AudioClip impactSound;

    // Internal references
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private bool hasHit = false;

    private void Awake()
    {
        // Get references to components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Add AudioSource if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Make sound 3D
        }

        // Make sure the rigidbody is set up correctly
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // Called when the fireball is spawned on the server
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Destroy after 3 seconds if no collision happens
        Invoke(nameof(DestroyFireball), 3f);
    }

    // Called when the fireball is spawned on clients
    public override void OnStartClient()
    {
        base.OnStartClient();

        // Apply velocity on clients for visual consistency
        ApplyMovement();

        // Flip sprite if moving left
        if (direction < 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = true;
        }

        // Start trail effect if assigned
        if (trailEffect != null)
        {
            trailEffect.Play();
        }
    }

    // Initialize the fireball properties (called by player controller)
    [Server]
    public void Initialize(int dir, float spd, int dmg)
    {
        direction = dir;
        speed = spd;
        damage = dmg;

        ApplyMovement();
    }

    // Apply movement force based on direction and speed
    private void ApplyMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * speed, 0);
        }
    }

    // Handle collisions
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Player"))
        {
            // Get the player's NetworkIdentity
            NetworkIdentity playerIdentity = collision.GetComponent<NetworkIdentity>();

            // Only damage if we hit an opponent, not our owner
            PlayerControllerMulti playerController = collision.GetComponent<PlayerControllerMulti>();
            if (playerController != null)
            {
                // Deal damage to the player
                playerController.TakeDamage(damage);
                Debug.Log($"Fireball hit player for {damage} damage");
            }

            // Call the impact effect on all clients
            RpcOnImpact(collision.transform.position);
            hasHit = true;
            DestroyFireball();
        }
        else if (collision.CompareTag("Enemy"))
        {
            EnemyAIMultiplayer enemy = collision.GetComponent<EnemyAIMultiplayer>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Fireball hit enemy for {damage} damage");
            }

            RpcOnImpact(collision.transform.position);
            hasHit = true;
            DestroyFireball();
        }
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall") || collision.CompareTag("FalseWall"))
        {
            // Hit environment
            RpcOnImpact(collision.ClosestPoint(transform.position));
            hasHit = true;
            DestroyFireball();
        }
    }

    // Play impact effects on all clients
    [ClientRpc]
    private void RpcOnImpact(Vector3 impactPosition)
    {
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Stop trail effect
        if (trailEffect != null)
        {
            trailEffect.Stop();
        }

        // Play impact effect at collision point
        if (impactEffect != null)
        {
            ParticleSystem impact = Instantiate(impactEffect, impactPosition, Quaternion.identity);
            Destroy(impact.gameObject, 1f);
        }

        // Play impact sound
        if (audioSource != null && impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }

        // Make sprite renderer invisible but leave game object alive briefly for sound to play
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    // Destroy the fireball - called from server
    [Server]
    private void DestroyFireball()
    {
        NetworkServer.Destroy(gameObject);
    }
}