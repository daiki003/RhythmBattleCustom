using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum OperationType
{
    Hybrid = 0,
    Ball = 1,
    SelectLine = 2,
    MAX = 3,
}

public enum ClickLineType
{
    NorrowPocket,
    Pocket,
    Line
}

public class ControlPanelPageBall : ControlPanelPageBase
{
    [SerializeField] private GameObject _hybridImage; // ハイブリッド画像
    [SerializeField] private GameObject _selectBallImage; // シングルボール選択画像
    [SerializeField] private GameObject _selectLineImage; // ライン選択画像
    [SerializeField] private CustomButton _changeButton; // 切替ボタン

    private OperationType _currentOperationType;

    public override void Init()
    {
        SetSelectBallType(OperationType.Hybrid);
        _changeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _currentOperationType++;
            if (_currentOperationType >= OperationType.MAX)
            {
                _currentOperationType = 0;
            }
            SetSelectBallType(_currentOperationType);
        }).AddTo(this);
    }

    public void SetSelectBallType(OperationType ballType)
    {
        _currentOperationType = ballType;
        _onRequest.OnNext(new ControlPanelRequestSelectBall(ballType));
        _hybridImage.SetActive(ballType == OperationType.Hybrid);
        _selectBallImage.SetActive(ballType == OperationType.Ball);
        _selectLineImage?.SetActive(ballType == OperationType.SelectLine);
    }
}
