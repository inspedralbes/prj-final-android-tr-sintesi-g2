using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Necesario para usar Coroutines

public class BossDeathSceneLoaderMulti : MonoBehaviour
{
    [SerializeField] private BossTutorialMultiplayer boss; // Referencia al script del jefe
    [SerializeField] private string sceneToLoad = "NombreDeLaEscena"; // Nombre de la escena a cargar
    [SerializeField] private float delayBeforeSceneLoad = 10f; // Segundos a esperar antes de cargar la escena

    private void Start()
    {
        if (boss == null)
        {
            Debug.LogError("No se ha asignado el BossTutorial en el Inspector.");
            return;
        }

        boss.OnBossDeath += HandleBossDeath;
    }

    private void OnDestroy()
    {
        if (boss != null)
        {
            boss.OnBossDeath -= HandleBossDeath;
        }
    }

    private void HandleBossDeath()
    {
        Debug.Log("El jefe ha muerto. Cargando la escena en " + delayBeforeSceneLoad + " segundos.");
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneLoad);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No se ha especificado una escena para cargar.");
        }
    }
}
