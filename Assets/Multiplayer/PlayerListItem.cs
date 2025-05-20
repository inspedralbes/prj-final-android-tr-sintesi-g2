using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class PlayerListItem : MonoBehaviour
{
    public string PlayerName;
    public int ConnectionID;
    public ulong PlayerSteamID;
    private bool AvatarReceived;

    public Text PlayerNameText;
    public RawImage PlayerIcon;
    public Text PlayerReadyText;
    public bool Ready;

    // Agregamos una imagen por defecto
    [SerializeField] private Texture2D defaultIcon;

    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    public void ChangeReadyStatus()
    {
        if (Ready)
        {
            PlayerReadyText.text = "Ready";
            PlayerReadyText.color = Color.green;
        }
        else
        {
            PlayerReadyText.text = "Unready";
            PlayerReadyText.color = Color.red;
        }
    }
    private void Start()
    {
        // Asegurar que tenemos los componentes necesarios
        if (PlayerNameText == null)
        {
            Debug.LogError("PlayerNameText no está asignado en " + gameObject.name);
        }

        if (PlayerIcon == null)
        {
            Debug.LogError("PlayerIcon no está asignado en " + gameObject.name);
        }

        // Establecer un icono por defecto si está disponible
        if (defaultIcon != null && PlayerIcon != null)
        {
            PlayerIcon.texture = defaultIcon;
        }

        // Inicializar el callback solo si Steam está inicializado
        if (SteamManager.Initialized)
        {
            try
            {
                ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al crear callback de avatar: " + e.Message);
            }
        }

        // Actualizar la UI inicial
        SetPlayerValues();
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (PlayerIcon == null)
        {
            return;
        }

        if (callback.m_steamID.m_SteamID == PlayerSteamID)
        {
            try
            {
                Texture2D avatarTexture = GetSteamImageAsTexture(callback.m_iImage);
                if (avatarTexture != null)
                {
                    PlayerIcon.texture = avatarTexture;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al cargar imagen de avatar: " + e.Message);
            }
        }
    }

    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        if (!SteamManager.Initialized)
        {
            return null;
        }

        try
        {
            bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
            if (!isValid || width == 0 || height == 0)
            {
                return null;
            }

            byte[] image = new byte[width * height * 4];
            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if (isValid)
            {
                Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
                AvatarReceived = true;
                return texture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al procesar imagen de Steam: " + e.Message);
            return null;
        }

        return null;
    }

    void GetPlayerIcon()
    {
        if (!SteamManager.Initialized || PlayerSteamID == 0)
        {
            return;
        }

        try
        {
            CSteamID steamID = new CSteamID(PlayerSteamID);
            int ImageID = SteamFriends.GetLargeFriendAvatar(steamID);

            // -1 significa que la imagen no está disponible todavía y se cargará después
            if (ImageID == -1)
            {
                return;
            }

            Texture2D texture = GetSteamImageAsTexture(ImageID);
            if (texture != null && PlayerIcon != null)
            {
                PlayerIcon.texture = texture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al obtener icono de jugador: " + e.Message);
        }
    }

    public void SetPlayerValues()
    {
        // Actualizar texto del nombre del jugador
        if (PlayerNameText != null)
        {
            PlayerNameText.text = string.IsNullOrEmpty(PlayerName) ? "Player" : PlayerName;
        }
        ChangeReadyStatus();

        // Intentar obtener el avatar si aún no lo hemos recibido
        if (!AvatarReceived && PlayerIcon != null && SteamManager.Initialized && PlayerSteamID != 0)
        {
            GetPlayerIcon();
        }
    }

    // Añadir función para limpiar recursos al destruir
    private void OnDestroy()
    {
        try
        {
            ImageLoaded = null;
        }
        catch (System.Exception)
        {
            // Ignorar errores aquí
        }
    }
    
}