using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
public enum HitResult
{
    None,
    Damaged,
    Killed
}

[Serializable]
public class RuntimeShipData
{
    public ShipType type;
    public int health;
    public int instanceId;
    private Vector2Int originCoords; // might be obsolete
    private Orientation orientation; // might be obsolete

/*
    public void Initialize(int maxHealth, ShipType type, int x, int y, Orientation orientation)
    {
        health = maxHealth; 
        this.type = type;

        originCoords.x = x; 
        originCoords.y = y; 
        this.orientation = orientation;
    }*/

    public void Initialize(int maxHealth, ShipType type, int id)
    {
        health = maxHealth;
        this.type = type;
        instanceId = id;
    }

    public int Damage()
    {
        health -= 1;
        return health;
    }
    public int Index()
    {
        return (int)type;
    }
}

public struct StaticShipData
{
    // static data:
    public ShipType shipType;
    public GameObject shipPrefab;
    public int maxShipCount;

    public void Initialize(ShipType type, GameObject prefab, int maxCount)
    {
        shipType = type;
        shipPrefab = prefab; 
        maxShipCount = maxCount;
    }

    public int Index()
    {
        return (int) shipType;
    }
    public int Size()
    {
        return (int) shipType + 1;
    }
    public int MaxHealth()
    {
        return (int)shipType + 1;
    }
    static public Vector2Int GetOrientation(Orientation orientation)
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
    public List<StaticShipData> shipDatas;

    public List<int> availableShipCounts = new List<int>();
    public List<int> currentShipCounts = new List<int>();
    public List<RuntimeShipData> shipInstances = new List<RuntimeShipData>();

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
        StaticShipData singleUnit = new StaticShipData();
        singleUnit.Initialize(ShipType.SingleUnit, setupData.singleUnitPrefab, 4);

        StaticShipData doubleUnit = new StaticShipData();
        doubleUnit.Initialize(ShipType.DoubleUnit, setupData.doubleUnitPrefab, 3);

        StaticShipData tripleUnit = new StaticShipData();
        tripleUnit.Initialize(ShipType.TripleUnit, setupData.tripleUnitPrefab, 2);

        StaticShipData quadUnit = new StaticShipData();
        quadUnit.Initialize(ShipType.QuadrupleUnit, setupData.quadrupleUnitPrefab, 1);

        shipDatas = new List<StaticShipData>();
        shipDatas.Add(singleUnit);
        shipDatas.Add(doubleUnit);
        shipDatas.Add(tripleUnit);
        shipDatas.Add(quadUnit);
    }

    public RuntimeShipData CreateShip(ShipType type, int x, int y, Orientation orientation)
    {
        int index = (int)type;

        if(availableShipCounts[index] < 1)
        {
            Debug.Log("No available ships of type " + type.ToString());
            return null;
        }

        availableShipCounts[index]--;
        currentShipCounts[index]++;
        totalShipCount++;

        RuntimeShipData shipInstance = new RuntimeShipData();
        int shipInstanceIndex = shipInstances.Count;

        shipInstance.Initialize(shipDatas[index].MaxHealth(), type, shipInstanceIndex);
        shipInstances.Add(shipInstance);

        return shipInstance; //#TODO: Return Runtime ref instead!
    }

    void InitializeShipCountLists()
    {
        for (int i = 0; i < (int)ShipType.Count; i++)
        {
            availableShipCounts.Add(shipDatas[i].maxShipCount);
            currentShipCounts.Add(0);
        }
    }

    public HitResult HitShip(int shipInstanceIndex)
    {
        int newHealth = shipInstances[shipInstanceIndex].Damage();

        if (newHealth <= 0)
        {
            currentShipCounts[shipInstances[shipInstanceIndex].Index()]--;
            totalShipCount--;
            return HitResult.Killed;
        }

        return HitResult.Damaged;
    }
/*
    public HitResult HitShip(RuntimeShipData runtimeShipData)
    {
        int newHealth = runtimeShipData.Damage();

        if (newHealth <= 0)
        {
            currentShipCounts[runtimeShipData.Index()]--;
            totalShipCount--;
            return HitResult.Killed;
        }

        return HitResult.Damaged;
    }*/

    public int GetAvailableShipsOfType(ShipType type)
    {
        return availableShipCounts[(int)type];
    }

    public StaticShipData GetShipData(ShipType type)
    {
        return shipDatas[(int)type];
    }

}
