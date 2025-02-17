using UnityEngine;

public class Skeleton : EnemyAI
{
    [SerializeField] protected int attackDamage = 10;
    [SerializeField] private int secondAttackDamage = 15;
    [SerializeField] private float blockChance = 0.4f;
    [SerializeField] private int reducedDamage = 5;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int coinReward = 5;
    private bool usedFirstAttack = false;
    private float lastAttackTime;
    private bool isDead = false;

    protected override void Attack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            if (usedFirstAttack)
            {
                animator.SetTrigger("SecondAttack");
                Invoke(nameof(DelayedSecondAttackDamage), 0.5f);
                usedFirstAttack = false;
            }
            else
            {
                animator.SetTrigger("Attack");
                Invoke(nameof(DelayedAttackDamage), 0.4f);
                usedFirstAttack = true;
            }
        }
    }

    private void DelayedAttackDamage()
    {
        DealDamage(attackDamage);
    }

    private void DelayedSecondAttackDamage()
    {
        DealDamage(secondAttackDamage);
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

    private bool TryBlock()
    {
        if (Random.value <= blockChance)
        {
            animator.SetTrigger("Block");
            return true;
        }
        return false;
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;

        bool blocked = TryBlock();
        int finalDamage = blocked ? Mathf.Max(damage - reducedDamage, 0) : damage;
        base.TakeDamage(finalDamage);

        if (GetHealth() <= 0 && !isDead)
        {
            isDead = true;
            animator.SetTrigger("Die");
            Debug.Log("💀 El esqueleto ha muerto!");

            if (player.TryGetComponent<PlayerController>(out PlayerController playerScript))
            {
                playerScript.AddCoins(coinReward);
            }
        }
        else if (!blocked)
        {
            animator.SetTrigger("Hit");
        }
    }
}

