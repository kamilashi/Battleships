using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Setup")]
    public GameManager gameManager;

    public GameObject shipButtonPrefab;
    public RectTransform shipButtonParent;

    void Start()
    {
        Initialize();
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
        for( int i = 0; i < (int) ShipType.Count; i++)
        {
            GameObject cellGameObject = GameObject.Instantiate(shipButtonPrefab, shipButtonParent);
            UIShipButton buttonObj = cellGameObject.GetComponent<UIShipButton>();
            buttonObj.Initialize(this, (ShipType) i);
        }
    }
}
