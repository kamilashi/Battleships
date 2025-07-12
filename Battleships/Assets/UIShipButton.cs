using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIShipButton : MonoBehaviour
{
    [Header("Setup in Prefab")]
    public Button button;
    public TMP_Text buttonNameText;
    public TMP_Text shipCountText;


    [Header("Debug View")]
    public ShipType shipType;
    public UIController uIController;

    public void Initialize(UIController uIController, StaticShipData shipData)
    {
        this.uIController = uIController;
        this.shipType = shipData.shipType;
        buttonNameText.text = shipData.shipType.ToString();
        shipCountText.text = shipData.maxShipCount.ToString();

        button.onClick.AddListener(OnButtonClick);
    }

    public void UpdateShipCount(int count)
    {
        shipCountText.text = count.ToString();
    }

    public void OnButtonClick()
    {
        uIController.OnShipButtonClicked(shipType);
    }
}
