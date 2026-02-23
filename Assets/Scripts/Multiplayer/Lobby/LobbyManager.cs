using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System.Collections.Generic;
using System;
using WebSocketSharp.Net;



public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private string playerName;
    private float lobbyUpdateTimer;
    private string lobbyName;
    private float refreshLobbyListTimer = 5f;
    private float lobbyPollTimer;


    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler OnGameStarted;
    //public event EventHandler<LobbyEventArgs> OnLobbyDifficultyChanged;

    private string KEY_START_GAME = "KEY_START_GAME";

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        OnLeftLobby += (object sender, EventArgs e) =>
        {
            joinedLobby = null;
            hostLobby = null;
        };

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate()
    {
        this.playerName = PlayerPrefs.GetString("PlayerName");
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in!" + AuthenticationService.Instance.PlayerId);
            ListLobbies();
        };

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

    }

    private async void HandleRefreshLobbyList()
    {
        if(UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            refreshLobbyListTimer -= Time.deltaTime;
            if(refreshLobbyListTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if(joinedLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if(lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                PrintPlayers(joinedLobby);

                if (!IsPlayerInLobby())
                {
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }

                if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    if (!IsLobbyHost()) // Lobby Host already joined Relay
                    {
                        RelayMultiplayerConnection.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                    }

                    joinedLobby = null;

                    OnGameStarted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }

    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby()
    {
        if(joinedLobby != null && joinedLobby.Players != null)
        {
            foreach(Player player in joinedLobby.Players)
            {
                if(player.Id == AuthenticationService.Instance.PlayerId)
                {
                    return true;
                }
            }
        }
        return false;
    }


    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName) }
                }
        };
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }  
    private async void HandleLobbyPollUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(hostLobby.Id);
                joinedLobby = lobby;

                PrintPlayers(lobby);
            }
        }
    }

    public async void CreateLobby()
    {
        try
        {
            Player player = GetPlayer();

            lobbyName = LobbyUI.Instance.GetLobbyName();
            int maxPlayers = 12;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            joinedLobby = lobby;
            hostLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
            PrintPlayers(lobby);

            //LobbyQueryUI.Instance.UpdatePlayersInLobby(lobby);

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void ListLobbies()
    {
        try
        {

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
        QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
        {
            Count = 25,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                new QueryFilter(QueryFilter.FieldOptions.HasPassword, "0", QueryFilter.OpOptions.EQ)
            },
            Order = new List<QueryOrder>
            {
                new QueryOrder(false,QueryOrder.FieldOptions.Created)
            }
        };
        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

        OnLobbyListChanged.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = queryResponse.Results });
        //LobbyQueryUI.Instance.UpdateQueryList(queryResponse);

        Debug.Log("Lobbies found " + queryResponse.Results.Count);
        foreach (Lobby lobby in queryResponse.Results) 
        {
            Debug.Log("Lobby: " + lobby.Id);
        }
    }

    private async void JoinLobby()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            await LobbyService.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
        }
        catch(LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            joinedLobby = lobby;

            Debug.Log("Joined lobby with code " + lobbyCode);
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void JoinLobbyButton(Lobby lobby)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };
            Lobby lobbyJ = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, joinLobbyByIdOptions);

            joinedLobby = lobbyJ;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobbyJ });
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        LobbyUI.Instance.JoinLobby(lobby);
   

    }

    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }


    private void PrintPlayers(Lobby lobby)
    {
        //Debug.Log("Player in Lobby " + lobby.Name);
        //foreach(Player player in lobby.Players)
        //{
        //    Debug.Log(player.Id + " " + player.Data["PlayerName"].Value); 
        //}
        LobbyUI.Instance.UpdatePlayersInLobby(lobby);
    }

    private async void UpdateLobbyName(string name)
    {
        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id,new UpdateLobbyOptions { Name = name });
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            OnLeftLobby.Invoke(this, new LobbyEventArgs {lobby = joinedLobby});
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private async void MigrateLobbyHost()
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id,
            });
            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void StartGame()
    {
        if (IsLobbyHost())
        {
            try
            {
                Debug.Log("StartGame");

                string relayCode = await RelayMultiplayerConnection.Instance.CreateRelay();

                Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                GameSettingsCarrier.Instance.ExpectedPlayers = lobby.Players.Count;
                joinedLobby = lobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
}
