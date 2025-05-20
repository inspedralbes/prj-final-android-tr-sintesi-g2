using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Animator))]
public class WebGLSkinManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string baseUrl = "http://localhost:3009/imagenes/shop/";
    [SerializeField] private string defaultSkin = "Default";

    private readonly string[] actions = { "Idle", "Run", "Jump", "Dash", "Fall", "Attack", "TakeHit", "Death" };
    private HashSet<string> loopActions = new HashSet<string> { "Fall", "Idle", "Run" };

    // Cantidad de frames por acción
    private Dictionary<string, int> framesPerAction = new Dictionary<string, int>()
    {
        { "Idle", 10 },
        { "Run", 8 },
        { "Jump", 4 },
        { "Dash", 4 },
        { "Fall", 4 },
        { "Attack", 6 },
        { "TakeHit", 4 },
        { "Death", 8 }
    };

    private Dictionary<string, Sprite[]> actionFrames = new Dictionary<string, Sprite[]>();

    private Animator animator;
    private string currentAction = "";

    private float animationTimer = 0f;
    private int currentFrame = 0;
    [SerializeField] private float frameDuration = 0.1f; // Duración de cada frame (ajustable)

    void Start()
    {
        animator = GetComponent<Animator>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        string skinName = PlayerPrefs.GetString("selectedSkin", defaultSkin);
        StartCoroutine(DownloadSkinSprites(skinName));
    }

    void Update()
    {
        if (actionFrames.Count == 0 || spriteRenderer == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        string newAction = GetActionFromState(stateInfo);

        if (newAction != currentAction)
        {
            currentAction = newAction;
            currentFrame = 0;
            animationTimer = 0f;
        }

        animationTimer += Time.deltaTime;

        if (!actionFrames.ContainsKey(currentAction)) return;

        Sprite[] frames = actionFrames[currentAction];
        if (frames.Length == 0) return;

        if (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;

            if (loopActions.Contains(currentAction))
            {
                spriteRenderer.sprite = frames[currentFrame];
                currentFrame = (currentFrame + 1) % frames.Length;
            }
            else
            {
                if (currentFrame < frames.Length - 1)
                {
                    spriteRenderer.sprite = frames[currentFrame];
                    currentFrame++;
                }
                else
                {
                    // Mantener el último frame
                    spriteRenderer.sprite = frames[frames.Length - 1];
                }
            }
        }
    }

    IEnumerator DownloadSkinSprites(string skinName)
    {
        actionFrames.Clear();
        foreach (string action in actions)
        {
            string spriteUrl = $"{baseUrl}{skinName}/{skinName}{action}.png";
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(spriteUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();

                    int framesCount = 1;
                    if (!framesPerAction.TryGetValue(action, out framesCount))
                    {
                        Debug.LogWarning($"No definido frames para acción '{action}', usando 1 por defecto.");
                        framesCount = 1;
                    }

                    Sprite[] frames = SliceSpriteSheet(texture, framesCount);
                    actionFrames[action] = frames;

                    Debug.Log($"Spritesheet '{action}' descargado y cortado en {frames.Length} frames.");
                }
                else
                {
                    Debug.LogWarning($"Error descargando '{action}': {request.error}");
                }
            }
        }

        currentAction = actionFrames.ContainsKey("Idle") ? "Idle" : actions[0];
        currentFrame = 0;
        animationTimer = 0f;
    }

    Sprite[] SliceSpriteSheet(Texture2D texture, int framesCount)
    {
        int frameWidth = texture.width / framesCount;
        int frameHeight = texture.height;

        Sprite[] frames = new Sprite[framesCount];

        for (int i = 0; i < framesCount; i++)
        {
            Rect rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.55f), 100f);
        }

        return frames;
    }

    string GetActionFromState(AnimatorStateInfo stateInfo)
    {
        foreach (string action in actions)
        {
            if (stateInfo.IsName(action) || stateInfo.shortNameHash.ToString().Contains(action))
            {
                return action;
            }
        }

        // Fallback
        return actionFrames.ContainsKey("Idle") ? "Idle" : actions[0];
    }
}
