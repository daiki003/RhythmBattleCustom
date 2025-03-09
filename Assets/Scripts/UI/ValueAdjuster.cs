using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンで値を調整するUI
/// </summary>
public class ValueAdjuster : MonoBehaviour
{
    [SerializeField] private Button _minusButton;
    [SerializeField] private Button _plusButton;
    [SerializeField] private ContentSizeFitter _titleSizeFitter;
    [SerializeField] private Text _valueText;
    [SerializeField] private VerticalLayoutGroup _layputGroup;

    private float _currentValue;
    private float _changeValueUnit;
    public float CurrentValue => _currentValue;

    public void Init(float startValue, float changeValueUnit)
    {
        _changeValueUnit = changeValueUnit;
        SetValue(startValue);
        _minusButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeValue(-1 * _changeValueUnit);
        });
        _plusButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeValue(_changeValueUnit);
        });

        _titleSizeFitter.SetLayoutVertical();
        _layputGroup.CalculateLayoutInputVertical();
        _layputGroup.SetLayoutVertical();
    }

    private void ChangeValue(float diff)
    {
        SetValue(_currentValue + diff);
    }

    private void SetValue(float value)
    {
        _currentValue = value;
        int decimalPlaces = _changeValueUnit.GetPrecision();
        _valueText.text = string.Format("{0:F" + decimalPlaces + "}", _currentValue);
    }
}
