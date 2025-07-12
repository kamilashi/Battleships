using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public struct CellData
{
    public Vector2Int index;
}

public class CellObject : MonoBehaviour, IHoverable, IClickable
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
}
