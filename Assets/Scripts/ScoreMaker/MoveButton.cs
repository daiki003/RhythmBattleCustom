using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveButton : Button
{
    public int TargetLineNumber { get; private set; }
    private const float _timeBarLength = 935f; // 時間移動バーの長さ
    private const float _timeBarOffset = 13f; // 時間移動バー位置補正

    public void SetLineNumber(int number, int totalLineNumber)
    {
        TargetLineNumber = number;
        transform.SetAnchoredPositionY(_timeBarLength * ((float)number / totalLineNumber) + _timeBarOffset);
    }
}
