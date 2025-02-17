using UnityEngine;
using UnityEngine.UIElements;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;  // Para cargar escenas

public class CargarPartidaAsset : MonoBehaviour
{
    private Button loadGameButton;
    private Button newGameButton;
    private Button exitButton;
    private Label dynamicText1;
    private Label dynamicText2;
    private Label dynamicText3;

    private const string apiUrl = "http://localhost:3000";

    private bool isGameActive = false;
    private string currentNickname;  // Usaremos el nickname en lugar del playerId

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        // Enlazamos los botones de la UI
        loadGameButton = root.Q<Button>("LoadGameButton");
        newGameButton = root.Q<Button>("NewGameButton");
        exitButton = root.Q<Button>("ExitButton");

        dynamicText1 = root.Q<Label>("DynamicText1");
        dynamicText2 = root.Q<Label>("DynamicText2");
        dynamicText3 = root.Q<Label>("DynamicText3");

        // Obtener el nickname desde PlayerPrefs
        currentNickname = PlayerPrefs.GetString("nickname", "");

        // Configuramos los botones para manejar las interacciones
        // Llamamos a LoadLastGame directamente para mostrar la información de la partida cargada
        // Nota: Esto cargará los datos tan pronto como se habilite la pantalla.
        if (!string.IsNullOrEmpty(currentNickname))
        {
            LoadLastGame(currentNickname);
        }
        else
        {
            dynamicText1.text = "No se encontró un nickname almacenado.";
            dynamicText2.text = "";
            dynamicText3.text = "";
        }

        // Cambiar a la pantalla principal
    }

    private async Task LoadLastGame(string nickname)
    {
        using (HttpClient client = new HttpClient())
        {
            // Modificar la URL para usar el nickname en lugar del playerId
            HttpResponseMessage response = await client.GetAsync($"{apiUrl}/lastGame/{nickname}");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var gameData = JsonConvert.DeserializeObject<GameData>(json);

                // Mostrar los valores iniciales cuando se carga la partida
                dynamicText1.text = $"Tiempo jugado: {gameData.time_played} segundos";
                dynamicText2.text = $"Nivel alcanzado: {gameData.level_reached}";
                dynamicText3.text = $"Progreso: {gameData.total_progress}%";
            }
            else
            {
                dynamicText1.text = "No se encontró una partida";
                dynamicText2.text = "";
                dynamicText3.text = "";
            }
        }
    }
}

public class GameData
{
    public int time_played { get; set; }
    public int level_reached { get; set; }
    public float total_progress { get; set; }
}
