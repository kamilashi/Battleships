using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIShipButton : MonoBehaviour
{
    [Header("Setup in Prefab")]
    public Button button;
    public TMP_Text buttonText;


    [Header("Debug View")]
    public ShipType shipType;
    public UIController uIController;

    public void Initialize(UIController uIController, ShipType shipType)
    {
        this.uIController = uIController;
        this.shipType = shipType;
        buttonText.text = shipType.ToString();

        //button.onClick.AddListener(OnButtonClick);
    }

    public void OnButtonClick()
    {
        uIController.OnShipButtonClicked(shipType);
    }
}
