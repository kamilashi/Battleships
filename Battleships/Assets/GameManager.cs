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

public class GameState
{
    public BattleField battleField;
    public ShipManager shipManager;
}

public class GameManager : MonoBehaviour
{
    [Header("Battlefield Setup")]
    public BattleFieldSetup battlefieldSetup;

    [Header("Ship Manager Setup")]
    public ShipManagerSetupData shipManagerSetupData;

    [Header("Manual Setup")]
    public BattleFieldView battlefieldView;

    [Header("Debug View")]
    public GamePhase currentPhase;

    public ShipType selectedShipType;
    public Orientation shipOrientation;

    public GameState gameState;

    void Awake()
    {
        currentPhase = GamePhase.Build;
        gameState = new GameState();

        gameState.battleField = new BattleField(battlefieldSetup);
        gameState.shipManager = new ShipManager(shipManagerSetupData);

        battlefieldView.Initialize();
    }

    private void TryPlaceShip(int x, int y, ShipType type)
    {
        if(type == ShipType.Count)
        {
            Debug.Log("Choose a ship to place first.");
            return;
        }

        StaticShipData shipData = gameState.shipManager.GetShipData(type);
        if (!gameState.battleField.CanPlaceShip(x, y, shipData, shipOrientation))
        {
            Debug.Log("Cannot place ship here.");
            return;
        }

        RuntimeShipData shipInstanceData = gameState.shipManager.CreateShip(type, x, y, shipOrientation);

        if (shipInstanceData != null)
        {
            gameState.battleField.PlaceShip(x, y, shipInstanceData, shipData, shipOrientation);
            int shipObjectId = battlefieldView.SpawnShipObject(x, y, shipData, shipInstanceData, shipOrientation);
            Debug.Assert(shipObjectId == shipInstanceData.instanceId);
            EventManager.onShipAdded?.Invoke();
        }
    }
    private void TryHitShip(int x, int y)
    {
        if (!gameState.battleField.field[x, y].IsFree())
        {
            int shipIndex = gameState.battleField.field[x, y].shipData.instanceId;
            HitResult hitResult = gameState.shipManager.HitShip(shipIndex);

            if(hitResult == HitResult.Killed)
            {
                battlefieldView.DestroyShipObject(shipIndex);

                Debug.Log("Killed ship");
            }
            else
            {
                Debug.Log("Damaged ship");
            }

            gameState.battleField.ClearCell(x, y);
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
        return gameState.shipManager.shipDatas;
    }
}
