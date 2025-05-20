using UnityEngine;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    //Callbacks
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<LobbyEnter_t> LobbyEntered;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;


    //Lobbies Callback
    protected Callback<LobbyMatchList_t> LobbyList;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdate;

    public List<CSteamID> lobbyIDs = new List<CSteamID>();


    //Variables
    public ulong CurrentLobbyID;
    private const string HostAdressKey = "HostAddress";
    private CustomNetworkManager manager;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
    }

    //GameObject
    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized!");
            return;
        }

        manager = GetComponent<CustomNetworkManager>();
        if (manager == null)
        {
            Debug.LogError("CustomNetworkManager not found on the same GameObject!");
            return;
        }

        //Callbacks
        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);

        LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
    }

    public void GetLobbiesList()
    {
        if (lobbyIDs.Count > 0) { lobbyIDs.Clear(); }

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(60);
        SteamMatchmaking.RequestLobbyList();
        
    }

    void OnGetLobbyList(LobbyMatchList_t result)
    {
        if (LobbiesListManager.instance.listOfLobbies.Count > 0) { LobbiesListManager.instance.DestroyLobbies(); }

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDs.Add(lobbyID);
            Debug.Log("Lobby ID: " + lobbyID);

            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        LobbiesListManager.instance.DisplayLobbies(lobbyIDs, result);
    }

    public void HostLobby()
    {
        Debug.Log("Host Lobby Button Clicked");

        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized! Cannot host lobby.");
            return;
        }

        try
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, manager.maxConnections);
            Debug.Log("CreateLobby called successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error creating Steam lobby: " + e.Message);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Lobby creation failed: " + callback.m_eResult.ToString());
            return;
        }

        Debug.Log("Lobby Created Successfully");

        CurrentLobbyID = callback.m_ulSteamIDLobby;

        try
        {
            manager.StartHost();
            Debug.Log("Network host started");

            SteamMatchmaking.SetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                HostAdressKey,
                SteamUser.GetSteamID().ToString()
            );

            SteamMatchmaking.SetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                "name",
                SteamFriends.GetPersonaName().ToString() + "'s Lobby"
            );

            Debug.Log("Lobby data set");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in OnLobbyCreated: " + e.Message);
        }
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Join Request");
        try
        {
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining lobby: " + e.Message);
        }
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        try
        {
            CurrentLobbyID = callback.m_ulSteamIDLobby;
            Debug.Log("Entered lobby: " + CurrentLobbyID);

            // Don't start the client if we're already the host
            if (NetworkServer.active)
            {
                Debug.Log("We are the host, not starting client");
                return;
            }

            Debug.Log("Starting as client");
            manager.networkAddress = SteamMatchmaking.GetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                HostAdressKey
            );

            manager.StartClient();
            Debug.Log("Client started, connecting to: " + manager.networkAddress);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in OnLobbyEntered: " + e.Message);
        }
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.JoinLobby(lobbyId);
    }
}