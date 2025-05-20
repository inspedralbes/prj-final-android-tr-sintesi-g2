using UnityEngine;

public class HeavyBanditEnemy : EnemyAI
{
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private int coinReward = 8;

    private float lastAttackTime;
    private SpriteRenderer spriteRenderer;
    public override void SetStats(EnemyData data)
    {
        base.SetStats(data); // Llama a la implementación base en EnemyAI

        attackDamage = data.attack_damage;
        attackCooldown = data.attack_cooldown;
        coinReward = data.coin_reward;
        // Puedes dejar specialAttackChance fijo o también cargarlo si decides incluirlo en la base de datos
    }
    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    protected override void Attack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            NormalAttack();
        }
    }

    private void NormalAttack()
    {
        animator.SetTrigger("Attack");
        Invoke(nameof(DelayedDamage), 0.3f);
    }

    private void DelayedDamage()
    {
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            if (player.TryGetComponent<PlayerController>(out PlayerController playerScript))
            {
                playerScript.TakeDamage(attackDamage);
            }
        }
    }

    protected override void HandleDeath()
    {
        if (IsDead()) return;

        base.HandleDeath();

        if (player.TryGetComponent<PlayerController>(out PlayerController playerScript))
        {
            playerScript.AddCoins(coinReward);
        }
    }
protected override void MoveTowardsPlayer()
{
    if (IsDead()) return;

    animator.SetBool("IsMoving", true);
    Vector2 direction = (player.position - transform.position).normalized;
    transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;

    // Solo voltea el sprite, no todo el GameObject
    spriteRenderer.flipX = (direction.x > 0);

}


}