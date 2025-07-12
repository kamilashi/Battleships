using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static UnityEvent onShipAdded = new UnityEvent();
}
public enum GamePhase
{
    Build,
    Combat
}
public class GameManager : MonoBehaviour
{
    [Header("Manual Setup")]
    public BattleFieldView battlefieldView;
    public BattleField battleField;
    public ShipManager shipManager;

    [Header("Debug View")]
    public GamePhase currentPhase;

    public ShipType selectedShipType;
    public Orientation shipOrientation;

    void Awake()
    {
        currentPhase = GamePhase.Build;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void TryPlaceShip(int x, int y, ShipType type)
    {
        if(type == ShipType.Count)
        {
            Debug.Log("Choose a ship to place first.");
            return;
        }

        StaticShipData shipData = shipManager.GetShipData(type);
        if (!battleField.CanPlaceShip(x, y, shipData, shipOrientation))
        {
            Debug.Log("Cannot place ship here.");
            return;
        }

        RuntimeShipData shipInstanceData = shipManager.CreateShip(type, x, y, shipOrientation);

        if (shipInstanceData != null)
        {
            battleField.PlaceShip(x, y, shipInstanceData, shipData, shipOrientation);
            int shipObjectId = battlefieldView.SpawnShipObject(x, y, shipData, shipInstanceData, shipOrientation);
            Debug.Assert(shipObjectId == shipInstanceData.instanceId);
            EventManager.onShipAdded?.Invoke();
        }
    }
    private void TryHitShip(int x, int y)
    {
        if (!battleField.field[x, y].IsFree())
        {
            int shipIndex = battleField.field[x, y].shipData.instanceId;
            //RuntimeShipData shipData = battleField.field[x, y].shipData;
            HitResult hitResult = shipManager.HitShip(shipIndex);

            if(hitResult == HitResult.Killed)
            {
                battlefieldView.DestroyShipObject(shipIndex);

                Debug.Log("Killed ship");
            }
            else
            {
                Debug.Log("Damaged ship");
            }

            battleField.ClearCell(x, y);
        }
    }

    public void OnCellSelected(int x, int y)
    {
        switch(currentPhase)
        {
            case GamePhase.Build:
                {
                    TryPlaceShip(x, y, selectedShipType);
                    break;
                }
            case GamePhase.Combat: //#TODO: SPLIT!
                {
                    TryHitShip(x,y);
                    break;
                }
        }
    }

    public void OnShipTypeSelected(ShipType selectedType)
    {
        selectedShipType = selectedType;
    }

    public List<StaticShipData> GetShipDataList()
    {
        return shipManager.shipDatas;
    }
}
