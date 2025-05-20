using UnityEngine;

public class SmokeEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f; // Duración del humo en segundos

    private void Start()
    {
        // Destruir el objeto después del tiempo definido
        Destroy(gameObject, lifetime);
    }
}