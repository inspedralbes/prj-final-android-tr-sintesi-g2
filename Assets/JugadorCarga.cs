using UnityEngine;

public class JugadorCarga : MonoBehaviour
{
    public float offsetY = 0.5f; // Offset vertical para ajustar la posición
    
    void Start()
    {
        // Verificar si hay datos guardados de posición
        if (PlayerPrefs.HasKey("position_x") && PlayerPrefs.HasKey("position_y"))
        {
            float posX = PlayerPrefs.GetFloat("position_x");
            float posY = PlayerPrefs.GetFloat("position_y");
            
            // Verificar que la escena cargada coincide con la escena guardada
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string savedScene = PlayerPrefs.GetString("lastScene", currentScene);
            
            if (currentScene == savedScene)
            {
                Debug.Log($"Colocando al jugador en la posición guardada: ({posX}, {posY}) en escena {currentScene}");
                transform.position = new Vector3(posX, posY + offsetY, transform.position.z);
            }
            else
            {
                Debug.Log($"La escena actual ({currentScene}) no coincide con la escena guardada ({savedScene})");
                // Si estás en una escena diferente, puedes decidir qué hacer aquí
            }
        }
        else
        {
            Debug.Log("No se encontraron datos de posición guardados. El jugador permanece en su posición inicial.");
        }
        
        // Verificar si hay datos guardados de salud y monedas
        if (PlayerPrefs.HasKey("health") && PlayerPrefs.HasKey("coins"))
        {
            int health = PlayerPrefs.GetInt("health");
            int coins = PlayerPrefs.GetInt("coins");
            
            // Obtener el componente PlayerController
            PlayerController playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                Debug.Log($"Restaurando salud: {health} y monedas: {coins}");
                playerController.SetCurrentHealth(health);
            }
            else
            {
                Debug.LogWarning("No se encontró el componente PlayerController en el jugador.");
            }
        }
    }
}