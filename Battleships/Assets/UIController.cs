using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Setup")]
    public GameObject shipButtonPrefab;
    public RectTransform shipButtonParent;
    public Button submitButton;

    [Header("Debug View")]
    public PlayerState localPlayerState;
    public List<UIShipButton> shipButtons;

    void Awake()
    {
        PlayerState.onShipAdded.AddListener(UpdateShipButtons);
        PlayerState.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
    }

    void OnLocalPlayerInitialized(PlayerState placerState)
    {
        if (placerState.isLocalPlayer)
        {
            localPlayerState = placerState;
            Initialize();
        }
    }

    public void Initialize()
    {
        List<StaticShipData> shipDatas = localPlayerState.GetShipDataList();

        for( int i = 0; i < (int) ShipType.Count; i++)
        {
            GameObject cellGameObject = GameObject.Instantiate(shipButtonPrefab, shipButtonParent);
            UIShipButton buttonObj = cellGameObject.GetComponent<UIShipButton>();
            shipButtons.Add(buttonObj);
            buttonObj.Initialize(this, shipDatas[i]);
        }
    }

    public void UpdateShipButtons(PlayerState player, Vector2Int coords, RuntimeShipData shipInstanceData, Orientation orientation)
    {
        if(player.isLocalPlayer)
        {
            for (int i = 0; i < (int)ShipType.Count; i++)
            {
                UIShipButton buttonObj = shipButtons[i];
                buttonObj.UpdateShipCount(localPlayerState.GetLocalGameState().shipManager.availableShipCounts[i]);
            }
        }
    }

    public void OnShipButtonClicked(ShipType type)
    {
        localPlayerState.OnShipTypeSelected(type);
    }
    public void OnSubmitButtonClicked()
    {
        localPlayerState.OnSubmitSignalReceived();
    }

}
