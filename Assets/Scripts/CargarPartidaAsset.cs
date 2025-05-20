using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Newtonsoft.Json; // No olvides este using si usas JsonConvert
using System.Collections;
public class CargarPartidaAsset : MonoBehaviour
{
    private Button shopButton;
    private Button loadGameButton;
    private Button newGameButton;
    private Button exitButton;
    private Label dynamicText1;
    private Label dynamicText2;
    private Label dynamicText3;

    private const string apiUrl = "http://localhost:3002";

    private string currentNickname;

    void Start()
    {
        Debug.Log("CargarPartidaAsset Start() llamado");
    }
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        shopButton = root.Q<Button>("ShopButton");
        loadGameButton = root.Q<Button>("LoadGameButton");
        newGameButton = root.Q<Button>("NewGameButton");
        exitButton = root.Q<Button>("ExitButton");

        dynamicText1 = root.Q<Label>("DynamicText1");
        dynamicText2 = root.Q<Label>("DynamicText2");
        dynamicText3 = root.Q<Label>("DynamicText3");

        currentNickname = PlayerPrefs.GetString("nickname", "");

        // if (!string.IsNullOrEmpty(currentNickname))
        // {
        //     StartCoroutine(LoadLastGameCoroutine(currentNickname));
        // }
        // else
        // {
        //     dynamicText1.text = "No se encontró un nickname almacenado.";
        //     dynamicText2.text = "";
        //     dynamicText3.text = "";
        // }
    }

    // public IEnumerator LoadLastGameCoroutine(string nickname)
    // {
    //     using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl}/lastGame/{nickname}"))
    //     {
    //         yield return request.SendWebRequest();

    //         if (request.result == UnityWebRequest.Result.Success)
    //         {
    //             var json = request.downloadHandler.text;
    //             var gameData = JsonConvert.DeserializeObject<GameData>(json);

    //             dynamicText1.text = $"Tiempo jugado: {gameData.time_played} segundos";
    //             dynamicText2.text = $"Nivel alcanzado: {gameData.level_reached}";
    //             dynamicText3.text = $"Progreso: {gameData.total_progress}%";
    //         }
    //         else
    //         {
    //             dynamicText1.text = "No se encontró una partida";
    //             dynamicText2.text = "";
    //             dynamicText3.text = "";
    //         }
    //     }
    // }
}

public class GameData
{
    public int time_played { get; set; }
    public int level_reached { get; set; }
    public float total_progress { get; set; }
}