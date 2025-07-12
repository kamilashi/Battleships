using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISquareScaler : MonoBehaviour
{
    [Header("Setup in Prefab")]
    public RectTransform rectTransform;

    void Awake()
    {
        Vector2 size = rectTransform.sizeDelta;
        size.x = Screen.height;
        rectTransform.sizeDelta = size;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
