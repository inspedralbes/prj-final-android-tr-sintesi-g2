using UnityEngine;
using UnityEngine.InputSystem;
using  UnityEngine.SceneManagement;
public class Checkpoint : MonoBehaviour
{
    bool isNearCheckpoint = false;
    PlayerController playerController;
    GameObject john = null;
    EnemyAI[] simpleEnemies;
    Vector3[] enemyInitialPositions;
    Animator campfireAnimator;
    bool isCampfireLit = false;
    public int campfireID;
    GuardarPartida guardarPartida;

    void Start()
    {
        john = GameObject.Find("John");
        playerController = john.GetComponent<PlayerController>();
        guardarPartida = FindObjectOfType<GuardarPartida>(); // Obtener referencia a NuevaPartida

        simpleEnemies = FindObjectsOfType<EnemyAI>();
        enemyInitialPositions = new Vector3[simpleEnemies.Length];

        campfireAnimator = GetComponent<Animator>();
        isCampfireLit = PlayerPrefs.GetInt($"Campfire{campfireID}Lit", 0) == 1;

        if (isCampfireLit && campfireAnimator != null)
        {
            campfireAnimator.SetTrigger("Ignite");
        }

        for (int i = 0; i < simpleEnemies.Length; i++)
        {
            enemyInitialPositions[i] = simpleEnemies[i].transform.position;
        }
    }

    void Update()
    {
        if (isNearCheckpoint && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SaveGame();
        }
    }

   public void SaveGame()
{
    Debug.Log("Guardando el juego... posx: " + john.transform.position.x + " posy: " + john.transform.position.y);
    Debug.Log("John Data... posx: " + john.transform.position.x + " posy: " + john.transform.position.y);

    playerController.SetCurrentHealth(playerController.GetMaxHealth());
    float posX = john.transform.position.x;
    float posY = john.transform.position.y;
    int health = playerController.GetMaxHealth();
    int coins = playerController.GetTotalCoins();

    // 🔽 Guardar posición y estado del jugador
    PlayerPrefs.SetFloat("position_x", posX);
    PlayerPrefs.SetFloat("position_y", posY);
    PlayerPrefs.SetInt("health", health);
    PlayerPrefs.SetInt("coins", coins);

    // 🔽 Guardar nombre de la escena actual
    string currentSceneName = SceneManager.GetActiveScene().name;
    PlayerPrefs.SetString("current_scene", currentSceneName);
    Debug.Log("Escena actual guardada: " + currentSceneName);

    // 🔽 Guardar estado de enemigos
    for (int i = 0; i < simpleEnemies.Length; i++)
    {
        PlayerPrefs.SetFloat($"Enemy{i}_PositionX", enemyInitialPositions[i].x);
        PlayerPrefs.SetFloat($"Enemy{i}_PositionY", enemyInitialPositions[i].y);
        PlayerPrefs.SetInt($"Enemy{i}_IsDead", simpleEnemies[i].IsDead() ? 1 : 0);
    }

    // 🔽 Guardar estado de la hoguera
    if (!isCampfireLit && campfireAnimator != null)
    {
        campfireAnimator.SetTrigger("Ignite");
        isCampfireLit = true;
        PlayerPrefs.SetInt($"Campfire{campfireID}Lit", 1);
    }

    PlayerPrefs.Save();
    Debug.Log("Partida guardada en PlayerPrefs.");

    if (guardarPartida != null)
    {
        guardarPartida.SaveCheckpoint(posX, posY, health, coins);
    }
    else
    {
        Debug.LogError("No se encontró el script GuardarPartida en la escena.");
    }

    ResetEnemies();
}

    void ResetEnemies()
    {
        for (int i = 0; i < simpleEnemies.Length; i++)
        {
            if (simpleEnemies[i] != null)
            {
                simpleEnemies[i].ResetEnemy();
                simpleEnemies[i].transform.position = enemyInitialPositions[i];
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isNearCheckpoint = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isNearCheckpoint = false;
        }
    }
}
