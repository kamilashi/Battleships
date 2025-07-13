using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
public enum PlayerCommandType
{
    FireHit,
    Count
}

public class GameState
{
    public BattleField battleField;
    public ShipManager shipManager;
    public PlayerController playerController;
    public List<PlayerCommand> commands = new List<PlayerCommand>();

    public bool submitSignalReceived = false;
}

public struct PlayerCommand
{
    public PlayerCommandType type;
    public int coordX;
    public int coordY;

    public PlayerCommand(PlayerCommandType commandType, int x, int y)
    {
        type = commandType;
        coordX = x;
        coordY = y;
    }
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

    private GameState gameState1;
    private GameState gameState2;

    void Awake()
    {
        currentPhase = GamePhase.Build;

        gameState1 = new GameState();
        gameState1.battleField = new BattleField(battlefieldSetup);
        gameState1.shipManager = new ShipManager(shipManagerSetupData);
        gameState1.playerController = new PlayerController();

        gameState1.commands = new List<PlayerCommand>();

        battlefieldView.Initialize();


        // initialize gameState2 to either be AI or second player 
        gameState2 = new GameState();
    }

    void FixedUpdate()
    {
        if (gameState1.submitSignalReceived /*&& gameState2.submitSignalReceived*/) 
        {
            ProcessTurn(gameState1, gameState1); //#TODO: pass 2nd gamestate here

            gameState1.submitSignalReceived = false;
            gameState2.submitSignalReceived = false;

            if(currentPhase == GamePhase.Build)
            {
                currentPhase = GamePhase.Combat;
            }
        }
    }

    private void TryPlaceShip(int x, int y, ShipType type)
    {
        GameState localGameState = GetLocalGameState();
        if(type == ShipType.Count)
        {
            Debug.Log("Choose a ship to place first.");
            return;
        }

        StaticShipData shipData = localGameState.shipManager.GetShipData(type);
        if (!localGameState.battleField.CanPlaceShip(x, y, shipData, shipOrientation))
        {
            Debug.Log("Cannot place ship here.");
            return;
        }

        RuntimeShipData shipInstanceData = localGameState.shipManager.CreateShip(type, x, y, shipOrientation);

        if (shipInstanceData != null)
        {
            localGameState.battleField.PlaceShip(x, y, shipInstanceData, shipData, shipOrientation);
            int shipObjectId = battlefieldView.SpawnShipObject(x, y, shipData, shipInstanceData, shipOrientation);
            Debug.Assert(shipObjectId == shipInstanceData.instanceId);
            EventManager.onShipAdded?.Invoke();
        }
    }
    private void TryHitShip(int x, int y, GameState targetgameState)
    {
        if (!targetgameState.battleField.field[x, y].IsFree())
        {
            int shipIndex = targetgameState.battleField.field[x, y].shipData.instanceId;
            HitResult hitResult = targetgameState.shipManager.HitShip(shipIndex);

            if(hitResult == HitResult.Killed)
            {
                battlefieldView.DestroyShipObject(shipIndex);

                Debug.Log("Killed ship");
            }
            else
            {
                Debug.Log("Damaged ship");
            }

            targetgameState.battleField.ClearCell(x, y);
        }
    }

    public void ProcessTurn(GameState player1, GameState player2)
    {
        foreach (PlayerCommand command in player1.commands)
        {
            ProcessCommand(command, player1, player2);
        }

        foreach (PlayerCommand command in player2.commands)
        {
            ProcessCommand(command, player2, player1);
        }

        // checkForWinner(player1, player2);
    }

    public void ProcessCommand(PlayerCommand command, GameState owner, GameState opponent)
    {
        switch (command.type)
        {
            case PlayerCommandType.FireHit:
                {
                    TryHitShip(command.coordX, command.coordY, opponent);
                    break;
                }
            case PlayerCommandType.Count:
                {
                    Debug.Log("Invalid command type!");
                    break;
                }
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
            case GamePhase.Combat:
                {
                    GameState localGameState = GetLocalGameState();
                    localGameState.playerController.StoreHit(x,y);
                    break;
                }
        }
    }

    public void OnShipTypeSelected(ShipType selectedType)
    {
        selectedShipType = selectedType;
    }
    public void OnSubmitSignalReceived()
    {
        GameState localGameState = GetLocalGameState();

        switch(currentPhase)
        {
            case GamePhase.Build :
                {
                    if (localGameState.shipManager.availableShipCounts.Exists(x => x > 0))
                    {
                        Debug.Log("You need to place all ships to progress!");
                        return;
                    }

                    break;
                }
            case GamePhase.Combat :
                {
                    if(!localGameState.playerController.HasHitStored())
                    {
                        Debug.Log("You need to select a cell to hit!");
                        return;
                    }

                    Vector2Int hitCoords = localGameState.playerController.hitCoords;
                    PlayerCommand hitCommand = new PlayerCommand(PlayerCommandType.FireHit, hitCoords.x, hitCoords.y);
                    localGameState.commands.Add(hitCommand);
                    localGameState.playerController.ClearHit();
                    break;
                }
        }

        localGameState.submitSignalReceived = true;
    }

    public List<StaticShipData> GetShipDataList()
    {
        return GetLocalGameState().shipManager.shipDatas;
    }

    public GameState GetLocalGameState()
    {
        return gameState1; //#TODO: do some kind of check here to determine which one is the local state or is gameState1 always local?
    }
}
