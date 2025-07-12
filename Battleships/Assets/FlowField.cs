using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Divergence
{
    Sink = -1,
    Source = 1,
    Swirl = 0
}

public struct FlowCell
{
    public Vector3 bottomLeftOrigin;
    public Vector3 velocity;
    public Vector3 direction;
    public float scale;

    public float convergence;
}

public class FlowField : MonoBehaviour
{
    [Header("Manual Setup")]
    public int horizCellsCount = 4;
    public int vertiCellsCount = 4;
    public float cellSize = 1.0f;

    public Transform originTransformBottomLeft;
    public Camera mainCamera;

    [Header("Debug")]
    public Vector3 direction;
    public float magnitude;

    public float sourceMagnitude = 2.0f;
    public float sourceSpread = 5.0f;

    public float sinkMagnitude = 4.0f;
    public float sinkSpread = 5.0f;

    public Divergence placedType;

    [Header("Debug View")]
    public FlowCell[,] field;
    public bool isInitialized = false;

    void Awake()
    {
        InitializeCells();
        isInitialized = true;
    }

    void Update()
    {
        
    }

    void InitializeCells()
    {
        field = new FlowCell[horizCellsCount, vertiCellsCount];

        for (int x= 0; x < horizCellsCount; x++)
        {
            for (int y = 0; y < vertiCellsCount; y++)
            {
                ref FlowCell cell = ref field[x, y];

                cell.convergence = 0.0f;

                cell.bottomLeftOrigin.x = originTransformBottomLeft.position.x + x * cellSize;
                cell.bottomLeftOrigin.z = originTransformBottomLeft.position.z + y * cellSize;
                cell.bottomLeftOrigin.y = originTransformBottomLeft.position.y;

                cell.direction = Vector3.zero;

                cell.velocity = direction.normalized * magnitude;

                cell.scale = 0.0f;

                cell.convergence = 0.0f;
            }
        }
    }


    [ContextMenu("FillTestDirection")]
    void FillTestDirection()
    {
        for (int x = 0; x < horizCellsCount; x++)
        {
            for (int y = 0; y < vertiCellsCount; y++)
            {
                ref FlowCell cell = ref field[x, y];

                cell.velocity = direction.normalized * magnitude;
            }
        }
    }

    void ApplyGaussianInfluence(int sourceX, int sourceY, float amplitude, float sigma, Divergence sign)
    {
        Vector3 source = field[sourceX, sourceY].bottomLeftOrigin;

        float twoSigmaSq = 2 * sigma * sigma;

        for (int x = 0; x < horizCellsCount; x++)
        {
            for (int y = 0; y < vertiCellsCount; y++)
            {
                Vector3 worldPos = field[x, y].bottomLeftOrigin;
                Vector3 dir = (int)sign * (worldPos - source).normalized;

                float distSq = (worldPos - source).sqrMagnitude;
                float weight = amplitude * Mathf.Exp(-distSq / twoSigmaSq);

                field[x, y].velocity += dir * weight;
            }
        }
    }

    public void OnCellSelected(int x, int y)
    {
        if(placedType == Divergence.Source)
        {
            PlaceSource(x, y, sourceMagnitude, sourceSpread);
        }
        else
        {
            PlaceSink(x, y, sourceMagnitude, sourceSpread);
        }
    }

    void PlaceSource(int x, int y, float magnitude, float spread)
    {
        ApplyGaussianInfluence(x, y, magnitude, spread, Divergence.Source);
    }

    void PlaceSink(int x, int y, float magnitude, float spread)
    {
        ApplyGaussianInfluence(x, y, magnitude, spread, Divergence.Sink);
    }

}
