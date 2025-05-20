using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class BarraVidaMulti : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private float lerpSpeed = 5f;

    private PlayerControllerMulti playerController;
    private float targetFillAmount;

    void Awake()
    {
        // Asegurar que la barra comience llena hasta que se actualice
        if (healthBar != null)
        {
            healthBar.fillAmount = 1f;
            targetFillAmount = 1f;
        }
    }

    void Update()
    {
        // Smooth interpolation of health bar
        if (healthBar != null && healthBar.fillAmount != targetFillAmount)
        {
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);
            
            // Snap to target when very close to avoid tiny differences
            if (Mathf.Abs(healthBar.fillAmount - targetFillAmount) < 0.01f)
            {
                healthBar.fillAmount = targetFillAmount;
            }
        }
    }

    public void InicializarVida()
    {
            gameObject.SetActive(true);
            Debug.Log("Barra de vida activada para el jugador local.");
            
    }

    // Actualizar la barra con los valores actuales
    public void ActualizarBarraVida(int currentHealth, int maxHealth)
    {
        Debug.Log($"[BARRA VIDA] GO: {gameObject.name}, healthBar: {(healthBar != null)}, fill: {healthBar?.fillAmount}");
        Debug.Log($"[HEALTH BAR] Está en canvas: {healthBar?.canvas.name}");


        // No necesitamos verificar isLocalPlayer aquí porque esto ya se comprueba
        // antes de llamar a este método en PlayerControllerMulti
        
        if (healthBar != null)
        {
            targetFillAmount = (float)currentHealth / maxHealth;
            
            // Para cambios grandes (como muerte o respawn), actualizar inmediatamente
            if (Mathf.Abs(healthBar.fillAmount - targetFillAmount) > 0.5f)
            {
                healthBar.fillAmount = targetFillAmount;
            }
            
            Debug.Log($"Actualizando barra: {currentHealth}/{maxHealth} = {targetFillAmount}");
        }
        else
        {
            Debug.LogError("Referencia a Image healthBar es null");
        }
    }

    // Método para visualizar daño recibido (efecto opcional)
    public void MostrarDanoRecibido()
    {
        if (healthBar != null)
        {
            // Puedes añadir aquí efectos visuales para el daño
            // Por ejemplo, parpadeo rojo temporal
            StartCoroutine(EfectoParpadeoDano());
        }
    }

    private System.Collections.IEnumerator EfectoParpadeoDano()
    {
        Color originalColor = healthBar.color;
        Color damageColor = Color.red;
        
        for (int i = 0; i < 3; i++)
        {
            healthBar.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            healthBar.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
}