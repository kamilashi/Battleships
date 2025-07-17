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
    public ShipType selectedShipType;
    public GamePhase gamePhase;
    public Orientation shipOrientation;
}

public class PlayerController : NetworkBehaviour
{
    // player -> server
    public static UnityEvent<PlayerController> onPlayerSpawnedEvent = new UnityEvent<PlayerController>();
    public static UnityEvent<PlayerController> onPlayerDespawnedEvent = new UnityEvent<PlayerController>();
    //public static UnityEvent<int> onSubmitSignalIssuedEvent = new UnityEvent<int>();

    // server -> player
    public static UnityEvent onTurnFinished = new UnityEvent();

    // player/server -> locals
    public static UnityEvent<PlayerController> onLocalPlayerInitializedEvent = new UnityEvent<PlayerController>();
    public static UnityEvent<PlayerController, Vector2Int, RuntimeShipData, Orientation> onShipAdded = new UnityEvent<PlayerController, Vector2Int, RuntimeShipData, Orientation>();
    public static UnityEvent<PlayerController, int> onShipDestroyed = new UnityEvent<PlayerController, int>();

    private GameLogic gameLogic;

    [SyncVar] public SyncedGameState syncedState;


    public List<CellHitData> hitQueue = new List<CellHitData>();

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
        gameLogic = new GameLogic(setup.battlefieldSetup, setup.shipManagerSetup);

        onLocalPlayerInitializedEvent?.Invoke(this);
    }
    private void TryPlaceShip(int x, int y, ShipType type)
    {
        GameLogic localGameState = gameLogic;
        if (type == ShipType.Count)
        {
            Debug.Log("Choose a ship to place first.");
            return;
        }

        StaticShipData shipData = gameLogic.shipManager.GetShipData(type);
        if (!gameLogic.battleField.CanPlaceShip(x, y, shipData, syncedState.shipOrientation))
        {
            Debug.Log("Cannot place ship here.");
            return;
        }

        RuntimeShipData shipInstanceData = gameLogic.shipManager.CreateShip(type, x, y, syncedState.shipOrientation);

        if (shipInstanceData != null)
        {
            gameLogic.battleField.PlaceShip(x, y, shipInstanceData, shipData, syncedState.shipOrientation);
            onShipAdded?.Invoke(this, new Vector2Int(x, y), shipInstanceData, syncedState.shipOrientation);
        }
    }
    public void AddHit(CellHitData hitData)
    {
        hitQueue.Add(hitData);
    }
    public void OnShipTypeSelected(ShipType selectedType)
    {
        syncedState.selectedShipType = selectedType;
    }

    public void OnSubmitSignalReceived()
    {
        GamePhase currentPhase = syncedState.gamePhase;

        switch (currentPhase)
        {
            case GamePhase.Build:
                {
                    /*if (gameState.shipManager.availableShipCounts.Exists(x => x > 0))
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
                        Debug.Log("You need to select a cell to hit!");
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
    public void StoreHitInput(int x, int y)
    {
        syncedState.hitCoords[0] = x;
        syncedState.hitCoords[1] = y;
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
                    TryPlaceShip(x, y, syncedState.selectedShipType);
                    break;
                }
            case GamePhase.Combat:
                {
                    StoreHitInput(x, y);
                    break;
                }
        }
    }

    public List<StaticShipData> GetShipDataList()
    {
        return gameLogic.shipManager.shipDatas;
    }

    public GameLogic GetLocalGameState()
    {
        return gameLogic;
    }
    public SyncedGameState GetSyncedGameState()
    {
        return syncedState;
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

    [TargetRpc]
    public void RpcInitializeClient(NetworkConnectionToClient conn)
    {
        Initialize();
    }

    [TargetRpc]
    public void RpcOnShipDestroyed(NetworkConnectionToClient conn, int shipIndex)
    {
        PlayerController.onShipDestroyed?.Invoke(this, shipIndex);
    }

    [TargetRpc]
    public void RpcOnTurnFinished(NetworkConnectionToClient conn)
    {
        PlayerController.onTurnFinished?.Invoke();
        DeserializeGameState();
    }

    [TargetRpc]
    public void RpcOnCellHit(NetworkConnectionToClient conn, CellHitData cellHitData)
    {
        if(isLocalPlayer)
        {
            AddHit(cellHitData);
        }
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
        int cellCount = gameLogic.battleField.GetFieldSize();

        syncedState.field = new BattleCell[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            BattleCell extCell = gameLogic.battleField.field[i];

            BattleCell cell = new BattleCell();
            cell.Initialize(extCell.getBottomLeftOriginRaw());

            if (!extCell.IsFree())
            {
                cell.shipData = new RuntimeShipData();
                cell.shipData.Initialize(extCell.shipData.health, extCell.shipData.type, extCell.shipData.instanceId);
            }

            syncedState.field[i] = cell;
        }

        int shipsCount = gameLogic.shipManager.totalShipCount;

        syncedState.shipInstances = new RuntimeShipData[shipsCount];
        for (int i = 0; i < shipsCount; i++)
        {
            RuntimeShipData extShipData = gameLogic.shipManager.shipInstances[i];

            RuntimeShipData shipData = new RuntimeShipData();
            shipData.Initialize(extShipData.health, extShipData.type, extShipData.instanceId);

            syncedState.shipInstances[i] = shipData;
        }


        int shipTypeCount = (int)ShipType.Count;
        syncedState.currentShipCounts = new int[shipTypeCount];

        for (int i = 0; i < shipTypeCount; i++)
        {
            syncedState.currentShipCounts[i] = gameLogic.shipManager.currentShipCounts[i];
        }

        syncedState.totalShipCount = gameLogic.shipManager.totalShipCount;
    }
    public void DeserializeGameState()
    {
        int cellCount = syncedState.field.Count();

        gameLogic.battleField.field = new BattleCell[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            BattleCell extCell = syncedState.field[i];

            BattleCell cell = new BattleCell();
            cell.Initialize(extCell.getBottomLeftOriginRaw());

            if (!extCell.IsFree())
            {
                cell.shipData = new RuntimeShipData();
                cell.shipData.Initialize(extCell.shipData.health, extCell.shipData.type, extCell.shipData.instanceId);
            }

            gameLogic.battleField.field[i] = cell;
        }

        int shipsCount = syncedState.shipInstances.Count();

        gameLogic.shipManager.shipInstances = new List<RuntimeShipData>();
        for (int i = 0; i < shipsCount; i++)
        {
            RuntimeShipData extShipData = syncedState.shipInstances[i];

            RuntimeShipData shipData = new RuntimeShipData();
            shipData.Initialize(extShipData.health, extShipData.type, extShipData.instanceId);

            gameLogic.shipManager.shipInstances.Add(shipData);
        }


        int shipTypeCount = (int)ShipType.Count;
        gameLogic.shipManager.currentShipCounts = new List<int>();

        for (int i = 0; i < shipTypeCount; i++)
        {
            gameLogic.shipManager.currentShipCounts.Add(syncedState.currentShipCounts[i]);
        }

        gameLogic.shipManager.totalShipCount = syncedState.totalShipCount;
    }
}
