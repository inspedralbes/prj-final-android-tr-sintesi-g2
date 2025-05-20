using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Steamworks;
public class LobbiesListManager : MonoBehaviour
{
    public static LobbiesListManager instance;
    public GameObject lobbiesMenu;
    public GameObject lobbyDataItemPrefab;
    public GameObject LobbyListContent;


    public GameObject lobbiesButton, hostButton;

    public List<GameObject> listOfLobbies = new List<GameObject>();

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }


    public void DestroyLobbies()
    {
        foreach (GameObject lobbyItem in listOfLobbies)
        {
            Destroy(lobbyItem);
        }
        listOfLobbies.Clear();
    }

    public void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t result)
    {
        for (int i = 0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                GameObject createdItem = Instantiate(lobbyDataItemPrefab);

                createdItem.GetComponent<LobbyDataEntry>().lobbyID = (CSteamID)lobbyIDs[i].m_SteamID;

                createdItem.GetComponent<LobbyDataEntry>().lobbyName =
                    SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");

                createdItem.GetComponent<LobbyDataEntry>().SetLobbyData();

                createdItem.transform.SetParent(LobbyListContent.transform);
                createdItem.transform.localScale = Vector3.one;

                listOfLobbies.Add(createdItem);
            }
        }

    }

    public void GetListOfLobbies()
    {
        lobbiesButton.SetActive(false);
        hostButton.SetActive(false);

        lobbiesMenu.SetActive(true);

        SteamLobby.Instance.GetLobbiesList();
    }
}
