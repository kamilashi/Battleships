using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipObject : MonoBehaviour
{
    public RuntimeShipData shipData;
    public float objectHeight = 1.0f;

    // Update is called once per frame
    public void Initialize(RuntimeShipData shipData)
    {
        this.shipData = shipData;
    }

    public void SpawnChild(GameObject child, Vector3 localPosition)
    {
        GameObject shipGameObject = GameObject.Instantiate(child, this.transform);

        shipGameObject.transform.localPosition = localPosition;
    }
    public void SpawnChildWithGlobalPosition(GameObject child, Vector3 globalPosition)
    {
        GameObject shipGameObject = GameObject.Instantiate(child, this.transform);

        Vector3 localPosition = transform.InverseTransformPoint(globalPosition);

        shipGameObject.transform.localPosition = localPosition;
    }
}
