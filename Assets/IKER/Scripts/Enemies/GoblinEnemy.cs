using UnityEngine;

public class GoblinEnemy : EnemyAI
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private int specialAttackDamage = 20;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float specialAttackChance = 0.3f; // 30% de probabilidad de ataque especial
    [SerializeField] private int coinReward = 5;

    private float lastAttackTime;

    protected override void Attack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            if (Random.value < specialAttackChance)
            {
                SpecialAttack();
            }
            else
            {
                NormalAttack();
            }
        }
    }

    private void NormalAttack()
    {
        animator.SetTrigger("Attack");
        Invoke(nameof(DelayedDamage), 0.2f);
    }

    private void SpecialAttack()
    {
        animator.SetTrigger("SpecialAttack");
        Invoke(nameof(DelayedSpecialDamage), 0.5f);
    }

    private void DelayedDamage()
    {
        DealDamage(attackDamage);
    }

    private void DelayedSpecialDamage()
    {
        DealDamage(specialAttackDamage);
    }

    private void DealDamage(int damage)
    {
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            if (player.TryGetComponent<PlayerController>(out PlayerController playerScript))
            {
                playerScript.TakeDamage(damage);
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
}