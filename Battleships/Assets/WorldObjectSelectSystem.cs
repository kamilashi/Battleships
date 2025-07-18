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

    [Header("Auto Setup")]
    public PlayerState localPlayerState;

    [Header("Debug View")]
    public Selectables hoverMode;
    public LayerMask clickableLayerMask;

    public GameObject hoveredObject;
    public GameObject clickedObject;

    public int cellLayerIdx;

    [Header("Test")]
    public bool highlightOnClick;

    private void Awake()
    { 
        cellLayerIdx = GetSingleLayerIndex(cellLayer);

        int combinedMask =
        (1 << cellLayerIdx);

        clickableLayerMask = combinedMask;

        PlayerState.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
        PlayerState.onOrientationToggled.AddListener(OnOrientationToggled);
    }

    public void OnLocalPlayerInitialized(PlayerState localPlayer)
    {
        if (localPlayer.isLocalPlayer)
        {
            localPlayerState = localPlayer;
        }
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

        if(highlightOnClick)
        {
            interactiveObject.battleFieldView.TestHightlight(interactiveObject.cellData.index.x, interactiveObject.cellData.index.y);
        }
    }

    private List<Vector2Int> GetCells(CellObject originCellObject, ShipType shipType, Orientation orientation)
    {
        LocalGameState localGameState = localPlayerState.GetLocalGameState();

        int shipLength = ShipData.TypeToSize(shipType);
        Vector2Int direction = ShipData.GetOrientation(orientation);

        return localGameState.battleField.GetBlindPathInRange(originCellObject.cellData.index.x, originCellObject.cellData.index.y,
            shipLength, direction);
    }

    private void OnCellHover(GameObject interactiveGameObject, GamePhase gamePhase, Orientation orientation)
    {
        CellObject hoveredCell = interactiveGameObject.GetComponent<CellObject>();

        if (gamePhase == GamePhase.Build)
        {
            List<Vector2Int> cells = GetCells(hoveredCell, localPlayerState.selectedShipType, orientation);

            foreach (Vector2Int coords in cells)
            {
                CellObject cellObject = hoveredCell.battleFieldView.GetCellObject(coords.x, coords.y);
                cellObject.OnStartHover();
            }
        }
        else
        {
            hoveredCell.OnStartHover();
        }
    }
    private void OnCellStopHover(GameObject interactiveGameObject, GamePhase gamePhase, Orientation orientation)
    {
        CellObject hoveredCell = interactiveGameObject.GetComponent<CellObject>();

        if (gamePhase == GamePhase.Build)
        {
            List<Vector2Int> cells = GetCells(hoveredCell, localPlayerState.selectedShipType, orientation);

            foreach (Vector2Int coords in cells)
            {
                CellObject cellObject = hoveredCell.battleFieldView.GetCellObject(coords.x, coords.y);
                cellObject.OnStopHover();
            }
        }
        else
        {
            hoveredCell.OnStopHover();
        }
    }

    private void OnOrientationToggled(Orientation oldOn, Orientation newOn)
    {
        GamePhase gamePhase = localPlayerState.GetCurrentGamePhase();
        if(gamePhase == GamePhase.Build && hoverMode == Selectables.Cells)
        {
            OnCellStopHover(hoveredObject, gamePhase, oldOn);
            OnCellHover(hoveredObject, gamePhase, newOn);
        }
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
                GamePhase currentGamePhase = localPlayerState.GetCurrentGamePhase();
                OnCellHover(hit.transform.gameObject, currentGamePhase, localPlayerState.selectedShipOrientation);
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
                    GamePhase currentGamePhase = localPlayerState.GetCurrentGamePhase();
                    OnCellStopHover(hoveredObject, currentGamePhase, localPlayerState.selectedShipOrientation);
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