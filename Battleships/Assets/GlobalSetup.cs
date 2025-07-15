using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GlobalSetup : MonoBehaviour
{
    [Header("Battlefield Setup")]
    public BattleFieldSetup battlefieldSetup;

    [Header("Ship Manager Setup")]
    public ShipManagerSetupData shipManagerSetup;

    private static GlobalSetup instance;

    public static GlobalSetup Instance()
    {
        return instance;
    }

    private void Awake()
    {
        instance = this;
    }
}
