using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipObject : MonoBehaviour
{
    public RuntimeShipData shipData;

    // Update is called once per frame
    public void Initialize(RuntimeShipData shipData)
    {
        this.shipData = shipData;
    }
}
