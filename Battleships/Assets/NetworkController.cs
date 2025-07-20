using kcp2k;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows;

public enum MultiplayerMode
{
    Local,
    Remote,
    None
}

public class NetworkController : MonoBehaviour
{
    public static UnityEvent<MultiplayerMode> onTransportSelected = new UnityEvent<MultiplayerMode>();
    public static UnityEvent onStartHostSelected = new UnityEvent();
    public static UnityEvent onStartClientSelected = new UnityEvent();
    public static UnityEvent onNetworkmanagerReady = new UnityEvent();
    public static UnityEvent<string, string> OnLocalMultiplayerDataSubmitted = new UnityEvent<string, string>();

    public static UnityEvent<string> onStatusInfoLogged = new UnityEvent<string>();
    public static UnityEvent onProceedToConnectionMenu = new UnityEvent();


    [Header("Setup")]
    public NetworkManager networkManager;
    public KcpTransport kcpTransport;
    public FizzySteamworks steamTransport;

    [Header("Auto Setup")]
    public SteamManager steamManager;

    [Header("Debug View")]
    public MultiplayerMode requestedMode;
    public MultiplayerMode assignedMode;

    private const string hostAddressKey = "HostAddress";

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> joinRequest;
    private Callback<LobbyEnter_t> lobbyEntered;

    private Coroutine waitForSteamInitCoroutine;
    private bool isRemoteReady;
    private bool remoteCallbacksInitialized;
    // public bool connectionRequested = false;

    void Awake()
    {
        onTransportSelected.AddListener(OnTransportSelected);
        onStartHostSelected.AddListener(OnHostStartRequested);
        onStartClientSelected.AddListener(OnClientStartRequested);
        OnLocalMultiplayerDataSubmitted.AddListener(OnLocalMultiplayerDataReceived);

        assignedMode = MultiplayerMode.None;

        kcpTransport.enabled = false;
        steamTransport.enabled = false;
        isRemoteReady = false;
        remoteCallbacksInitialized = false;
    }

    // Update is called once per frame
    void Update()
    {
       // if(connectionRequested && )
    }

    private void OnTransportSelected(MultiplayerMode transport)
    {
        requestedMode = transport;

        AssingTransport();

        if (requestedMode == MultiplayerMode.Local)
        {
            DisableSteamworks();
            return;
        }

        if (SteamAPI.IsSteamRunning())
        {
            EnableSteamworks();
        }
        else
        {
            if (waitForSteamInitCoroutine == null)
            {
                waitForSteamInitCoroutine = StartCoroutine(WaitForSteamCoroutine(EnableSteamworks));
            }
        }
    }

    private void OnHostStartRequested()
    {
        //AssingTransport();

        if (assignedMode == MultiplayerMode.Remote)
        {
            if (isRemoteReady)
            {
                HostRemoteLobby();
            }
            else
            {
                LogStatusInfo("Please launch the steam app and try again.");
            }
        }
        else
        {
            networkManager.StartHost();
        }
    }

    private void OnClientStartRequested()
    {
        //AssingTransport();

        if (assignedMode == MultiplayerMode.Remote)
        {
            LogStatusInfo("Please join via a Steam invite!");
        }
        else
        {
            networkManager.StartClient();
        }
    }

    private void OnLocalMultiplayerDataReceived(string ipAddress, string portNumber)
    {
        string logString = "";
        bool parsedSuccessfully = true;

        if (System.Net.IPAddress.TryParse(ipAddress, out _))
        {
            networkManager.networkAddress = ipAddress;
        }
        else
        {
            parsedSuccessfully = false;
            logString += "Invalid network address. The host can check theirs by running ipconfig in the command line. \n";
        }

        if (ushort.TryParse(portNumber, out ushort portResult))
        {
            kcpTransport.port = portResult;
        }
        else
        {
            parsedSuccessfully = false;
            logString += "Invalid port number. Please enter a number between 0 and 65535. \n";
        }

        LogStatusInfo(logString);

        if (parsedSuccessfully)
        {
            onProceedToConnectionMenu?.Invoke();
        }
    }

    private void AssingTransport()
    {
        if(requestedMode == assignedMode)
        {
            return;
        }

        if (requestedMode == MultiplayerMode.Remote)
        {
            if (!remoteCallbacksInitialized)
            {
                lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
                joinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
                lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

                remoteCallbacksInitialized = true;
            }

            kcpTransport.enabled = false;
            steamTransport.enabled = true;

            networkManager.transport = steamTransport;
            networkManager.Initialize();
        }
        else // if(selectedTransport == MultiplayerMode.Local )
        {
            kcpTransport.enabled = true;
            steamTransport.enabled = false;

            networkManager.transport = kcpTransport;
        }


        assignedMode = requestedMode;
    }

    private void EnableSteamworks()
    {
        if(isRemoteReady)
        {
            onProceedToConnectionMenu?.Invoke();
            return;
        }

        if (steamManager == null)
        {
            steamManager = gameObject.AddComponent<SteamManager>();
        }

        steamTransport.enabled = true;
        isRemoteReady = true;
        LogStatusInfo("Steam is initialized and ready!");

        onProceedToConnectionMenu?.Invoke();
    }

    private void OnDisable()
    {
        DisableSteamworks();
    }

    private void DisableSteamworks()
    {
        SteamAPI.Shutdown();
        Debug.Log("Called SteamAPI shutdown");

        if (!isRemoteReady)
        {
            return;
        }

        if (steamManager != null)
        {
            Destroy(steamManager);
            steamManager = null;
        }

        steamTransport.enabled = false;

        isRemoteReady = false;
        LogStatusInfo("Steam connection has been disabled.");
    }

    public void HostRemoteLobby()
    {

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, GameManager.intendedPlayerCount);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) { return; }

        LogStatusInfo("Lobby Created!");

        networkManager.StartHost();
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), hostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName() + "'s Lobby"); //change the name of the Lobby
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Request to join lobby");

        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) { return; } // if hosting
        networkManager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), hostAddressKey);
        networkManager.StartClient();
    }

    IEnumerator WaitForSteamCoroutine(Action onSteamApiInitialized)
    {

        while (!SteamAPI.IsSteamRunning())
        {
            LogStatusInfo("The Steam app needs to be running for a remote session.");
            yield return new WaitForSeconds(2f);
        }

        if (SteamAPI.IsSteamRunning() && SteamAPI.Init())
        {
            onSteamApiInitialized.Invoke();
        }
        else
        {
            LogStatusInfo("Steam failed to initialize. Please try again.");
        }

        waitForSteamInitCoroutine = null;
    }

    private void LogStatusInfo(string info)
    {
        onStatusInfoLogged?.Invoke(info);
    }
}
