using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        ShipData shipData = shipManager.GetShipData(type);
        if (!battleField.CanPlaceShip(x, y, shipData))
        {
            Debug.Log("Cannot place ship here.");
            return;
        }

        if (shipManager.CreateShip(type))
        {
            battleField.PlaceShip(x, y, shipData);
            battlefieldView.SpawnShipObject(x, y, shipData);
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

                    break;
                }
        }
    }

    public void OnShipTypeSelected(ShipType selectedType)
    {
        selectedShipType = selectedType;
    }
}
