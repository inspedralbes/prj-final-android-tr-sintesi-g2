using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class BarraManaMulti : MonoBehaviour
{
    [SerializeField] private Image manaBar;
    [SerializeField] private float lerpSpeed = 5f;

    private PlayerControllerMulti playerController;
    private float targetFillAmount;

    void Awake()
    {
        // Asegurar que la barra comience llena hasta que se actualice
        if (manaBar != null)
        {
            manaBar.fillAmount = 1f;
            targetFillAmount = 1f;
        }
    }

        public void InicializarMana()
    {
            gameObject.SetActive(true);
            Debug.Log("Barra de mana activada para el jugador local.");
            
    }

    // Actualizar la barra con los valores actuales
    public void ActualizarBarraMana(int currentMana, int maxMana)
    {
        Debug.Log($"[BARRA VIDA] GO: {gameObject.name}, manaBar: {(manaBar != null)}, fill: {manaBar?.fillAmount}");
        Debug.Log($"[HEALTH BAR] Está en canvas: {manaBar?.canvas.name}");


        // No necesitamos verificar isLocalPlayer aquí porque esto ya se comprueba
        // antes de llamar a este método en PlayerControllerMulti
        
        if (manaBar != null)
        {
            targetFillAmount = (float)currentMana / maxMana;
            
            // Para cambios grandes (como muerte o respawn), actualizar inmediatamente
            if (Mathf.Abs(manaBar.fillAmount - targetFillAmount) > 0.5f)
            {
                manaBar.fillAmount = targetFillAmount;
            }
            
            Debug.Log($"Actualizando barra: {currentMana}/{maxMana} = {targetFillAmount}");
        }
        else
        {
            Debug.LogError("Referencia a Image manaBar es null");
        }
    }

}
