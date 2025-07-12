using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum ShipType
{
    SingleUnit,
    DoubleUnit,
    TripleUnit,
    QuadrupleUnit,

    Count
}
public enum Orientation
{
    Vertical,
    Horizontal
}

public struct ShipData
{
    // static data:
    public ShipType shipType;
    public GameObject shipPrefab;
    public int maxShipCount;

    // dynamic data:
    public int damageCount;
    public Orientation orientation;

    public void Initialize(ShipType type, GameObject prefab, int maxCount)
    {
        shipType = type;
        shipPrefab = prefab; 
        maxShipCount = maxCount;

        damageCount = 0;
        orientation = Orientation.Vertical;
    }

    public int Index()
    {
        return (int) shipType;
    }
    public int Size()
    {
        return (int) shipType + 1;
    }
    public Vector2Int GetOrientation()
    {
        return orientation == Orientation.Vertical ? new Vector2Int(0,1) : new Vector2Int(1, 0);
    }
}
[Serializable]
public struct ShipSetupData // #TODO: move to a scriptable object
{
    public GameObject singleUnitPrefab;
    public GameObject doubleUnitPrefab;
    public GameObject tripleUnitPrefab;
    public GameObject quadrupleUnitPrefab;
}

public class ShipManager : MonoBehaviour
{
    [Header("Manual Setup")]
    public GameManager gameManager;

    public ShipSetupData setupData;

    [Header("Debug View")]

    public int totalShipCount = 0;
    public List<ShipData> shipDatas;

    public List<int> availableShips = new List<int>();
    public List<int> currentShips = new List<int>();

    void Awake()
    {
        Initialize();
    }

    void Update()
    {
        
    }

    void Initialize()
    {
        CreateShipDatas();
        InitializeShipCountLists();
    }

    void CreateShipDatas()
    {
        ShipData singleUnit = new ShipData();
        singleUnit.Initialize(ShipType.SingleUnit, setupData.singleUnitPrefab, 4);

        ShipData doubleUnit = new ShipData();
        doubleUnit.Initialize(ShipType.DoubleUnit, setupData.doubleUnitPrefab, 3);

        ShipData tripleUnit = new ShipData();
        tripleUnit.Initialize(ShipType.TripleUnit, setupData.tripleUnitPrefab, 2);

        ShipData quadUnit = new ShipData();
        quadUnit.Initialize(ShipType.QuadrupleUnit, setupData.quadrupleUnitPrefab, 1);

        shipDatas = new List<ShipData>();
        shipDatas.Add(singleUnit);
        shipDatas.Add(doubleUnit);
        shipDatas.Add(tripleUnit);
        shipDatas.Add(quadUnit);
    }

    public bool CreateShip(ShipType type)
    {
        int index = (int)type;

        if(availableShips[index] < 1)
        {
            Debug.LogError("No available ships of type " + type.ToString());
            return false;
        }

        availableShips[index]--;
        currentShips[index]++;
        totalShipCount++;

        return true;
    }

    void InitializeShipCountLists()
    {
        for (int i = 0; i < (int)ShipType.Count; i++)
        {
            availableShips.Add(shipDatas[i].maxShipCount);
            currentShips.Add(0);
        }
    }

    public int GetAvailableShipsOfType(ShipType type)
    {
        return availableShips[(int)type];
    }

    public ShipData GetShipData(ShipType type)
    {
        return shipDatas[(int)type];
    }

}
