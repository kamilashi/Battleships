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
    public Color mainColor;
    public Color hitColor;

    [Header("Auto Setup")]
    public CellData cellData;
    public BattleFieldView battleFieldView;

    [Header("Debug View")]
    public bool IsHighlightLocked;
    private MaterialPropertyBlock mpb;

    public void Awake()
    {
        mpb = new MaterialPropertyBlock();
        IsHighlightLocked = false;
    }
    public void OnClicked()
    {
        battleFieldView.OnCellObjectSelected(cellData.index.x, cellData.index.y);
    }

    public void OnStartHover()
    {
        if(!IsHighlightLocked)
        {
            SetMaterialProperty("_HighlightColor", mainColor);
            SetMaterialProperty("_Highlighted", 1.0f);
        }
    }

    public void OnStopHover()
    {
        if (!IsHighlightLocked)
        {
            SetMaterialProperty("_Highlighted", 0.0f);
        }
    }

    public void HighlightHitCell()
    {
        SetMaterialProperty("_HighlightColor", hitColor);
        SetMaterialProperty("_Highlighted", 1.0f);
        IsHighlightLocked = true;
    }
    public void HighlightExposedCell()
    {
        SetMaterialProperty("_MainColor", hitColor);
    }

    public void StopHighlightHitCell()
    {
        SetMaterialProperty("_Highlighted", 0.0f);
        IsHighlightLocked = false;
    }

    private void SetMaterialProperty(string propertyName, float value)
    {
        meshRenderer.GetPropertyBlock(mpb); 
        mpb.SetFloat(propertyName, value);
        meshRenderer.SetPropertyBlock(mpb);
    }
    private void SetMaterialProperty(string propertyName, Color color)
    {
        meshRenderer.GetPropertyBlock(mpb); 
        mpb.SetColor(propertyName, color);
        meshRenderer.SetPropertyBlock(mpb);
    }
    public void SpawnChild(GameObject child, Vector3 localPosition)
    {
        GameObject childObject = GameObject.Instantiate(child, this.transform);

        childObject.transform.localPosition = localPosition;
    }
    public void SpawnChildWithGlobalPosition(GameObject child, Vector3 globalPosition)
    {
        GameObject childObject = GameObject.Instantiate(child, this.transform);

        Vector3 localPosition = transform.InverseTransformPoint(globalPosition);

        childObject.transform.localPosition = localPosition;
    }
}
