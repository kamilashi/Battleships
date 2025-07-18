using Mirror;
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
    Miss,
    Damaged,
    Killed,
    Undefined
}

[Serializable]
public abstract class ShipData
{
    public ShipType type;
    public int Index()
    {
        return (int)type;
    }
    public int Size()
    {
        return (int)type + 1;
    }
    public int MaxHealth()
    {
        return (int)type + 1;
    }
    static public Vector2Int GetOrientation(Orientation orientation) // #TODO: move out of here
    {
        return orientation == Orientation.Vertical ? new Vector2Int(0, 1) : new Vector2Int(1, 0);
    }

    static public int TypeToSize(ShipType type)
    {
        return (int)type + 1;
    }
}

[Serializable]
public class RuntimeShipData : ShipData
{
    public int health;
    public int instanceId;
    public Orientation orientation;
    public int[] origin;
    public void Initialize(int maxHealth, ShipType type, Orientation orientation, int id, int x, int y)
    {
        health = maxHealth;
        this.type = type;
        this.orientation = orientation;
        this.instanceId = id;
        origin = new int[2] { x, y };
    }

    public void Initialize(RuntimeShipData shipData)
    {
        health = shipData.health;
        this.type = shipData.type;
        this.orientation = shipData.orientation;
        this.instanceId = shipData.instanceId;
        origin = new int[2] { shipData.origin[0], shipData.origin[1] };
    }

    public int Damage()
    {
        health -= 1;
        return health;
    }

    public Vector2Int GetOrigin()
    {
        Vector2Int origin = new Vector2Int();
        origin.x = origin[0];
        origin.y = origin[1];
        return origin;
    }
}

public class StaticShipData : ShipData
{
    public GameObject shipPrefab;
    public int maxShipCount;

    public void Initialize(ShipType type, GameObject prefab, int maxCount)
    {
        this.type = type;
        shipPrefab = prefab; 
        maxShipCount = maxCount;
    }
}


[Serializable]
public class ShipManagerSetupData // #TODO: move to a scriptable object
{
    public GameObject singleUnitPrefab;
    public GameObject doubleUnitPrefab;
    public GameObject tripleUnitPrefab;
    public GameObject quadrupleUnitPrefab;
}

[Serializable]
public class ShipManager
{
    public ShipManagerSetupData setupData;

    public List<StaticShipData> shipDatas;

    // to de synced
    public List<int> availableShipCounts = new List<int>();
    public List<int> currentShipCounts = new List<int>();
    public List<RuntimeShipData> shipInstances = new List<RuntimeShipData>();
    public int totalShipCount = 0;

    public ShipManager(ShipManagerSetupData shipManagerSetupData)
    {
        setupData = shipManagerSetupData;
        Initialize();
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
            PlayerState.onMessageLogged?.Invoke("Out of ships for the selected type!");
            return null;
        }

        availableShipCounts[index]--;
        currentShipCounts[index]++;
        totalShipCount++;

        RuntimeShipData shipInstance = new RuntimeShipData();
        int shipInstanceIndex = shipInstances.Count;

        shipInstance.Initialize(shipDatas[index].MaxHealth(), type, orientation, shipInstanceIndex, x, y);
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
        if(shipInstanceIndex >= shipInstances.Count)
        {
            Debug.Log("Ship instance index was out of range! " + shipInstanceIndex);
            return HitResult.Undefined;
        }

        int newHealth = shipInstances[shipInstanceIndex].Damage();

        if (newHealth <= 0)
        {
            currentShipCounts[shipInstances[shipInstanceIndex].Index()]--;
            totalShipCount--;
            return HitResult.Killed;
        }

        return HitResult.Damaged;
    }

    public int GetAvailableShipsOfType(ShipType type)
    {
        return availableShipCounts[(int)type];
    }

    public StaticShipData GetShipData(ShipType type)
    {
        return shipDatas[(int)type];
    }

    public bool HasAvailableShipsRemaining()
    {
        return availableShipCounts.Exists(x => x > 0);
    }

    public bool HasShipsRemaining()
    {
        return totalShipCount > 0;
    }
}
