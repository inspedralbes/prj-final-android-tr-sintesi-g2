[System.Serializable]
public class EnemyData
{
    public float move_speed;
    public int enemy_max_health;
    public float follow_range;
    public float attack_range;
    public int hit_damage;
    public int attack_damage;

    public int? second_attack_damage; // opcional
    public float? block_chance;       // opcional
    public int? reduced_damage;       // opcional
    public float attack_cooldown;
    public int coin_reward;
}
