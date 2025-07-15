using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public struct CellHitData
{
    public int[] hitCellCoords;
    public HitResult hitResult;
    public int shipInstanceIndex;

    public CellHitData(HitResult result, int shipId, int coordX, int coordY)
    {
        hitResult = result;
        hitCellCoords = new int[2];
        hitCellCoords[0] = coordX;
        hitCellCoords[1] = coordY;
        shipInstanceIndex = shipId;
    }
}

public class BattleFieldView : MonoBehaviour
{
    [Header("Manual Setup")]
    public Transform cellSpawnParent;
    public Transform shipSpawnParent;

    [Header("Visualization")]
    public GameObject cellPrefab;
    public GameObject damagedCellPrefab;

    [Header("DebugView")]
    public PlayerController localPlayerController;

    public List<CellObject> cellObjects;
    public List<ShipObject> shipObjects;
    public CellObject hoveredObject;
    public bool isDebugRenderEnabled = false;

    public void Awake()
    {
        PlayerController.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
        PlayerController.onTurnFinished.AddListener(ProcessCellHits);
        PlayerController.onShipAdded.AddListener(OnShipAdded);
        PlayerController.onShipDestroyed.AddListener(OnShipDestroyed);
        //PlayerController.onCellHit.AddListener(OnCellHit);
    }

    public void OnLocalPlayerInitialized(PlayerController localPlayer)
    {
        if(localPlayer.isLocalPlayer)
        {
            localPlayerController = localPlayer;
            Initialize();
        }
    }

    public void Initialize()
    {
        GameState localgameState = localPlayerController.GetLocalGameState();
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

                cellGameObject.transform.position = localgameState.battleField.GetCell(i , j).getBottomLeftOrigin();

                cellObjects.Add(newCellObject);
            }
        }
    }

   public void OnCellObjectSelected(int x, int y)
    {
        localPlayerController.OnCellSelected(x,y);
    }

   public void VisualizeCellHit(CellHitData hitData)
    {
        ShipObject damagedShip =  shipObjects[hitData.shipInstanceIndex];
        BattleCell cell = localPlayerController.GetLocalGameState().battleField.GetCell(hitData.hitCellCoords[0], hitData.hitCellCoords[1]);

        Vector3 position = cell.getBottomLeftOrigin();
        position.y += damagedShip.objectHeight;

        damagedShip.SpawnChildWithGlobalPosition(damagedCellPrefab, position);
    }

    public void OnShipAdded(PlayerController player, Vector2Int coords, RuntimeShipData shipInstanceData, Orientation orientation)
    {
        if(player.isLocalPlayer)
        {
            StaticShipData shipData = localPlayerController.GetLocalGameState().shipManager.GetShipData(shipInstanceData.type);

            int shipObjectId = SpawnShipObject(coords.x, coords.y, shipData, shipInstanceData, orientation);
            Debug.Assert(shipObjectId == shipInstanceData.instanceId);
        }
    }
    public void OnShipDestroyed(PlayerController player, int shipIndex)
    {
        if (player.isLocalPlayer)
        {
            DestroyShipObject(shipIndex);
        }
    }

    public int SpawnShipObject(int x, int y, StaticShipData shipData, RuntimeShipData runtimeShipData, Orientation orientation)
    {
        GameObject shipGameObject = GameObject.Instantiate(shipData.shipPrefab, shipSpawnParent);
        shipGameObject.transform.position = localPlayerController.GetLocalGameState().battleField.GetCell(x, y).getBottomLeftOrigin();

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
    }

    /*
    public void OnCellHit(PlayerController player, CellHitData hitData)
    {
        if (player.isLocalPlayer)
        {
            AddHit(hitData);
        }
    }*/


    public void ProcessCellHits()
    {
        List<CellHitData> hitQueue = localPlayerController.hitQueue;

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