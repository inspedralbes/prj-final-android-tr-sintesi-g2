using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Steamworks;

public class LobbyDataEntry : MonoBehaviour
{
    public CSteamID lobbyID;
    public string lobbyName;
    public Text lobbyNameText;


    public void JoinLobby()
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }
    public void SetLobbyData()
    {
        if (lobbyName == "")
        {
            lobbyNameText.text = "Empty";
        }
        else
        {
            lobbyNameText.text = lobbyName;
        }
    }


    
}
