// Modified FalseWall.cs
using UnityEngine;
public class FalseWall : MonoBehaviour
{
    [SerializeField] private int blockHealth = 1;

    // Method that accepts an integer damage value
    public void TakeDamage(int damage)
    {
        blockHealth -= damage;
        if (blockHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    // Overloaded method that accepts a Vector2 hit point
    public void TakeDamage(Vector2 hitPoint)
    {
        // You can use the hit point for visual effects if needed
        // Then call the integer version with your default damage value
        TakeDamage(1); // Default damage value of 1
    }
}