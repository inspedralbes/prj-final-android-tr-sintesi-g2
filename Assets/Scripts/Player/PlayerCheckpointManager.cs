using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCheckpointManager : MonoBehaviour
{
    private string checkpointKeyX;
    private string checkpointKeyY;

    void Awake()
    {
        // Genera una clave única por escena
        string sceneName = SceneManager.GetActiveScene().name;
        checkpointKeyX = sceneName + "_checkpoint_x";
        checkpointKeyY = sceneName + "_checkpoint_y";

        // Si hay un checkpoint guardado, mueve al jugador ahí
        if (PlayerPrefs.HasKey(checkpointKeyX) && PlayerPrefs.HasKey(checkpointKeyY))
        {
            float x = PlayerPrefs.GetFloat(checkpointKeyX);
            float y = PlayerPrefs.GetFloat(checkpointKeyY);
            transform.position = new Vector2(x, y);
        }
        else
        {
            // Si no hay checkpoint, guarda la posición actual como checkpoint inicial
            SaveCheckpoint();
        }
    }

    void Start()
    {
        // Cada vez que entras a la escena, actualiza el checkpoint
        SaveCheckpoint();
    }

    public void SaveCheckpoint()
    {
        PlayerPrefs.SetFloat(checkpointKeyX, transform.position.x);
        PlayerPrefs.SetFloat(checkpointKeyY, transform.position.y);
        PlayerPrefs.Save();
    }

    // Llama a este método cuando el jugador muera para reaparecer en el checkpoint
    public void RespawnAtCheckpoint()
    {
        float x = PlayerPrefs.GetFloat(checkpointKeyX, transform.position.x);
        float y = PlayerPrefs.GetFloat(checkpointKeyY, transform.position.y);
        transform.position = new Vector2(x, y);
    }
}