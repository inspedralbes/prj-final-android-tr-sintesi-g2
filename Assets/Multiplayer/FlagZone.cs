using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class FlagZone : NetworkBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private float delayBeforeReload = 3f;
    
    // Efecto visual para cuando se alcanza la bandera
    [SerializeField] private GameObject flagReachedVFX;
    
    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer) return;
        
        if (collision.CompareTag("Player"))
        {
            Debug.Log("[SERVER] Jugador ha alcanzado la bandera!");
            
            // Mostrar efectos visuales en todos los clientes
            if (flagReachedVFX != null)
            {
                RpcPlayFlagReachedEffects();
            }
            
            // Mostrar UI de nivel completado si existe
            if (levelCompleteUI != null)
            {
                RpcShowLevelCompleteUI(true);
            }
            
            // Aumentar el nivel de dificultad
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.IncreaseDifficulty();
                
                // Puedes mostrar información del nuevo nivel a los jugadores
                RpcShowDifficultyIncreasedMessage(DifficultyManager.Instance.GetDifficultyLevel());
            }
            
            // Cargar la escena después de un retraso
            Invoke(nameof(ReloadScene), delayBeforeReload);
        }
    }
    
    [Server]
    private void ReloadScene()
    {
        // Primero notificamos a los clientes que se va a cargar una nueva escena
        RpcPrepareForSceneChange();
        
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Para Mirror, usamos NetworkManager para cambiar de escena
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.ServerChangeScene(sceneToLoad);
            }
            else
            {
                Debug.LogError("[SERVER] NetworkManager no encontrado!");
            }
        }
        else
        {
            // Recargamos la escena actual si no se especificó una
            Scene currentScene = SceneManager.GetActiveScene();
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.ServerChangeScene(currentScene.name);
            }
            else
            {
                Debug.LogError("[SERVER] NetworkManager no encontrado!");
            }
        }
    }
    
    [ClientRpc]
    private void RpcPlayFlagReachedEffects()
    {
        if (flagReachedVFX != null)
        {
            // Instanciar efectos de partículas o activar animaciones
            GameObject vfx = Instantiate(flagReachedVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);  // Destruir después de unos segundos
        }
        
        // Opcionalmente reproducir sonido
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.Play();
        }
    }
    
    [ClientRpc]
    private void RpcShowLevelCompleteUI(bool show)
    {
        if (levelCompleteUI != null)
        {
            levelCompleteUI.SetActive(show);
        }
    }
    
    [ClientRpc]
    private void RpcShowDifficultyIncreasedMessage(int newLevel)
    {
        Debug.Log($"¡Nivel {newLevel} alcanzado! Los bosses serán más fuertes ahora.");
        
        // Aquí podrías mostrar un mensaje en la UI para todos los jugadores
        // Por ejemplo, usando un TextMeshProUGUI o un sistema de notificaciones propio
    }
    
    [ClientRpc]
    private void RpcPrepareForSceneChange()
    {
        // Código que se ejecuta en todos los clientes antes del cambio de escena
        // Por ejemplo, guardar datos locales, mostrar pantalla de carga, etc.
        Debug.Log("[CLIENT] Preparando para cambio de escena...");
    }
}