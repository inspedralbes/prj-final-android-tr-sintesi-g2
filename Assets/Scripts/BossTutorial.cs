using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

[Serializable]
public class BossData
{
    public string bossName;
    public int bossMaxHealth;
    public float moveSpeed;
    public float attackRange;
    public float attackCooldown;
    public int attack1Damage;
    public int attack2Damage;
    public float visionRange;
    public float disintegrationTime;
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}


public class BossTutorial : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int bossMaxHealth = 200;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attack1Damage = 30;
    [SerializeField] private int attack2Damage = 15;
    [SerializeField] private float visionRange = 30;
    [SerializeField] private float disintegrationTime = 2f;

    private Transform player;
    private PlayerController playerController;
    private Animator animator;
    private Rigidbody2D rb;
    public int bossHealth;
    private bool isDead = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    public event Action OnBossDeath;

    [SerializeField] private Image healthBarBackground;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text bossNameText;
    [SerializeField] private string bossName = "Valthorr";

    [SerializeField] private GameObject staffHitbox;
    [SerializeField] private AudioSource bossMusic;

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
        SetAttack(false);

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
            healthBarBackground.gameObject.SetActive(false);
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossName;
            bossNameText.color = Color.red;
            bossNameText.gameObject.SetActive(false);
        }

        if (bossMusic != null) bossMusic.Stop();

        StartCoroutine(FetchBossData());
    }

    private IEnumerator FetchBossData()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http://localhost:3008/bosses"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al obtener datos del boss: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                BossData[] bosses = JsonHelper.FromJson<BossData>(json);

                foreach (var boss in bosses)
                {
                    if (boss.bossName == this.bossName)
                    {
                        ApplyBossData(boss);
                        break;
                    }
                }

                bossHealth = bossMaxHealth; // Inicializar salud
            }
        }
    }

    private void ApplyBossData(BossData data)
    {
        bossMaxHealth = data.bossMaxHealth;
        moveSpeed = data.moveSpeed;
        attackRange = data.attackRange;
        attackCooldown = data.attackCooldown;
        attack1Damage = data.attack1Damage;
        attack2Damage = data.attack2Damage;
        visionRange = data.visionRange;
        disintegrationTime = data.disintegrationTime;

        Debug.Log($"📡 Boss '{data.bossName}' actualizado desde el servidor.");
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
        if (isDead || player == null) return;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)bossHealth / bossMaxHealth;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= visionRange)
        {
            healthBarBackground.gameObject.SetActive(true);
            if (bossNameText != null) bossNameText.gameObject.SetActive(true);
            MoveTowardsPlayer();

            if (bossMusic != null && !bossMusic.isPlaying)
            {
                bossMusic.Play();
            }

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
        rb.MovePosition(new Vector2(rb.position.x + direction.x * moveSpeed * Time.fixedDeltaTime, rb.position.y));
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

    private void DelayedAttack1Damage() => DealDamage(attack1Damage);
    private void DelayedAttack2Damage() => DealDamage(attack2Damage);

    private void DealDamage(int damage)
    {
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            playerController.TakeDamage(damage);
            Vector2 direction = player.position.x < transform.position.x ? Vector2.left : Vector2.right;
            playerController.PushPlayer(direction.normalized, 25);
        }
    }

    private void EndAttack() => SetAttack(false);
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
        if (bossMusic != null && bossMusic.isPlaying) bossMusic.Stop();

        OnBossDeath?.Invoke();

        // Registrar la muerte del jefe
        string playerNickname = PlayerPrefs.GetString("nickname", "Desconocido");
        EnemyDeathService.Instance.RegisterBossDeath(bossName, playerNickname);

        // Iniciar la desintegración después de registrar la muerte
        StartCoroutine(Disintegrate());
    }

    public void ResetHealth()
    {
        bossHealth = bossMaxHealth;
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
        }
        Debug.Log("El jefe ha restaurado su salud completamente.");
    }

    private IEnumerator Disintegrate()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float elapsedTime = 0f;

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
            int damage = bossHealth > bossMaxHealth / 2 ? attack1Damage : attack2Damage;
            playerController.TakeDamage(damage);
            Vector2 direction = player.position.x < transform.position.x ? Vector2.left : Vector2.right;
            playerController.PushPlayer(direction.normalized, 25);
            Debug.Log("💥 El jefe golpeó al jugador. Daño: " + damage);
        }
    }
}