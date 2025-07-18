using kcp2k;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

public interface IVisualSpawner
{
    public void SpawnChild(GameObject child, Vector3 localPosition);
    public void SpawnChildWithGlobalPosition(GameObject child, Vector3 globalPosition);
}

[Serializable]
public struct CellHitData
{
    public int[] hitCellCoords;
    public HitResult hitResult;
    public int shipInstanceIndex;
    public bool sourceIsOpponent;

    public CellHitData(HitResult result, int shipId, int coordX, int coordY, bool sourceIsOpponent)
    {
        hitResult = result;
        hitCellCoords = new int[2];
        hitCellCoords[0] = coordX;
        hitCellCoords[1] = coordY;
        shipInstanceIndex = shipId;
        this.sourceIsOpponent = sourceIsOpponent;
    }
}

public class BattleFieldView : MonoBehaviour
{
    [Header("Manual Setup")]
    public Transform cellSpawnParent;
    public Transform shipSpawnParent;

    [Header("Visualization")]
    public GameObject cellPrefab;
    public GameObject damagedCellMarkerPrefab;
    public GameObject missedCellMarkerPrefab;

    [Header("DebugView")]
    public PlayerState localPlayerState;

    public List<CellObject> cellObjects;
    public List<ShipObject> shipObjects;
    public CellObject hoveredObject;
    public bool isDebugRenderEnabled = false;

    [Header("DebugView")]
    public Vector2Int testCoords;
    public bool testSourceIsOpponent;
    public HitResult testHitResult;

    public void Awake()
    {
        PlayerState.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
        PlayerState.onTurnFinished.AddListener(ProcessCellHits);
        PlayerState.onShipAdded.AddListener(OnShipAdded);
        PlayerState.onShipDestroyed.AddListener(OnShipDestroyed);
    }

    public void OnLocalPlayerInitialized(PlayerState localPlayer)
    {
        if(localPlayer.isLocalPlayer)
        {
            localPlayerState = localPlayer;
            Initialize();
        }
    }

    public void Initialize()
    {
        LocalGameState localgameState = localPlayerState.GetLocalGameState();
        cellPrefab.transform.localScale = new Vector3(localgameState.battleField.setup.cellSize, localgameState.battleField.setup.cellSize, localgameState.battleField.setup.cellSize);

        for (int i = 0; i < localgameState.battleField.setup.horizCellsCount; i++)
        {
            for (int j = 0; j < localgameState.battleField.setup.vertiCellsCount; j++)
            {
                GameObject cellGameObject = GameObject.Instantiate(cellPrefab, cellSpawnParent);

                CellObject newCellObject = cellGameObject.GetComponent<CellObject>();
                CellData cellData = new CellData(i, j);

                newCellObject.cellData = cellData;

                newCellObject.battleFieldView = this;

                cellGameObject.transform.position = localgameState.battleField.GetCell(i , j).getBottomLeftOrigin();

                cellObjects.Add(newCellObject);
            }
        }
    }

   public void OnCellObjectSelected(int x, int y)
    {
        localPlayerState.OnCellSelected(x,y);
    }

   public void VisualizeCellHit(CellHitData hitData)
    {
        LocalGameState localGameState = localPlayerState.GetLocalGameState();
        if (hitData.sourceIsOpponent)
        {
            ShipObject damagedShip = shipObjects[hitData.shipInstanceIndex];

            int flatIndex = localGameState.battleField.GetFlatCellIndex(hitData.hitCellCoords[0], hitData.hitCellCoords[1]);
            damagedShip.HighlightShipSegment(flatIndex);
        }
        else
        {
            BattleCell cell = localGameState.battleField.GetCell(hitData.hitCellCoords[0], hitData.hitCellCoords[1]);

            Vector3 position = cell.getBottomLeftOrigin();

            if(!cell.IsFree())
            {
                ShipObject occupiedObject = shipObjects[cell.shipData.instanceId];
                position.y += occupiedObject.objectHeight;
            }

            int cellObjectIndex = localGameState.battleField.GetFlatCellIndex(hitData.hitCellCoords[0], hitData.hitCellCoords[1]);
            cellObjects[cellObjectIndex].SpawnChildWithGlobalPosition(damagedCellMarkerPrefab, position);
        }
    }
    public void VisualizeCellKill(CellHitData hitData)
    {
        VisualizeCellHit(hitData);
    }

    public void VisualizeCellMiss(CellHitData hitData)
    {
        LocalGameState localGameState = localPlayerState.GetLocalGameState();
        BattleCell cell = localGameState.battleField.GetCell(hitData.hitCellCoords[0], hitData.hitCellCoords[1]);

        Vector3 position = cell.getBottomLeftOrigin();

        int cellObjectIndex = localGameState.battleField.GetFlatCellIndex(hitData.hitCellCoords[0], hitData.hitCellCoords[1]);
        cellObjects[cellObjectIndex].SpawnChildWithGlobalPosition(missedCellMarkerPrefab, position);
    }

    public void OnShipAdded(PlayerState player, Vector2Int coords, RuntimeShipData shipInstanceData, Orientation orientation)
    {
        if(player.isLocalPlayer)
        {
            StaticShipData shipData = localPlayerState.GetLocalGameState().shipManager.GetShipData(shipInstanceData.type);

            int shipObjectId = SpawnShipObject(coords.x, coords.y, shipData, shipInstanceData, orientation);
            Debug.Assert(shipObjectId == shipInstanceData.instanceId);
        }
    }
    public void OnShipDestroyed(PlayerState player, int shipIndex)
    {
        if (player.isLocalPlayer)
        {
            DestroyShipObject(shipIndex);
        }
    }

    public int SpawnShipObject(int x, int y, StaticShipData shipData, RuntimeShipData runtimeShipData, Orientation orientation)
    {
        GameObject shipGameObject = GameObject.Instantiate(shipData.shipPrefab, shipSpawnParent);
        shipGameObject.transform.position = localPlayerState.GetLocalGameState().battleField.GetCell(x, y).getBottomLeftOrigin();

        ShipObject shipObject = shipGameObject.GetComponent<ShipObject>();
        shipObject.Initialize(this, runtimeShipData, orientation, x, y);

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
    }

    public void ProcessCellHits()
    {
        List<CellHitData> hitQueue = localPlayerState.hitQueue;

        foreach (CellHitData hitData in hitQueue)
        {
            switch(hitData.hitResult)
            {
                case HitResult.Damaged:
                    VisualizeCellHit(hitData);
                    break;
                case HitResult.Killed:
                    Debug.Log("Here should be a visualizer for a killer hit!");
                    VisualizeCellKill(hitData);
                    //#TODO: if source is self, mark all fields around the ship as missed and mark the last damaged ship
                    break;
                case HitResult.None:
                    VisualizeCellMiss(hitData);
                    break;
            }
        }

        hitQueue.Clear();
    }


    public CellObject GetCellObject(int x, int y)
    {
        LocalGameState localgameState = localPlayerState.GetLocalGameState();
        int flatIndex = localgameState.battleField.GetFlatCellIndex(x, y);
        return cellObjects[flatIndex];
    }


    public void TestHightlight(int x, int y)
    {
        List<CellHitData> hitQueue = localPlayerState.hitQueue;

        int flatIndex = localPlayerState.GetLocalGameState().battleField.GetFlatCellIndex(x, y);
        CellHitData hitData = new CellHitData(testHitResult, 0, x, y, testSourceIsOpponent);

        hitQueue.Add(hitData);

        ProcessCellHits();
    }
}