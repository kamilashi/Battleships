using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct CellData
{
    public Vector2Int index;

    public CellData(int x, int y)
    {
        index = new Vector2Int(x, y);
        index.x = x; 
        index.y = y;
    }
}

public class CellObject : MonoBehaviour, IHoverable, IClickable, IVisualSpawner
{
    [Header("Setup in Prefab")]
    public MeshRenderer meshRenderer;

    [Header("Auto Setup")]
    public CellData cellData;
    public BattleFieldView battleFieldView;

    private MaterialPropertyBlock mpb;

    public void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }
    public void OnClicked()
    {
        battleFieldView.OnCellObjectSelected(cellData.index.x, cellData.index.y);
    }

    public void OnStartHover()
    {
        SetMaterialProperty("_Highlighted", 1.0f);
    }

    public void OnStopHover()
    {
        SetMaterialProperty("_Highlighted", 0.0f);
    }

    public void SetMaterialProperty(string propertyName, float value)
    {
        meshRenderer.GetPropertyBlock(mpb); 
        mpb.SetFloat(propertyName, value);
        meshRenderer.SetPropertyBlock(mpb);
    }
    public void SpawnChild(GameObject child, Vector3 localPosition)
    {
        GameObject childObject = GameObject.Instantiate(child, this.transform);

        childObject.transform.rotation.eulerAngles.Set(0, 0, 0);
        childObject.transform.localPosition = localPosition;
    }
    public void SpawnChildWithGlobalPosition(GameObject child, Vector3 globalPosition)
    {
        GameObject childObject = GameObject.Instantiate(child, this.transform);

        Vector3 localPosition = transform.InverseTransformPoint(globalPosition);

        childObject.transform.rotation.eulerAngles.Set(0, 0, 0);
        childObject.transform.localPosition = localPosition;
    }
}
