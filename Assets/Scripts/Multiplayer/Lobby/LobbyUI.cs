using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("Lobby Query")]
    [SerializeField] private Transform lobbiesHolder;
    [SerializeField] private GameObject lobbyTemplate;
    private Transform lobbiesQuery;
    [SerializeField] private Button refreshButton;

    [Header("Current lobby")]
    [SerializeField] private TextMeshProUGUI currentLobbyName;
    [SerializeField] private Transform currentLobbyPlayersHolder;
    [SerializeField] private GameObject playerTemplate;
    [SerializeField] private Button startGameButton;
    public TMP_InputField lobbyInputField;
    private Transform currentLobbyForm;

    [Header("Lobby creation menu")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Transform lobbyCreationMenu;

    [SerializeField] private TMP_InputField playerName;

    [Header("In lobby main menu changes")]
    [SerializeField] private GameObject returnToLobbyButton;
    [SerializeField] private GameObject onlineButton;
    private void Awake()
    {
        Instance = this;

        GetPlayerName();


        refreshButton.onClick.AddListener(RefreshButtonClick);
        createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
    }

    private void Start()
    {
        currentLobbyForm = currentLobbyName.transform.parent;
        lobbiesQuery = lobbiesHolder.transform.parent.parent;

        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        LobbyManager.Instance.OnJoinedLobby      += LobbyManager_OnJoinedLobby;
        LobbyManager.Instance.OnLeftLobby        += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby  += LobbyManager_OnKickedFromLobby;
    }

    private void LobbyManager_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
    {
        UpdateQueryList(e.lobbyList);
    }
    private void LobbyManager_OnJoinedLobby (object sender, LobbyManager.LobbyEventArgs e)
    {
        Hide();
        startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
    }   
    private void LobbyManager_OnLeftLobby(object sender, EventArgs e)
    {
        Show();
    }   
    private void LobbyManager_OnKickedFromLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Show();
    }

    //Shows lobby querry menu
    private void Hide()
    {
        lobbiesQuery.gameObject.SetActive(false);
        onlineButton.SetActive(false);
        returnToLobbyButton.SetActive(true);
        //currentLobbyForm.gameObject.SetActive(false);
    }
    //Hides lobby querry menu
    private void Show()
    {
        returnToLobbyButton.SetActive(false);
        onlineButton.SetActive(true);
        //currentLobbyForm.gameObject.SetActive(true);
    }
    public void UpdateQueryList(List<Lobby> lobbyList)
    {
        ClearLobbies();
        foreach (Lobby lobby in lobbyList)
        {
            GameObject currentLobby = Instantiate(lobbyTemplate, lobbiesHolder);
            currentLobby.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = lobby.Name;
            currentLobby.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Players.Count + "/" + lobby.MaxPlayers.ToString();
            currentLobby.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() => LobbyManager.Instance.JoinLobbyButton(lobby));
        }
    }

    public void UpdatePlayersInLobby(Lobby lobby)
    {
        currentLobbyName.text = lobby.Name;
        ClearPlayers();
        foreach (Player player in lobby.Players)
        {

            GameObject currentPlayer = Instantiate(playerTemplate, currentLobbyPlayersHolder);
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                currentPlayer.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = player.Data["PlayerName"].Value;
            };
        }
    }

    public void UpdateCurrentLobbyInfo(Lobby lobby)
    {
        currentLobbyName.text = lobby.Name;
        UpdatePlayersInLobby(lobby);
    }

    private void ClearLobbies()
    {
        foreach (Transform child in lobbiesHolder.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private void ClearPlayers()
    {
        foreach (Transform child in currentLobbyPlayersHolder)
        {
            Destroy(child.gameObject);
        }
    }

    public string GetLobbyName()
    {
        return lobbyInputField.text;
    }

    public void JoinLobby(Lobby lobby)
    {
        currentLobbyForm.gameObject.SetActive(true);
        lobbyCreationMenu.gameObject.SetActive(false);

        UpdateCurrentLobbyInfo(lobby);
    }

    private void RefreshButtonClick()
    {
        LobbyManager.Instance.ListLobbies();
    } 
    private void CreateLobbyButtonClick()
    {

    }

    public void UpdatePlayerName(string value)
    {
        PlayerPrefs.SetString("PlayerName", value);
    }

    private void GetPlayerName()
    {
        playerName.text = PlayerPrefs.GetString("PlayerName");
    }
}
