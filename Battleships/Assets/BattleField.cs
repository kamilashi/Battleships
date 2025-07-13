using Library;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BattleCell
{
    public Vector3 bottomLeftOrigin;
    public RuntimeShipData shipData;
    public void Initialize(Vector3 origin)
    {
        bottomLeftOrigin.x = origin.x;
        bottomLeftOrigin.y = origin.y;
        bottomLeftOrigin.z = origin.z;

        shipData = null;

        Reset();
    }

    public bool IsFree()
    {
        return shipData == null;
    }
    public void ConnectShip(RuntimeShipData shipData)
    {
        this.shipData = shipData;
    }
    public void Reset()
    {
        shipData = null;
    }
}

[Serializable]
public class BattleFieldSetup
{
    public int horizCellsCount = 10;
    public int vertiCellsCount = 10;
    public float cellSize = 1.0f;

    public Transform originTransformBottomLeft;
}

public class BattleField
{
    public readonly BattleFieldSetup setup;
    public BattleCell[,] field;
    public bool isInitialized = false;

    public BattleField(BattleFieldSetup gameManagerSetup)
    {
        setup = gameManagerSetup;
        InitializeCells();
    }

    void InitializeCells()
    {
        field = new BattleCell[setup.horizCellsCount, setup.vertiCellsCount];

        for (int x= 0; x < setup.horizCellsCount; x++)
        {
            for (int y = 0; y < setup.vertiCellsCount; y++)
            {
                ref BattleCell cell = ref field[x, y];

                Vector3 cellOrigin;
                cellOrigin.x = setup.originTransformBottomLeft.position.x + x * setup.cellSize;
                cellOrigin.z = setup.originTransformBottomLeft.position.z + y * setup.cellSize;
                cellOrigin.y = setup.originTransformBottomLeft.position.y;

                cell.Initialize(cellOrigin);
            }
        }

        isInitialized = true;
    }

    private bool IsInRange(int x, int y)
    {
        return x >= 0 && y >= 0 && x < setup.horizCellsCount && y < setup.vertiCellsCount;
    }

    private bool IsPathFree(int x, int y, int length, Vector2Int direction, bool adjacentSpaceCheck)
    {
        int coordX;
        int coordY;

        for (int i = 0; i < length; i++)
        {
            coordX = x + i * direction.x;
            coordY = y + i * direction.y;

            if(adjacentSpaceCheck)
            {
                if (IsInRange(coordX, coordY) && !field[coordX, coordY].IsFree())
                {
                    return false;
                }
            }
            else
            {
                if (!IsInRange(coordX, coordY) || !field[coordX, coordY].IsFree())
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool CanPlaceShip(int x, int y, StaticShipData shipData, Orientation shipOrientation)
    {
        bool areCellsFree = true;
        Vector2Int orientation = StaticShipData.GetOrientation(shipOrientation);
        Vector2Int cellCoords = new Vector2Int(x, y);
        int shipSize = shipData.Size();

        if (!IsPathFree(x, y, shipSize, orientation, false))
        {
            return false;
        }

        cellCoords.x = x - orientation.x;
        cellCoords.y = y - orientation.y;

        areCellsFree = !IsInRange(cellCoords.x, cellCoords.y) || field[cellCoords.x, cellCoords.y].IsFree();

        Vector2Int localRight = Helpers.GetRightCWVector(orientation);
        cellCoords += localRight;

        if(!areCellsFree || !IsPathFree(cellCoords.x, cellCoords.y, shipSize + 2, orientation, true))
        {
            return false;
        }

        cellCoords -= 2 * localRight;

        if (!areCellsFree || !IsPathFree(cellCoords.x, cellCoords.y, shipSize + 2, orientation, true))
        {
            return false;
        }

        cellCoords.x = x + shipSize * orientation.x;
        cellCoords.y = y + shipSize * orientation.y;

        areCellsFree = !IsInRange(cellCoords.x, cellCoords.y) || field[cellCoords.x, cellCoords.y].IsFree();
        return areCellsFree;
    }

    public void PlaceShip(int x, int y, RuntimeShipData runtimeShipData, StaticShipData staticShipData, Orientation shipOrientation)
    {
        Vector2Int orientation = StaticShipData.GetOrientation(shipOrientation);
        Vector2Int cellCoords = new Vector2Int(x, y);

        for (int i = 0; i < staticShipData.Size(); i++)
        {
            cellCoords.x = x + i * orientation.x;
            cellCoords.y = y + i * orientation.y;

            ref BattleCell cell = ref field[cellCoords.x, cellCoords.y];
            cell.ConnectShip(runtimeShipData);
        }
    }

    public void ClearCell(int x, int y)
    {
        ref BattleCell cell = ref field[x, y];
        cell.Reset();
    }
}
