using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Steamworks;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private PlayerObjectController GamePlayerPrefab;
    
    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();
    
    // Add these properties to track scene state
    private bool isChangingScene = false;

    // Override OnStartServer to handle server initialization


    public void StartGame(string SceneName)
    {
        ServerChangeScene(SceneName);
    }

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started successfully");
    }
    
    // Override OnServerConnect to handle new connections properly
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("Client connected: " + conn.connectionId);
    }
    
    // Override OnServerDisconnect to clean up player objects when clients disconnect
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Find and remove the disconnected player from our list
        PlayerObjectController playerToRemove = null;
        foreach (var player in GamePlayers)
        {
            if (player.ConnectionID == conn.connectionId)
            {
                playerToRemove = player;
                break;
            }
        }
        
        if (playerToRemove != null)
        {
            GamePlayers.Remove(playerToRemove);
        }
        
        base.OnServerDisconnect(conn);
    }
    
    // Properly handle adding players to the server
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("OnServerAddPlayer called for connection ID: " + conn.connectionId);
        
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            try
            {
                // Make sure the SteamLobby instance exists
                if (SteamLobby.Instance == null)
                {
                    Debug.LogError("SteamLobby.Instance is null!");
                    return;
                }
                
                // Make sure the lobby ID is valid
                if (SteamLobby.Instance.CurrentLobbyID == 0)
                {
                    Debug.LogError("CurrentLobbyID is invalid (0)!");
                    return;
                }
                
                // Create the player instance
                PlayerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);
                
                // Set player properties
                GamePlayerInstance.ConnectionID = conn.connectionId;
                GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
                
                // Get the Steam ID safely
                try
                {
                    CSteamID lobbyID = new CSteamID(SteamLobby.Instance.CurrentLobbyID);
                    int memberIndex = GamePlayers.Count;
                    
                    // Safety check for lobby member index
                    if (memberIndex < SteamMatchmaking.GetNumLobbyMembers(lobbyID))
                    {
                        GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, memberIndex);
                    }
                    else
                    {
                        Debug.LogWarning("Invalid lobby member index: " + memberIndex);
                        GamePlayerInstance.PlayerSteamID = 0;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error getting Steam ID: " + e.Message);
                    GamePlayerInstance.PlayerSteamID = 0;
                }
                
                // Add the player to the connection
                NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
                Debug.Log("Player added for connection: " + conn.connectionId);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in OnServerAddPlayer: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("OnServerAddPlayer called from non-Lobby scene: " + SceneManager.GetActiveScene().name);
            base.OnServerAddPlayer(conn);
        }
    }
    
    // Handle scene changes
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        isChangingScene = false;
        Debug.Log("Server scene changed to: " + sceneName);
    }
    
    // Handle client scene changes
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        Debug.Log("Client scene changed");
    }
    
    // Call this when you want to start a game and change scene
    // public void StartGame(string sceneName)
    // {
    //     if (!NetworkServer.active) return;
        
    //     isChangingScene = true;
    //     ServerChangeScene(sceneName);
    // }
}