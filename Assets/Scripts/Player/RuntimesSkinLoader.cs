using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class RuntimeSkinLoader : MonoBehaviour
{
    [SerializeField] private string backendUrl = "http://localhost:3005/imagenes/shop/";
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    
    // Definición de animaciones del personaje
    [System.Serializable]
    public class AnimationData
    {
        public string animationName;    // Nombre de la animación (debe coincidir con el nombre en el Animator)
        public string spritesheetSuffix; // Sufijo del archivo en el servidor
        public int framesCount;         // Número total de frames
        public int framesPerRow;        // Frames por fila en el spritesheet
        public float frameRate = 12f;   // Velocidad de animación
        public bool loop = false;       // Si la animación se repite en bucle
    }
    
    [SerializeField] private AnimationData[] animations;
    
    // Diccionario para almacenar los datos de animación por nombre
    private Dictionary<string, AnimationData> animationDataMap = new Dictionary<string, AnimationData>();
    
    // Diccionario para almacenar los sprites cargados por nombre de animación
    private Dictionary<string, Sprite[]> loadedSprites = new Dictionary<string, Sprite[]>();
    
    // Para controlar la animación actual
    private string currentAnimation = "";
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private Coroutine animationCoroutine;
    
    private void Awake()
    {
        // Inicializar el mapa de datos de animación
        foreach (var anim in animations)
        {
            animationDataMap[anim.animationName] = anim;
        }
        
        // Inicializar la detección de cambios en el Animator
        StartCoroutine(MonitorAnimatorStates());
    }
    
    private void Start()
    {
        // Cargar la skin seleccionada
        string selectedSkin = PlayerPrefs.GetString("selectedSkin", "Default");
        LoadSkin(selectedSkin);
    }
    
    public void LoadSkin(string skinName)
    {
        StartCoroutine(LoadAllAnimations(skinName));
    }
    
    private IEnumerator LoadAllAnimations(string skinName)
    {
        Debug.Log($"Cargando skin: {skinName}");
        
        // Limpiar sprites cargados previamente
        loadedSprites.Clear();
        
        // Cargar cada animación
        foreach (var animData in animations)
        {
            yield return LoadAnimation(skinName, animData);
        }
        
        Debug.Log($"Skin '{skinName}' cargada completamente");
    }
    
    private IEnumerator LoadAnimation(string skinName, AnimationData animData)
    {
        // Construir la URL del spritesheet
        string spritesheetUrl = $"{backendUrl}{skinName}/{skinName}{animData.spritesheetSuffix}.png";
        Debug.Log($"Cargando spritesheet: {spritesheetUrl}");
        
        // Descargar el spritesheet
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(spritesheetUrl);
        yield return www.SendWebRequest();
        
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error cargando {spritesheetUrl}: {www.error}");
            yield break;
        }
        
        // Obtener la textura descargada
        Texture2D spritesheet = ((DownloadHandlerTexture)www.downloadHandler).texture;
        
        // Dividir el spritesheet en sprites individuales
        Sprite[] sprites = SliceSpritesheet(spritesheet, animData.framesCount, animData.framesPerRow);
        
        // Guardar los sprites en el diccionario
        loadedSprites[animData.animationName] = sprites;
        
        Debug.Log($"Animación '{animData.animationName}' cargada correctamente");
    }
    
    private Sprite[] SliceSpritesheet(Texture2D spritesheet, int framesCount, int framesPerRow)
    {
        Sprite[] sprites = new Sprite[framesCount];
        
        int frameWidth = spritesheet.width / framesPerRow;
        int frameHeight = frameWidth; // Asumimos frames cuadrados - ajusta si son rectangulares
        int rows = Mathf.CeilToInt((float)framesCount / framesPerRow);
        
        for (int i = 0; i < framesCount; i++)
        {
            int col = i % framesPerRow;
            int row = i / framesPerRow;
            
            // Crear el rectángulo para recortar el sprite
            Rect rect = new Rect(
                col * frameWidth,
                spritesheet.height - ((row + 1) * frameHeight), // Ajusta según la orientación de tu spritesheet
                frameWidth,
                frameHeight
            );
            
            // Crear el sprite
            sprites[i] = Sprite.Create(
                spritesheet,
                rect,
                new Vector2(0.5f, 0.5f), // Pivot en el centro
                100f // Píxeles por unidad
            );
        }
        
        return sprites;
    }
    
    // Monitorea constantemente el Animator para detectar cambios de estado
    private IEnumerator MonitorAnimatorStates()
    {
        // Nombre del estado actual
        string lastStateName = "";
        
        while (true)
        {
            // Obtener información del estado actual del Animator
            if (playerAnimator.isActiveAndEnabled)
            {
                // Obtener el estado actual
                AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
                
                // Determinar qué animación está activa buscando en todas las animaciones definidas
                foreach (var animData in animations)
                {
                    // Comprueba si el nombre del estado contiene el nombre de la animación
                    // Esto asume que los nombres de estado en tu Animator contienen el nombre de la animación
                    string fullStateName = $"Base Layer.{animData.animationName}";
                    if (stateInfo.IsName(fullStateName) && lastStateName != animData.animationName)
                    {
                        // Si hemos cambiado de estado, actualizar la animación
                        lastStateName = animData.animationName;
                        PlayAnimation(animData.animationName);
                        break;
                    }
                }
            }
            
            yield return null;
        }
    }
    
    // Reproducir una animación específica
    private void PlayAnimation(string animationName)
    {
        // Si ya estamos reproduciendo esta animación, no hacer nada
        if (currentAnimation == animationName) return;
        
        // Si la animación no está cargada, no hacer nada
        if (!loadedSprites.ContainsKey(animationName)) return;
        
        currentAnimation = animationName;
        currentFrame = 0;
        frameTimer = 0f;
        
        // Detener la coroutine anterior si existe
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // Iniciar la nueva animación
        animationCoroutine = StartCoroutine(AnimateSprites(animationName));
    }
    
    // Coroutine para animar los sprites
    private IEnumerator AnimateSprites(string animationName)
    {
        // Verificar que tenemos los datos de la animación
        if (!animationDataMap.ContainsKey(animationName) || !loadedSprites.ContainsKey(animationName))
        {
            Debug.LogError($"No se encontró la animación: {animationName}");
            yield break;
        }
        
        AnimationData animData = animationDataMap[animationName];
        Sprite[] sprites = loadedSprites[animationName];
        
        float frameDuration = 1f / animData.frameRate;
        int frameIndex = 0;
        
        // Loop de animación
        while (true)
        {
            // Aplicar el sprite actual
            playerSpriteRenderer.sprite = sprites[frameIndex];
            
            // Esperar la duración del frame
            yield return new WaitForSeconds(frameDuration);
            
            // Avanzar al siguiente frame
            frameIndex++;
            
            // Si llegamos al final de la animación
            if (frameIndex >= sprites.Length)
            {
                // Si es loop, volver al principio
                if (animData.loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    // Si no es loop, mantener el último frame y terminar
                    break;
                }
            }
        }
    }
    
    // Para depuración - permite cambiar manualmente la animación actual
    public void DebugPlayAnimation(string animName)
    {
        PlayAnimation(animName);
    }
}