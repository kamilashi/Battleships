using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Setup")]
    public GameManager gameManager;

    public GameObject shipButtonPrefab;
    public RectTransform shipButtonParent;

    [Header("Setup")]
    public List<UIShipButton> shipButtons;

    void Start()
    {
        Initialize();
        EventManager.onShipAdded.AddListener(UpdateShipButtons);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnShipButtonClicked(ShipType type)
    {
        gameManager.OnShipTypeSelected(type);
    }

    public void Initialize()
    {
        List<StaticShipData> shipDatas = gameManager.GetShipDataList();

        for( int i = 0; i < (int) ShipType.Count; i++)
        {
            GameObject cellGameObject = GameObject.Instantiate(shipButtonPrefab, shipButtonParent);
            UIShipButton buttonObj = cellGameObject.GetComponent<UIShipButton>();
            shipButtons.Add(buttonObj);
            buttonObj.Initialize(this, shipDatas[i]);
        }
    }

    public void UpdateShipButtons()
    {
        for (int i = 0; i < (int)ShipType.Count; i++)
        {
            UIShipButton buttonObj = shipButtons[i];
            buttonObj.UpdateShipCount(gameManager.shipManager.availableShipCounts[i]);
        }
    }
}
