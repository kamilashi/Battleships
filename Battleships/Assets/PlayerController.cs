using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController
{
    public Vector2Int hitCoords;
    public PlayerController()
    {
        hitCoords = new Vector2Int();
    }

    public void StoreHit(int x, int y)
    {
        hitCoords.x = x;
        hitCoords.y = y;
    }
    public void ClearHit()
    {
        hitCoords.x = -1;
        hitCoords.y = -1;
    }

    public bool HasHitStored()
    {
        return hitCoords.x >= 0 && hitCoords.y >= 0;
    }
}
