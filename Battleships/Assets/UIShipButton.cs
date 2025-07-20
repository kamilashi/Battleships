using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIShipButton : UIBasicButton
{
    [Header("Setup in Prefab")]
    public TMP_Text shipCountText;


    [Header("Debug View")]
    public ShipType shipType;
    public UIController uIController;

    public void Initialize(UIController uIController, StaticShipData shipData)
    {
        this.uIController = uIController;
        this.shipType = shipData.type;
        buttonNameText.text = shipData.type.ToString();
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
