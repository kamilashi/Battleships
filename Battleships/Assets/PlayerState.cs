using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct SyncedGameState
{
    public BattleCell[] field;
    public int[] currentShipCounts;
    public RuntimeShipData[] shipInstances;
    public int totalShipCount;

    public int playerId;
    public int[] hitCoords;

    public List<PlayerCommand> commands;

    public bool submitSignalReceived;

    public GamePhase gamePhase;
    public GameOverState gameOverState;
}

public class PlayerState : NetworkBehaviour
{
    // player -> server
    public static UnityEvent<PlayerState> onPlayerSpawnedEvent = new UnityEvent<PlayerState>();
    public static UnityEvent<PlayerState> onPlayerDespawnedEvent = new UnityEvent<PlayerState>();
    public static UnityEvent<PlayerState> onPlayerRestartEvent = new UnityEvent<PlayerState>();

    // server -> player
    public static UnityEvent onTurnFinished = new UnityEvent();

    // player/server -> locals
    public static UnityEvent<PlayerState> onLocalPlayerInitializedEvent = new UnityEvent<PlayerState>();
    public static UnityEvent<PlayerState, Vector2Int, RuntimeShipData, Orientation> onShipAdded = new UnityEvent<PlayerState, Vector2Int, RuntimeShipData, Orientation>();
    public static UnityEvent<PlayerState, int> onShipDestroyed = new UnityEvent<PlayerState, int>();
    public static UnityEvent<Orientation, Orientation> onOrientationToggled = new UnityEvent<Orientation, Orientation>();
    public static UnityEvent<GamePhase, GamePhase> onGamePhaseChanged = new UnityEvent<GamePhase, GamePhase>();
    public static UnityEvent<int, int> onHitStored = new UnityEvent<int, int>();
    public static UnityEvent<string> onMessageLogged = new UnityEvent<string>();

    private LocalGameState localGameState;

    [SyncVar] public SyncedGameState syncedState;

    public List<CellHitData> hitQueue = new List<CellHitData>();
    public ShipType selectedShipType;
    public Orientation selectedShipOrientation;

    public override void OnStartClient()
    {
        base.OnStartServer();

        if(isLocalPlayer)
        {
            CmdOnClientSpawned();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if(isLocalPlayer)
        {
            CmdOnClientDespawned();
        }
    }
    public void Initialize()
    {
        syncedState.hitCoords = new int[2];
        syncedState.commands = new List<PlayerCommand>();

        GlobalSetup setup = GlobalSetup.Instance();
        localGameState = new LocalGameState(setup.battlefieldSetup, setup.shipManagerSetup);

        onLocalPlayerInitializedEvent?.Invoke(this);
    }
    private void TryPlaceShip(int x, int y, ShipType type)
    {
        Orientation selectedOrientation = selectedShipOrientation;
        if (type == ShipType.Count)
        {
            //Debug.Log("Choose a ship to place first.");
            onMessageLogged?.Invoke("Choose a ship to place first");
            return;
        }

        StaticShipData shipData = localGameState.shipManager.GetShipData(type);
        if (!localGameState.battleField.CanPlaceShip(x, y, shipData, selectedOrientation))
        {
            //Debug.Log("Cannot place ship here.");
            onMessageLogged?.Invoke("Cannot place ship here");
            return;
        }

        RuntimeShipData shipInstanceData = localGameState.shipManager.CreateShip(type, x, y, selectedOrientation);

        if (shipInstanceData != null)
        {
            localGameState.battleField.PlaceShip(x, y, shipInstanceData, shipData, selectedOrientation);
            onShipAdded?.Invoke(this, new Vector2Int(x, y), shipInstanceData, selectedOrientation);
        }
    }
    public void AddHit(CellHitData hitData)
    {
        hitQueue.Add(hitData);
    }
    public void OnShipTypeSelected(ShipType selectedType)
    {
        selectedShipType = selectedType;
    }
    public void OnShipOrientationToggled()
    {
        Orientation old = selectedShipOrientation;
        selectedShipOrientation = old == Orientation.Vertical ? Orientation.Horizontal : Orientation.Vertical;
        PlayerState.onOrientationToggled?.Invoke(old, selectedShipOrientation);
    }

    public void OnSubmitSignalReceived()
    {
        GamePhase currentPhase = syncedState.gamePhase;

        switch (currentPhase)
        {
            case GamePhase.Build:
                {
                    /*if (gameState.shipManager.HasAvailableShipsRemaining())
                    {
                        Debug.Log("You need to place all ships to progress!");
                        return;
                    }*/
                    break;
                }
            case GamePhase.Combat:
                {
                    if (!HasHitInputStored())
                    {
                        onMessageLogged?.Invoke("You need to select a cell to hit!");
                        return;
                    }

                    PlayerCommand hitCommand = new PlayerCommand(PlayerCommandType.FireHit, syncedState.hitCoords[0], syncedState.hitCoords[1]);

                    syncedState.commands.Add(hitCommand);
                    ClearHitInput();
                    break;
                }
        }

        syncedState.submitSignalReceived = true;
        SerializeGameState();
        CmdSyncGameStateWServer(this.syncedState);
    }
    public void TryStoreHitInput(int x, int y)
    {
        if(!localGameState.battleField.WasCellHitOnce(x, y))
        {
            syncedState.hitCoords[0] = x;
            syncedState.hitCoords[1] = y;

            onHitStored?.Invoke(x, y);
        }
        else
        {
            onMessageLogged?.Invoke("You cannot hit the same cell twice!");
        }
    }
    public void ClearHitInput()
    {
        syncedState.hitCoords[0] = -1;
        syncedState.hitCoords[1] = -1;
    }
    public bool HasHitInputStored()
    {
        return syncedState.hitCoords[0] >= 0 && syncedState.hitCoords[1] >= 0;
    }

    public void OnCellSelected(int x, int y)
    {
        switch (syncedState.gamePhase)
        {
            case GamePhase.Build:
                {
                    TryPlaceShip(x, y, selectedShipType);
                    break;
                }
            case GamePhase.Combat:
                {
                    TryStoreHitInput(x, y);
                    break;
                }
        }
    }

    public List<StaticShipData> GetShipDataList()
    {
        return localGameState.shipManager.shipDatas;
    }

    public LocalGameState GetLocalGameState()
    {
        return localGameState;
    }
    public SyncedGameState GetSyncedGameState()
    {
        return syncedState;
    }
    public GamePhase GetCurrentGamePhase()
    {
        return syncedState.gamePhase;
    }

    [Command]
    private void CmdOnClientSpawned()
    {
        onPlayerSpawnedEvent?.Invoke(this);
    }

    [Command]
    private void CmdOnClientDespawned()
    {
        onPlayerDespawnedEvent?.Invoke(this);
    }

    [Command]
    public void CmdRequestRestart()
    {
        onPlayerRestartEvent?.Invoke(this);
    }

    [TargetRpc]
    public void RpcInitializeClient(NetworkConnectionToClient conn)
    {
        Initialize();
    }

    [TargetRpc]
    public void RpcOnGamePhaseChanged(NetworkConnectionToClient conn, GamePhase oldPhase, GamePhase newPhase)
    {
        PlayerState.onGamePhaseChanged?.Invoke(oldPhase, newPhase);
    }

    [TargetRpc]
    public void RpcOnShipDestroyed(NetworkConnectionToClient conn, int shipIndex)
    {
        PlayerState.onShipDestroyed?.Invoke(this, shipIndex);
    }

    [TargetRpc]
    public void RpcOnTurnFinished(NetworkConnectionToClient conn)
    {
        PlayerState.onTurnFinished?.Invoke();
        DeserializeGameState();
    }

    [TargetRpc]
    public void RpcOnCellHit(NetworkConnectionToClient conn, CellHitData cellHitData)
    {
        AddHit(cellHitData);
    }

    [Command]
    public void CmdSyncGameStateWServer(SyncedGameState gameState)
    {
        this.syncedState = gameState;
    }

    [TargetRpc]
    public void RpcSyncGameStateWClient(NetworkConnectionToClient conn, SyncedGameState gameState)
    {
        this.syncedState = gameState;
    }
    public void SerializeGameState()
    {
        int cellCount = localGameState.battleField.GetFieldSize();

        syncedState.field = new BattleCell[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            BattleCell extCell = localGameState.battleField.field[i];

            BattleCell cell = new BattleCell();
            cell.Initialize(extCell.getBottomLeftOriginRaw());
            cell.wasHitOnce = extCell.wasHitOnce;

            if (!extCell.IsFree())
            {
                cell.shipData = new RuntimeShipData();
                cell.shipData.Initialize(extCell.shipData);
            }

            syncedState.field[i] = cell;
        }

        int shipsCount = localGameState.shipManager.totalShipCount;

        syncedState.shipInstances = new RuntimeShipData[shipsCount];
        for (int i = 0; i < shipsCount; i++)
        {
            RuntimeShipData extShipData = localGameState.shipManager.shipInstances[i];

            RuntimeShipData shipData = new RuntimeShipData();
            shipData.Initialize(extShipData);

            syncedState.shipInstances[i] = shipData;
        }


        int shipTypeCount = (int)ShipType.Count;
        syncedState.currentShipCounts = new int[shipTypeCount];

        for (int i = 0; i < shipTypeCount; i++)
        {
            syncedState.currentShipCounts[i] = localGameState.shipManager.currentShipCounts[i];
        }

        syncedState.totalShipCount = localGameState.shipManager.totalShipCount;
    }
    public void DeserializeGameState()
    {
        int cellCount = syncedState.field.Count();

        localGameState.battleField.field = new BattleCell[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            BattleCell extCell = syncedState.field[i];

            BattleCell cell = new BattleCell();
            cell.Initialize(extCell.getBottomLeftOriginRaw());
            cell.wasHitOnce = extCell.wasHitOnce;

            if (!extCell.IsFree())
            {
                cell.shipData = new RuntimeShipData();
                cell.shipData.Initialize(extCell.shipData);
            }

            localGameState.battleField.field[i] = cell;
        }

        int shipsCount = syncedState.shipInstances.Count();

        localGameState.shipManager.shipInstances = new List<RuntimeShipData>();
        for (int i = 0; i < shipsCount; i++)
        {
            RuntimeShipData extShipData = syncedState.shipInstances[i];

            RuntimeShipData shipData = new RuntimeShipData();
            shipData.Initialize(extShipData);

            localGameState.shipManager.shipInstances.Add(shipData);
        }


        int shipTypeCount = (int)ShipType.Count;
        localGameState.shipManager.currentShipCounts = new List<int>();

        for (int i = 0; i < shipTypeCount; i++)
        {
            localGameState.shipManager.currentShipCounts.Add(syncedState.currentShipCounts[i]);
        }

        localGameState.shipManager.totalShipCount = syncedState.totalShipCount;
    }
}
