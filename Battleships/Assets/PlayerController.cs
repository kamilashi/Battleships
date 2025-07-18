using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Debug View")]
    PlayerState localPlayerState; //#DOTO: reroute all calls to playerState to go through playerController

    void Awake()
    {
        PlayerState.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(3))
        {
            localPlayerState.OnShipOrientationToggled();
        }
    }

    public void OnLocalPlayerInitialized(PlayerState localPlayer)
    {
        if (localPlayer.isLocalPlayer)
        {
            localPlayerState = localPlayer;
        }
    }
}
