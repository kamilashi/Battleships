using kcp2k;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum MultiplayerMode
{
    Local,
    Remote
}

public class NetworkController : MonoBehaviour
{
    public static UnityEvent<MultiplayerMode> onTransportSelected = new UnityEvent<MultiplayerMode>();
    public static UnityEvent onStartHostSelected = new UnityEvent();
    public static UnityEvent onStartClientSelected = new UnityEvent();
    public static UnityEvent onNetworkmanagerReady = new UnityEvent();
    public static UnityEvent<string> onStatusInfoLogged = new UnityEvent<string>();

    [Header("Setup")]
    public NetworkManager networkManager;
    public KcpTransport kcpTransport;

    [Header("Debug View")]
    public MultiplayerMode selectedTransport;
   // public bool connectionRequested = false;

    void Awake()
    {
        onTransportSelected.AddListener(OnTransportSelected);
        onStartHostSelected.AddListener(OnHostStartRequested);
        onStartClientSelected.AddListener(OnClientStartRequested);
    }

    // Update is called once per frame
    void Update()
    {
       // if(connectionRequested && )
    }

    private void OnTransportSelected(MultiplayerMode transport)
    {
        selectedTransport = transport;
    }
    private void OnHostStartRequested()
    {
        AssingTransport();
        networkManager.StartHost();
    }
    private void OnClientStartRequested()
    {
        AssingTransport();
        networkManager.StartClient();
    }

    private void AssingTransport()
    {
        networkManager.transport = kcpTransport; //selectedTransport == MultiplayerMode.Local ? kcpTransport : null;
        networkManager.transport.enabled = true;
    }

    private void LogStatusInfo(string info)
    {
        onStatusInfoLogged?.Invoke(info);
    }
}
