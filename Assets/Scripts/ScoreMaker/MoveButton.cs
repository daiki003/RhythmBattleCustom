using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveButton : Button
{
    public int TargetLineNumber { get; private set; }
    private const float _timeBarLength = 960f; // 時間移動バーの長さ

    public void SetLineNumber(int number, int totalLineNumber)
    {
        TargetLineNumber = number;
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, _timeBarLength * ((float)number / totalLineNumber));
    }
}
