using UnityEngine;

public class Sensor_HeroKnight : MonoBehaviour
{
    private bool isGrounded;

    public bool IsGrounded()
    {
        return isGrounded;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = true;
            Debug.Log("Entró al suelo");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = false;
            Debug.Log("Salió del suelo");
        }
    }

}
