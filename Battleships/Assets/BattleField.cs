using Library;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleCell
{
    public float[] bottomLeftOrigin;
    public RuntimeShipData shipData;
    public bool wasHitOnce;
    public void Initialize(Vector3 origin)
    {
        bottomLeftOrigin = new float[3];
        bottomLeftOrigin[0] = origin.x;
        bottomLeftOrigin[1] = origin.y;
        bottomLeftOrigin[2] = origin.z;

        wasHitOnce = false;

        Reset();
    }
    public void Initialize(float[] origin)
    {
        bottomLeftOrigin = new float[3];
        bottomLeftOrigin[0] = origin[0];
        bottomLeftOrigin[1] = origin[1];
        bottomLeftOrigin[2] = origin[2];

        wasHitOnce = false;

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

    public Vector3 getBottomLeftOrigin()
    {
        Vector3 result = new Vector3 (bottomLeftOrigin[0], bottomLeftOrigin[1], bottomLeftOrigin[2]);
        return result;
    }

    public float[] getBottomLeftOriginRaw()
    {
        return bottomLeftOrigin;
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

    public BattleCell[] field;                 // to de synced
    public BattleField(BattleFieldSetup gameManagerSetup)
    {
        setup = gameManagerSetup;
        InitializeCells();
    }

    void InitializeCells()
    {
        field = new BattleCell[setup.horizCellsCount * setup.vertiCellsCount];

        for (int x= 0; x < setup.horizCellsCount; x++)
        {
            for (int y = 0; y < setup.vertiCellsCount; y++)
            {
                BattleCell cell = new BattleCell();

                Vector3 cellOrigin;
                cellOrigin.x = setup.originTransformBottomLeft.position.x + x * setup.cellSize;
                cellOrigin.z = setup.originTransformBottomLeft.position.z + y * setup.cellSize;
                cellOrigin.y = setup.originTransformBottomLeft.position.y;

                cell.Initialize(cellOrigin);

                field[x * setup.vertiCellsCount + y] = cell;
            }
        }
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
                if (IsInRange(coordX, coordY) && !field[coordX * setup.vertiCellsCount + coordY].IsFree())
                {
                    return false;
                }
            }
            else
            {
                if (!IsInRange(coordX, coordY) || !field[coordX * setup.vertiCellsCount + coordY].IsFree())
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

        areCellsFree = !IsInRange(cellCoords.x, cellCoords.y) || field[cellCoords.x * setup.vertiCellsCount + cellCoords.y].IsFree();

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

        areCellsFree = !IsInRange(cellCoords.x, cellCoords.y) || field[cellCoords.x * setup.vertiCellsCount + cellCoords.y].IsFree();
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

            ref BattleCell cell = ref field[cellCoords.x * setup.vertiCellsCount + cellCoords.y];
            cell.ConnectShip(runtimeShipData);
        }
    }

    public void ClearCell(int x, int y)
    {
        GetCell(x, y).Reset();
    }
    public BattleCell GetCell(int x, int y)
    {
        return field[x * setup.vertiCellsCount + y];
    }
    public int GetFlatCellIndex(int x, int y)
    {
        return x * setup.vertiCellsCount + y;
    }
    public int GetFieldSize()
    {
        return setup.vertiCellsCount * setup.horizCellsCount;
    }
}
