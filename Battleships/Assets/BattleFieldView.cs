using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
public struct CellHitData
{
    public BattleCell hitCell;
    public HitResult hitResult;
    public int shipInstanceIndex;

    public CellHitData(HitResult result, BattleCell cell, int shipId)
    {
        hitResult = result;
        hitCell = cell;
        shipInstanceIndex = shipId;
    }
}

public class BattleFieldView : MonoBehaviour
{
    [Header("Manual Setup")]
    public GameManager gameManager;

    public Transform cellSpawnParent;
    public Transform shipSpawnParent;

    [Header("Visualization")]
    public GameObject cellPrefab;
    public GameObject damagedCellPrefab;

    [Header("DebugView")]
    public List<CellObject> cellObjects;
    public List<ShipObject> shipObjects;
    public CellObject hoveredObject;
    public bool isDebugRenderEnabled = false;

    public List<CellHitData> hitQueue = new List<CellHitData>();

    public void Initialize()
    {
        GameState localgameState = gameManager.GetLocalGameState();
        cellPrefab.transform.localScale = new Vector3(localgameState.battleField.setup.cellSize, localgameState.battleField.setup.cellSize, localgameState.battleField.setup.cellSize);

        for (int i = 0; i < localgameState.battleField.setup.horizCellsCount; i++)
        {
            for (int j = 0; j < localgameState.battleField.setup.vertiCellsCount; j++)
            {
                GameObject cellGameObject = GameObject.Instantiate(cellPrefab, cellSpawnParent);

                CellObject newCellObject = cellGameObject.GetComponent<CellObject>();
                newCellObject.cellData.index.x = i;
                newCellObject.cellData.index.y = j;

                newCellObject.battleFieldView = this;

                cellGameObject.transform.position = localgameState.battleField.field[i, j].bottomLeftOrigin;

                cellObjects.Add(newCellObject);
            }
        }
    }

   public void OnCellObjectSelected(int x, int y)
    {
        gameManager.OnCellSelected(x,y);
    }

   public void VisualizeCellHit(CellHitData hitData)
    {
        ShipObject damagedShip =  shipObjects[hitData.shipInstanceIndex];

        Vector3 position = hitData.hitCell.bottomLeftOrigin;
        position.y += damagedShip.objectHeight;

        damagedShip.SpawnChildWithGlobalPosition(damagedCellPrefab, position);
    }

    public int SpawnShipObject(int x, int y, StaticShipData shipData, RuntimeShipData runtimeShipData, Orientation orientation)
    {
        GameObject shipGameObject = GameObject.Instantiate(shipData.shipPrefab, shipSpawnParent);
        shipGameObject.transform.position = gameManager.GetLocalGameState().battleField.field[x, y].bottomLeftOrigin;

        ShipObject shipObject = shipGameObject.GetComponent<ShipObject>();
        shipObject.Initialize(runtimeShipData);

        if (orientation == Orientation.Horizontal)
        {
            shipGameObject.transform.localRotation.eulerAngles.Set(0,90,0);
        }

        int shipObjectIndex = shipObjects.Count;
        shipObjects.Add(shipObject);

        return shipObjectIndex;
    }

    public void DestroyShipObject(int index)
    {
        GameObject shipGameObject = shipObjects[index].gameObject;
        Destroy(shipGameObject);
        //shipObjects.RemoveAt(index);
    }

    public void AddHit(CellHitData hitData)
    {
        hitQueue.Add(hitData);
    }

    public void ProcessCellHits()
    {
        foreach (CellHitData hitData in hitQueue)
        {
            switch(hitData.hitResult)
            {
                case HitResult.Damaged:
                    VisualizeCellHit(hitData);
                    break;
                case HitResult.Killed:
                    Debug.Log("Here should be a visualizer for a killer hit!");
                    break;
                case HitResult.None:
                    Debug.Log("Here should be a visualized miss!");
                    break;
            }
        }

        hitQueue.Clear();
    }
}