using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Events;

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

public class LocalGameState
{
    public BattleField battleField;
    public ShipManager shipManager;

    public LocalGameState(BattleFieldSetup battlefieldSetup, ShipManagerSetupData shipManagerSetup)
    {
        battleField = new BattleField(battlefieldSetup);
        shipManager = new ShipManager(shipManagerSetup);
    }
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

public class GameManager : NetworkBehaviour
{
    [Header("Manual Setup")]
    public const int intendedPlayerCount = 2;

    [Header("Debug View")]

    public List<PlayerController> playerControllers = new List<PlayerController>();
    [SerializeField] private GamePhase currentGamePhase;
    [SerializeField] private int currentTurnCount;

    void Awake()
    {
        PlayerController.onPlayerSpawnedEvent.AddListener(RegisterPlayer);
        PlayerController.onPlayerDespawnedEvent.AddListener(DeregisterPlayer);
        currentTurnCount = 0;
    }

    void FixedUpdate()
    {
        if(playerControllers.Count != intendedPlayerCount)
        {
            return;
        }

        if (playerControllers[0].syncedState.submitSignalReceived && playerControllers[1].syncedState.submitSignalReceived) 
        {
            foreach (PlayerController playerController in playerControllers)
            {
                playerController.DeserializeGameState();
            }

            ProcessTurn(playerControllers[0], playerControllers[1]); 

            if(currentGamePhase == GamePhase.Build)
            {
                currentGamePhase = GamePhase.Combat;
            }

            currentTurnCount++;

            foreach (PlayerController playerController in playerControllers)
            {
                playerController.syncedState.submitSignalReceived = false;
                playerController.syncedState.gamePhase = currentGamePhase;
                playerController.SerializeGameState();

                playerController.RpcSyncGameStateWClient(playerController.connectionToClient, playerController.syncedState);
                playerController.RpcOnTurnFinished(playerController.connectionToClient);
            }
        }
    }

/*
    private void LogSubmissionSignal(int playerId)
    {
        playerControllers[playerId].syncedState.submitSignalReceived = true;
    }*/

    private void RegisterPlayer(PlayerController player)
    {
        if(playerControllers.Count == intendedPlayerCount)
        {
            Debug.LogError("Maximum connected player count reached!");
            return;
        }

        int playerIndex = playerControllers.Count;

        if(!player.isLocalPlayer)
        {
            player.Initialize();
        }

        player.RpcInitializeClient(player.connectionToClient);

        player.syncedState.playerId = playerIndex;
        playerControllers.Add(player);
    }
    private void DeregisterPlayer(PlayerController player)
    {
        playerControllers.Remove(player);
    }

    private void TryHitShip(int x, int y, PlayerController attackerPlayer, PlayerController targetPlayer)
    {
        LocalGameState targetGameState = targetPlayer.GetLocalGameState();
        
        HitResult hitResult = HitResult.None;
        int shipIndex = -1;

        BattleCell attackedCell = targetGameState.battleField.GetCell( x, y);
        if (!attackedCell.IsFree())
        {
            shipIndex = attackedCell.shipData.instanceId;
            hitResult = targetGameState.shipManager.HitShip(shipIndex);

            if(hitResult == HitResult.Killed)
            {
                targetPlayer.RpcOnShipDestroyed(targetPlayer.connectionToClient, shipIndex);
                Debug.Log("Killed ship");
            }
            else
            {
                Debug.Log("Damaged ship");
            }

            CellHitData otherHitData = new CellHitData(HitResult.Damaged, shipIndex, x, y, true);
            targetPlayer.RpcOnCellHit(targetPlayer.connectionToClient, otherHitData);

            targetGameState.battleField.ClearCell(x, y);
        }
        else
        {
            Debug.Log("Missed!");
        }

        attackedCell.wasHitOnce = true;

        CellHitData selfHitData = new CellHitData(hitResult, shipIndex, x, y, false);
        attackerPlayer.RpcOnCellHit(attackerPlayer.connectionToClient, selfHitData);
    }

    public void ProcessTurn(PlayerController player1, PlayerController player2)
    {
        foreach (PlayerCommand command in player1.syncedState.commands)
        {
            ProcessCommand(command, player1, player2);
        }

        player1.syncedState.commands.Clear();

        foreach (PlayerCommand command in player2.syncedState.commands)
        {
            ProcessCommand(command, player2, player1);
        }

        player2.syncedState.commands.Clear();
        // checkForWinner(player1, player2);
    }

    public void ProcessCommand(PlayerCommand command, PlayerController owner, PlayerController opponent)
    {
        switch (command.type)
        {
            case PlayerCommandType.FireHit:
                {
                    TryHitShip(command.coordX, command.coordY, owner, opponent);
                    break;
                }
            case PlayerCommandType.Count:
                {
                    Debug.Log("Invalid command type!");
                    break;
                }
        }
    }
}
