using UnityEngine;
using Mirror;

public class GoblinEnemyMultiplayer : EnemyAIMultiplayer
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private int specialAttackDamage = 20;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float specialAttackChance = 0.3f;
    [SerializeField] private int coinReward = 5;

    private float lastAttackTime;

    protected override void Attack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            if (Random.value < specialAttackChance)
            {
                RpcTriggerAnimation(animator != null ? Animator.StringToHash("SpecialAttack") : 0);
                Invoke(nameof(DelayedSpecialDamage), 0.5f);
            }
            else
            {
                RpcTriggerAnimation(animator != null ? Animator.StringToHash("Attack") : 0);
                Invoke(nameof(DelayedDamage), 0.2f);
            }
        }
    }

    [Server]
    private void DelayedDamage()
    {
        DealDamage(attackDamage);
    }

    [Server]
    private void DelayedSpecialDamage()
    {
        DealDamage(specialAttackDamage);
    }

    [Server]
    private void DealDamage(int damage)
    {
        if (targetPlayer == null) return;
        
        if (Vector2.Distance(transform.position, targetPlayer.position) <= attackRange)
        {
            PlayerControllerMulti playerScript = targetPlayer.GetComponent<PlayerControllerMulti>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(damage);
            }
        }
    }

    [Server]
    protected override void HandleDeath()
    {
        if (IsDead()) return;

        base.HandleDeath();

        // Recompensar al jugador más cercano si está dentro del rango
        FindNearestPlayer(); // Actualizar el jugador objetivo
        if (targetPlayer != null && Vector2.Distance(transform.position, targetPlayer.position) <= followRange)
        {
            PlayerControllerMulti playerScript = targetPlayer.GetComponent<PlayerControllerMulti>();
            if (playerScript != null)
            {
                playerScript.AddCoins(coinReward);
            }
        }
    }
}