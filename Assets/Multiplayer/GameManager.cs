using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class GameManager : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject difficultyUI;
    [SerializeField] private TextMeshProUGUI difficultyLevelText;
    [SerializeField] private TextMeshProUGUI bossStatsInfoText;

    [Header("Mensajes")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float messageDuration = 3f;

    private void Start()
    {
        if (difficultyUI != null)
        {
            UpdateDifficultyUI();
        }
    }

    private void Update()
    {
        // Actualizar UI periódicamente para mantenerse sincronizado con el DifficultyManager
        if (isClient && Time.frameCount % 60 == 0 && difficultyUI != null)
        {
            UpdateDifficultyUI();
        }
    }

    // Método para actualizar la UI con el nivel de dificultad actual
    private void UpdateDifficultyUI()
    {
        if (DifficultyManager.Instance != null)
        {
            int level = DifficultyManager.Instance.GetDifficultyLevel();

            if (difficultyLevelText != null)
            {
                difficultyLevelText.text = $"Nivel: {level}";
            }

            if (bossStatsInfoText != null)
            {
                // Calcular y mostrar información sobre los multiplicadores
                float healthBonus = level * 0.1f * 100; // 10% por nivel
                float damageBonus = level * 0.1f * 100; // 10% por nivel
                float speedBonus = level * 0.05f * 100; // 5% por nivel

                bossStatsInfoText.text = $"Bonus de enemigos:\n" +
                                        $"Salud: +{healthBonus}%\n" +
                                        $"Daño: +{damageBonus}%\n" +
                                        $"Velocidad: +{speedBonus}%";
            }
        }
    }

    // Método para mostrar mensajes temporales en la UI
    public void ShowMessage(string message, float duration = 0)
    {
        if (messagePanel != null && messageText != null)
        {
            messageText.text = message;
            messagePanel.SetActive(true);

            float actualDuration = duration > 0 ? duration : messageDuration;
            Invoke(nameof(HideMessage), actualDuration);
        }
    }

    private void HideMessage()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    // Método para ser llamado desde comandos de red
    [ClientRpc]
    public void RpcShowMessage(string message, float duration)
    {
        ShowMessage(message, duration);
    }

    // Instancia singleton para fácil acceso
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnBossDefeated(string bossName)
    {
        if (isServer)
        {
            RpcShowBossDefeatedMessage(bossName);
        }
    }

    [ClientRpc]
    private void RpcShowBossDefeatedMessage(string bossName)
    {
        ShowMessage($"¡{bossName} ha sido derrotado!", 3f);
    }

    public void OnDifficultyIncreased(int newLevel)
    {
        if (isServer)
        {
            RpcShowDifficultyIncreasedMessage(newLevel);
        }
        UpdateDifficultyUI();
    }

    [ClientRpc]
    private void RpcShowDifficultyIncreasedMessage(int newLevel)
    {
        ShowMessage($"¡Nivel {newLevel} alcanzado! Los enemigos son más fuertes ahora.", 5f);
    }
}
