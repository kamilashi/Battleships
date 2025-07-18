using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ShipObjectData
{
    public GameObject segment;
    public MeshRenderer meshRenderer;
}

public class ShipObject : MonoBehaviour, IVisualSpawner
{
    [Header("Setup in Prefab")]
    public List<GameObject> segments;
    public Color highlightColor;

    [Header("AutoSetup")]
    public RuntimeShipData shipData;
    public float objectHeight = 1.0f;
    public Dictionary<int, ShipObjectData> segmentDatas;
    public BattleFieldView battleFieldView;

    private MaterialPropertyBlock mpb;

    // Update is called once per frame
    public void Initialize(BattleFieldView view, RuntimeShipData shipData, Orientation shipOrientation, int originX, int originY)
    {
        this.shipData = shipData;
        this.battleFieldView   = view;

        segmentDatas = new Dictionary<int, ShipObjectData>();
        Vector2Int direction = RuntimeShipData.GetOrientation(shipOrientation);
        BattleField localBattleField = battleFieldView.localPlayerController.GetLocalGameState().battleField;

        Debug.Assert(segments.Count == shipData.Size());

        for(int i= 0; i< shipData.Size(); i++)
        {
            int coordX = originX + i * direction.x;
            int coordY = originY + i * direction.y;
            //CellData cellData = battleFieldView.GetCellObject(coordX, coordY).cellData;
            int flatIndex = localBattleField.GetFlatCellIndex(coordX, coordY);

            ShipObjectData shipObjectData = new ShipObjectData();
            shipObjectData.segment = segments[i];
            shipObjectData.meshRenderer = segments[i].GetComponent<MeshRenderer>();

            segmentDatas.Add(flatIndex, shipObjectData);
        }

        mpb = new MaterialPropertyBlock();
    }

    public void SpawnChild(GameObject child, Vector3 localPosition)
    {
        GameObject childObject = GameObject.Instantiate(child, this.transform);

        childObject.transform.localPosition = localPosition;
    }
    public void SpawnChildWithGlobalPosition(GameObject child, Vector3 globalPosition)
    {
        GameObject childObject = GameObject.Instantiate(child, this.transform);

        Vector3 localPosition = transform.InverseTransformPoint(globalPosition);

        childObject.transform.localPosition = localPosition;
    }
    public void HighlightShipSegment(int flatIndex)
    {
        SetMaterialColor(segmentDatas[flatIndex].meshRenderer, "_BaseColor",  highlightColor);
    }
    public void SetMaterialColor(MeshRenderer meshRenderer, string propertyName, Color color)
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(propertyName, color);
        meshRenderer.SetPropertyBlock(mpb);
    }
}
