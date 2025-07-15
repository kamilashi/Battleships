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
    public PlayerController localPlayerController;
    public List<UIShipButton> shipButtons;


    void Awake()
    {
        PlayerController.onShipAdded.AddListener(UpdateShipButtons);
        PlayerController.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
    }

    void OnLocalPlayerInitialized(PlayerController playerController)
    {
        if (playerController.isLocalPlayer)
        {
            localPlayerController = playerController;
            Initialize();
        }
    }

    public void Initialize()
    {
        List<StaticShipData> shipDatas = localPlayerController.GetShipDataList();

        for( int i = 0; i < (int) ShipType.Count; i++)
        {
            GameObject cellGameObject = GameObject.Instantiate(shipButtonPrefab, shipButtonParent);
            UIShipButton buttonObj = cellGameObject.GetComponent<UIShipButton>();
            shipButtons.Add(buttonObj);
            buttonObj.Initialize(this, shipDatas[i]);
        }
    }

    public void UpdateShipButtons(PlayerController player, Vector2Int coords, RuntimeShipData shipInstanceData, Orientation orientation)
    {
        if(player.isLocalPlayer)
        {
            for (int i = 0; i < (int)ShipType.Count; i++)
            {
                UIShipButton buttonObj = shipButtons[i];
                buttonObj.UpdateShipCount(localPlayerController.GetLocalGameState().shipManager.availableShipCounts[i]);
            }
        }
    }

    public void OnShipButtonClicked(ShipType type)
    {
        localPlayerController.OnShipTypeSelected(type);
    }
    public void OnSubmitButtonClicked()
    {
        localPlayerController.OnSubmitSignalReceived();
    }

}
