using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using System.Collections;

public class PlayerObjectController : NetworkBehaviour
{
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;

    private CustomNetworkManager manager;



    public void CanStartGame(string SceneName)
    {
        if (isOwned)
        {
         CmdCanStartGame(SceneName);
        }
    }

    [Command]
    public void CmdCanStartGame(string SceneName)
    {
        manager.StartGame(SceneName);
    }
    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.Ready = newValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this.Ready, !this.Ready);
    }

    public void ChangeReady()
    {
        if (isOwned)
        {
            CmdSetPlayerReady();
        }
    }
    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
        
        // Ensure LobbyController exists before calling its methods
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.FindLocalPlayer();
            LobbyController.Instance.UpdateLobbyName();
        }
        else
        {
            Debug.LogError("LobbyController.Instance is null in OnStartAuthority");
        }
    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        
        // Ensure LobbyController exists before calling its methods
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdateLobbyName();
            LobbyController.Instance.UpdatePlayerList();
        }
        else
        {
            Debug.LogError("LobbyController.Instance is null in OnStartClient");
        }
    }

    public override void OnStopClient()
    {
        Manager.GamePlayers.Remove(this);
        
        // Ensure LobbyController exists before calling its methods
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerName(string PlayerName)
    {
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string oldValue, string newValue)
    {
        if (isServer)
        {
            this.PlayerName = newValue;
        }
        
        if (isClient)
        {
            // Ensure LobbyController exists before calling its methods
            if (LobbyController.Instance != null)
            {
                LobbyController.Instance.UpdatePlayerList();
            }
        }
    }
}