using UnityEngine;
using TMPro;
using Mirror;

public class DifficultyDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI difficultyLevelText;
    [SerializeField] private TextMeshProUGUI bossBonusText;
    [SerializeField] private GameObject difficultyPanel;
    
    // Opciones de visibilidad
    [SerializeField] private bool showAlways = false;
    [SerializeField] private bool showOnToggle = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    
    private bool isVisible = false;
    
    private void Start()
    {
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(showAlways || isVisible);
        }
        UpdateInfo();
    }
    
    private void Update()
    {
        // Actualización periódica de información
        if (Time.frameCount % 60 == 0)
        {
            UpdateInfo();
        }
        
        // Toggle de visibilidad con tecla
        if (showOnToggle && Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (difficultyPanel != null)
            {
                difficultyPanel.SetActive(showAlways || isVisible);
            }
        }
    }
    
    private void UpdateInfo()
    {
        if (DifficultyManager.Instance == null) return;
        
        int level = DifficultyManager.Instance.GetDifficultyLevel();
        
        if (difficultyLevelText != null)
        {
            difficultyLevelText.text = $"Nivel de dificultad: {level}";
        }
        
        if (bossBonusText != null)
        {
            float healthBonus = level * 0.1f * 100; // 10% por nivel
            float damageBonus = level * 0.1f * 100; // 10% por nivel
            float speedBonus = level * 0.05f * 100; // 5% por nivel
            
            bossBonusText.text = $"Bonus de enemigos:\n" +
                                $"• Salud: +{healthBonus:F0}%\n" +
                                $"• Daño: +{damageBonus:F0}%\n" +
                                $"• Velocidad: +{speedBonus:F0}%";
        }
    }
    
    // Método para mostrar u ocultar el panel desde otros scripts
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(showAlways || isVisible);
        }
    }
}