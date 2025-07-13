using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BattleFieldView : MonoBehaviour
{
    [Header("Manual Setup")]
    public GameManager gameManager;

    public Transform cellSpawnParent;
    public Transform shipSpawnParent;

    [Header("Visualization")]
    public GameObject cellPrefab;

    [Header("DebugView")]
    public List<CellObject> cellObjects;
    public List<ShipObject> shipObjects;
    public CellObject hoveredObject;
    public bool isDebugRenderEnabled = false;

    public void Initialize()
    {
        cellPrefab.transform.localScale = new Vector3(gameManager.gameState.battleField.setup.cellSize, gameManager.gameState.battleField.setup.cellSize, gameManager.gameState.battleField.setup.cellSize);

        for (int i = 0; i < gameManager.gameState.battleField.setup.horizCellsCount; i++)
        {
            for (int j = 0; j < gameManager.gameState.battleField.setup.vertiCellsCount; j++)
            {
                GameObject cellGameObject = GameObject.Instantiate(cellPrefab, cellSpawnParent);

                CellObject newCellObject = cellGameObject.GetComponent<CellObject>();
                newCellObject.cellData.index.x = i;
                newCellObject.cellData.index.y = j;

                newCellObject.battleFieldView = this;

                cellGameObject.transform.position = gameManager.gameState.battleField.field[i, j].bottomLeftOrigin;

                cellObjects.Add(newCellObject);
            }
        }
    }

   public void OnCellObjectSelected(int x, int y)
    {
        gameManager.OnCellSelected(x,y);
    }

    public int SpawnShipObject(int x, int y, StaticShipData shipData, RuntimeShipData runtimeShipData, Orientation orientation)
    {
        GameObject shipGameObject = GameObject.Instantiate(shipData.shipPrefab, shipSpawnParent);
        shipGameObject.transform.position = gameManager.gameState.battleField.field[x, y].bottomLeftOrigin;

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
}