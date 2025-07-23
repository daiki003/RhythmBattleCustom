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
    [SerializeField] private InputField _inputField;

    private ReactiveProperty<float> _currentValue = new(0f);
    private float _changeValueUnit;
    public ReactiveProperty<float> CurrentValue => _currentValue;

    public void Init(float startValue, float changeValueUnit)
    {
        _changeValueUnit = changeValueUnit;
        SetValue(startValue);
        _minusButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeValue(-1 * _changeValueUnit);
        }).AddTo(this);
        _plusButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeValue(_changeValueUnit);
        }).AddTo(this);

        // InputFieldがあるならそれも監視
        if (_inputField != null)
        {
            _inputField.onEndEdit.AddListener(value =>
            {
                if (float.TryParse(value, out float parsedValue))
                {
                    SetValue(parsedValue);
                }
                else
                {
                    // パースに失敗した場合は元の値に戻す
                    _inputField.text = _currentValue.ToString();
                }
            });
        }

        if (_titleSizeFitter != null)
        {
            _titleSizeFitter.SetLayoutVertical();
        }
        if (_layputGroup != null)
        {
            _layputGroup.CalculateLayoutInputVertical();
            _layputGroup.SetLayoutVertical();
        }
    }

    private void ChangeValue(float diff)
    {
        SetValue(_currentValue.Value + diff);
    }

    public void SetValue(float value)
    {
        _currentValue.Value = value;
        int decimalPlaces = _changeValueUnit.GetPrecision();
        string valueString = string.Format("{0:F" + decimalPlaces + "}", _currentValue);
        if (_inputField != null)
        {
            _inputField.text = valueString;
        }
        else
        {
            _valueText.text = valueString;
        }
    }
}
