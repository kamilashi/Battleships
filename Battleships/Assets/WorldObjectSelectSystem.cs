using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public interface IHoverable
{
    public void OnStartHover();
    public void OnStopHover();
}
public interface IClickable
{
    public void OnClicked();
}

public enum Selectables
{
    None,
    Cells
}

public class WorldObjectSelectSystem : MonoBehaviour
{
    [Header("Setup")]
    public LayerMask cellLayer;
    public Camera mainCamera;

    [Header("Debug View")]
    public Selectables hoverMode;
    public LayerMask clickableLayerMask;

    public GameObject hoveredObject;
    public GameObject clickedObject;

    public int cellLayerIdx;

    private void Awake()
    { 
        cellLayerIdx = GetSingleLayerIndex(cellLayer);

        int combinedMask =
        (1 << cellLayerIdx);

        clickableLayerMask = combinedMask;
    }

    void Update()
    {
        ProcessHovering();
        ProcessSelection();
    }


    private void OnCellClick(GameObject interactiveGameObject)
    {
        CellObject interactiveObject = interactiveGameObject.GetComponent<CellObject>();
        interactiveObject.OnClicked();
    }
    private void OnCellHover(GameObject interactiveGameObject)
    {
        CellObject interactiveObject = interactiveGameObject.GetComponent<CellObject>();
        interactiveObject.OnStartHover();
    }
    private void OnCellsStopHover(GameObject interactiveGameObject)
    {
        CellObject interactiveObject = interactiveGameObject.GetComponent<CellObject>();
        interactiveObject.OnStopHover();
    }

    private bool ProcessSelection() // needs to be called after processHovering
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return false;
        }

        bool receivedSelection = false;
        
        switch (hoverMode)
        {
            case Selectables.None:
                break;
            case Selectables.Cells:
               {
                    OnCellClick(hoveredObject);
                    receivedSelection = true;
                    break;
                }
        }

        return receivedSelection;
    }

    private void ProcessHovering()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, float.MaxValue, clickableLayerMask))
        {
            if(hoveredObject != null && hit.transform.gameObject != hoveredObject)
            {
                ProcessStopHover(hoverMode);
            }

            int layerIndex = hit.transform.gameObject.layer;
            if (layerIndex == cellLayerIdx)
            {
                hoverMode = Selectables.Cells;
                OnCellHover(hit.transform.gameObject);
            }

            hoveredObject = hit.transform.gameObject;
        }
        else
        {
            if(hoveredObject!=null)
            {
                ProcessStopHover(hoverMode);
            }

            hoverMode = Selectables.None;
            hoveredObject = null;
        } 
    }

    private void ProcessStopHover(Selectables mode)
    {
        switch (mode)
        {
            case Selectables.Cells:
                {
                    OnCellsStopHover(hoveredObject);
                    break;
                }
            case Selectables.None:
                break;
        }
    }

    int GetSingleLayerIndex(LayerMask mask)
    {
        return Mathf.RoundToInt(Mathf.Log(mask.value, 2));
    }
}