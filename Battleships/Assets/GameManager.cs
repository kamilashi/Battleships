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

    public List<PlayerState> players = new List<PlayerState>();
    [SerializeField] private GamePhase currentGamePhase;
    [SerializeField] private int currentTurnCount;

    void Awake()
    {
        PlayerState.onPlayerSpawnedEvent.AddListener(RegisterPlayer);
        PlayerState.onPlayerDespawnedEvent.AddListener(DeregisterPlayer);
        currentTurnCount = 0;
    }

    void FixedUpdate()
    {
        if(players.Count != intendedPlayerCount)
        {
            return;
        }

        if (players[0].syncedState.submitSignalReceived && players[1].syncedState.submitSignalReceived) 
        {
            GamePhase oldGamePhase = currentGamePhase;

            foreach (PlayerState playerState in players)
            {
                playerState.DeserializeGameState();
            }

            ProcessTurn(players[0], players[1]); 

            if(currentGamePhase == GamePhase.Build)
            {
                currentGamePhase = GamePhase.Combat;
            }

            currentTurnCount++;

            foreach (PlayerState playerState in players)
            {
                playerState.syncedState.submitSignalReceived = false;
                playerState.syncedState.gamePhase = currentGamePhase;
                playerState.SerializeGameState();

                playerState.RpcSyncGameStateWClient(playerState.connectionToClient, playerState.syncedState);
                playerState.RpcOnTurnFinished(playerState.connectionToClient);

                if(oldGamePhase != currentGamePhase)
                {
                    playerState.RpcOnGamePhaseChanged(playerState.connectionToClient, oldGamePhase, currentGamePhase);
                }
            }
        }
    }

/*
    private void LogSubmissionSignal(int playerId)
    {
        playerControllers[playerId].syncedState.submitSignalReceived = true;
    }*/

    private void RegisterPlayer(PlayerState player)
    {
        if(players.Count == intendedPlayerCount)
        {
            Debug.LogError("Maximum connected player count reached!");
            return;
        }

        int playerIndex = players.Count;

        if(!player.isLocalPlayer)
        {
            player.Initialize();
        }

        player.RpcInitializeClient(player.connectionToClient);

        player.syncedState.playerId = playerIndex;
        players.Add(player);
    }
    private void DeregisterPlayer(PlayerState player)
    {
        players.Remove(player);
    }

    private void TryHitShip(int x, int y, PlayerState attackerPlayer, PlayerState targetPlayer)
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

            CellHitData otherHitData = new CellHitData(HitResult.Damaged, shipIndex, x, y, true, attackedCell.shipData.Size(), attackedCell.shipData.orientation);
            targetPlayer.RpcOnCellHit(targetPlayer.connectionToClient, otherHitData);

            targetGameState.battleField.ClearCell(x, y);
        }
        else
        {
            Debug.Log("Missed!");
        }

        attackedCell.wasHitOnce = true;

        CellHitData selfHitData = new CellHitData(hitResult, -1, x, y, false);
        attackerPlayer.RpcOnCellHit(attackerPlayer.connectionToClient, selfHitData);
    }

    public void ProcessTurn(PlayerState player1, PlayerState player2)
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

    public void ProcessCommand(PlayerCommand command, PlayerState owner, PlayerState opponent)
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
