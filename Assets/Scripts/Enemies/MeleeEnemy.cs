using UnityEngine;

public class MeleeEnemy : EnemyAI
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int coinReward = 5; // Monedas específicas para este enemigo

    private float lastAttackTime;

    protected override void Attack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            animator.SetTrigger("Attack");
            lastAttackTime = Time.time;

            if (Vector2.Distance(transform.position, player.position) <= attackRange)
            {
                if (player.TryGetComponent<PlayerController>(out PlayerController playerScript))
                {
                    playerScript.TakeDamage(attackDamage);
                }
            }
        }
    }

    protected override void HandleDeath()
    {
        if (IsDead()) return;

        base.HandleDeath(); // Llama al método padre para la animación de muerte y desactivación

        // Dar las monedas al jugador
        if (player.TryGetComponent<PlayerController>(out PlayerController playerScript))
        {
            playerScript.AddCoins(coinReward);
        }
    }
}
