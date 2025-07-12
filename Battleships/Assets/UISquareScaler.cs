using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISquareScaler : MonoBehaviour
{
    [Header("Setup in Prefab")]
    public CanvasScaler canvasScaler;
    public RectTransform canvasTransform;
    public RectTransform viewportRectTransform;

    public RectTransform leftRectTransform;
    public RectTransform rightRectTransform;


    void Awake()
    {
        Init();
    }

    void Init()
    {
        float heightInPPU = canvasTransform.rect.height;
        float widthInPPU = canvasTransform.rect.width;

        viewportRectTransform.sizeDelta = new Vector2(heightInPPU, heightInPPU);

        float halfRemainindWidth = (float)(widthInPPU - heightInPPU) / 2.0f;

        leftRectTransform.sizeDelta = new Vector2(halfRemainindWidth, heightInPPU);
        rightRectTransform.sizeDelta = new Vector2(halfRemainindWidth, heightInPPU);
    }
}
