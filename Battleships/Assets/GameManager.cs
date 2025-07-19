using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Events;

public enum GamePhase
{
    Wait,
    Build,
    Combat,
    GameOver
}
public enum GameOverState
{
    None,
    Win,
    Lose,
    Tie
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


    [Header("Test")]
    public bool startIntoBuild;

    void Awake()
    {
        PlayerState.onPlayerSpawnedEvent.AddListener(RegisterPlayer);
        PlayerState.onPlayerDespawnedEvent.AddListener(DeregisterPlayer);
        PlayerState.onPlayerRestartEvent.AddListener(RegisterPlayer);

        currentGamePhase = GamePhase.Wait;

        currentTurnCount = 0;
    }

    void FixedUpdate()
    {
        if(players.Count != intendedPlayerCount)
        {
            return;
        }

        if (currentGamePhase == GamePhase.Wait || startIntoBuild)
        {
            SwitchToBuildPhase();
            startIntoBuild = false;
        }

        if (players[0].syncedState.submitSignalReceived && players[1].syncedState.submitSignalReceived) 
        {
            GamePhase oldGamePhase = currentGamePhase;

            if (currentGamePhase == GamePhase.Build)
            {
                currentGamePhase = GamePhase.Combat;
            }

            foreach (PlayerState playerState in players)
            {
                playerState.DeserializeGameState();
            }

            ProcessTurn(players[0], players[1]);

            ProcessGameOver(ref currentGamePhase, players[0], players[1]);

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

            if(currentGamePhase == GamePhase.GameOver)
            {
                ResetGameManager();
            }
        }
    }

    private void RegisterPlayer(PlayerState player)
    {
        if(players.Count == intendedPlayerCount)
        {
            Debug.LogError("Maximum connected player count reached!");
            return;
        }

        int playerIndex = players.Count;

        if (!player.isLocalPlayer)
        {
            player.Initialize();
        }

        player.RpcInitializeClient(player.connectionToClient);

        player.syncedState.playerId = playerIndex;
        players.Add(player);


        if (startIntoBuild)
        {
            SwitchToBuildPhase();
        }
    }

    private void ResetGameManager()
    {
        currentGamePhase = GamePhase.Wait;
        players.Clear();
        currentTurnCount = 0;
    }
    private void DeregisterPlayer(PlayerState player)
    {
        players.Remove(player);
    }

    private void SwitchToBuildPhase()
    {
        GamePhase previousGamePhase = currentGamePhase;
        currentGamePhase = GamePhase.Build;
        foreach (PlayerState playerState in players)
        {
            playerState.syncedState.gamePhase = currentGamePhase;
            playerState.RpcSyncGameStateWClient(playerState.connectionToClient, playerState.syncedState);
            playerState.RpcOnGamePhaseChanged(playerState.connectionToClient, previousGamePhase, currentGamePhase);
        }
    }

    private void TryHitShip(int x, int y, PlayerState attackerPlayer, PlayerState targetPlayer)
    {
        LocalGameState targetGameState = targetPlayer.GetLocalGameState();
        LocalGameState attackerGameState = attackerPlayer.GetLocalGameState();
        
        HitResult hitResult = HitResult.Miss;
        int shipIndex = -1;

        BattleCell attackedCell = targetGameState.battleField.GetCell( x, y);
        if (!attackedCell.IsFree())
        {
            shipIndex = attackedCell.shipData.instanceId;
            hitResult = targetGameState.shipManager.HitShip(shipIndex);

            if (hitResult == HitResult.Undefined)
            {
                return;
            }

            if (hitResult == HitResult.Killed)
            {
                targetPlayer.RpcOnShipDestroyed(targetPlayer.connectionToClient, shipIndex);
                Debug.Log("Killed ship");

                RuntimeShipData hitShip = attackedCell.shipData;

                List<Vector2Int> surroundingCells = attackerGameState.battleField.GetBlindAdjacentPathInRange(hitShip.origin[0], hitShip.origin[1], hitShip.Size(), ShipData.GetOrientation(hitShip.orientation));

                foreach (Vector2Int cell in surroundingCells)
                {
                    if(!attackerGameState.battleField.WasCellHitOnce(cell.x, cell.y))
                    {
                        CellHitData surroundedHitData = new CellHitData(HitResult.Miss, -1, cell.x, cell.y, false);
                        attackerPlayer.RpcOnCellHit(attackerPlayer.connectionToClient, surroundedHitData);

                        attackerGameState.battleField.MarkCellHitOnce(cell.x, cell.y);
                    }
                }
            }
            else
            {
                Debug.Log("Damaged ship");
            }

            targetGameState.battleField.ClearCell(x, y);

            CellHitData otherHitData = new CellHitData(hitResult, shipIndex, x, y, true);
            targetPlayer.RpcOnCellHit(targetPlayer.connectionToClient, otherHitData);
        }
        else
        {
            Debug.Log("Missed!");
        }

        attackerGameState.battleField.MarkCellHitOnce(x, y);

        CellHitData selfHitData = new CellHitData(hitResult, -1, x, y, false);
        attackerPlayer.RpcOnCellHit(attackerPlayer.connectionToClient, selfHitData);
    }

    private bool HasLosingCondition(PlayerState player)
    {
        LocalGameState localState = player.GetLocalGameState();
        return !localState.shipManager.HasShipsRemaining();
    }
    private void ProcessGameOver(ref GamePhase newGamePhase, PlayerState player1, PlayerState player2)
    {
        bool player1Lost = HasLosingCondition(player1);
        bool player2Lost = HasLosingCondition(player2);

        if (!player1Lost && !player2Lost) 
        {
            return;
        }

        newGamePhase = GamePhase.GameOver;

        if (player1Lost && player2Lost) 
        {
            player1.syncedState.gameOverState = GameOverState.Tie;
            player2.syncedState.gameOverState = GameOverState.Tie;
        }
        else
        {
            player1.syncedState.gameOverState = player1Lost ? GameOverState.Lose : GameOverState.Win;
            player2.syncedState.gameOverState = player2Lost ? GameOverState.Lose : GameOverState.Win;
        }
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
