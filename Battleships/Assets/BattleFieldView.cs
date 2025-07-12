using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BattleFieldView : MonoBehaviour
{
    [Header("Manual Setup")]
    public BattleField flowField;
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

    void Start()
    {
        cellPrefab.transform.localScale = new Vector3(flowField.cellSize, flowField.cellSize, flowField.cellSize);

        Initialize();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void Initialize()
    {
        for (int i = 0; i < flowField.horizCellsCount; i++)
        {
            for (int j = 0; j < flowField.vertiCellsCount; j++)
            {
                GameObject cellGameObject = GameObject.Instantiate(cellPrefab, cellSpawnParent);

                CellObject newCellObject = cellGameObject.GetComponent<CellObject>();
                newCellObject.cellData.index.x = i;
                newCellObject.cellData.index.y = j;

                newCellObject.battleFieldView = this;

                cellGameObject.transform.position = flowField.field[i, j].bottomLeftOrigin;

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
        shipGameObject.transform.position = flowField.field[x, y].bottomLeftOrigin;

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

/*
    public void DestroyShipObject(ShipObject shipObject)
    {
        shipObjects.Remove(shipObject);
        Destroy(shipObject);
    }*/

/*
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!isDebugRenderEnabled || !flowField.isInitialized)
        {
            return;
        }

        RenderCells();
    }

    void RenderCells()
    {
        Gizmos.color = Color.magenta;
        Vector3 from, to, temp;

        for (int i = 0; i < flowField.horizCellsCount; i++)
        {

            for (int j = 0; j < flowField.vertiCellsCount; j++)
            {
                from = flowField.field[i, j].bottomLeftOrigin;
                from.y = flowField.originTransformBottomLeft.position.y;

                //from.x += flowField.cellSize * 0.5f;
                //from.z += flowField.cellSize * 0.5f;

                to = from;
                to += flowField.field[i, j].velocity;

                Gizmos.DrawLine(from, to);

                from = to;
                temp = Vector3.Cross(flowField.field[i, j].velocity, Vector3.up);
                to += temp * 0.2f;
                to -= flowField.field[i, j].velocity * 0.5f;

                Gizmos.DrawLine(from, to);

                to = from - temp * 0.2f;
                to -= flowField.field[i, j].velocity * 0.5f;

                Gizmos.DrawLine(from, to);
            }
        }
    }
#endif*/
}