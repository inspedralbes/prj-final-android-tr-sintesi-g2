using UnityEngine;

public class AnimationTester : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) // Activa el Hit
        {
            animator.SetTrigger("Hit");
            Debug.Log("Trigger de 'Hit' activado.");
        }

        if (Input.GetKeyDown(KeyCode.F)) // Activa la muerte
        {
            animator.SetBool("IsDead", true);
            Debug.Log("Bool de 'IsDead' activado.");
        }
    }
}
