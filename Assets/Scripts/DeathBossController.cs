using UnityEngine;

public class DeathBossController : MonoBehaviour
{
    [Header("Prefab del Ítem")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Configuración del Drop")]
    [SerializeField] private float dropForce = 5f; // Fuerza con la que el ítem será lanzado
    [SerializeField] private Vector2 dropDirection = new Vector2(1f, 1f); // Dirección inicial del drop

    private BossTutorial bossTutorial;

    private void Awake()
    {
        // Obtiene el componente BossTutorial para escuchar la muerte del jefe
        bossTutorial = GetComponent<BossTutorial>();

        if (bossTutorial == null)
        {
            Debug.LogError("No se encontró el componente BossTutorial en este objeto.");
        }
    }

    private void OnEnable()
    {
        // Suscribirse al evento de muerte del jefe
        if (bossTutorial != null)
        {
            bossTutorial.OnBossDeath += HandleBossDeath;
        }
    }

    private void OnDisable()
    {
        // Desuscribirse del evento de muerte del jefe
        if (bossTutorial != null)
        {
            bossTutorial.OnBossDeath -= HandleBossDeath;
        }
    }

    private void HandleBossDeath()
    {
        // Instanciar el ítem en la posición del jefe
        if (itemPrefab != null)
        {
            GameObject droppedItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);

            // Agregar una fuerza para simular el drop
            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Aplicar fuerza en la dirección especificada
                rb.AddForce(dropDirection.normalized * dropForce, ForceMode2D.Impulse);
            }
        }
        else
        {
            Debug.LogWarning("No se asignó un prefab de ítem.");
        }
    }
}