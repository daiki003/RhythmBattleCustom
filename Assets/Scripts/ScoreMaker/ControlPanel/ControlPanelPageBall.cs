using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum OperationType
{
    Ball,
    SelectLine,
    Hybrid,
    Paste,
}

public enum ClickLineType
{
    NorrowPocket,
    Pocket,
    Line
}

public class ControlPanelPageBall : ControlPanelPageBase
{
    [SerializeField] private CustomButton _hybridButton; // ハイブリッドボタン
    [SerializeField] private CustomButton _selectBallButton; // シングルボール選択ボタン
    [SerializeField] private CustomButton _selectLineButton; // ライン選択ボタン
    [SerializeField] private CustomButton _pasteButton; // ペーストボタン

    public override void Init()
    {
        _hybridButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Hybrid);
        }).AddTo(this);
        _selectBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Ball);
        }).AddTo(this);
        _selectLineButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.SelectLine);
        }).AddTo(this);
        _pasteButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Paste);
        }).AddTo(this);
    }

    public void SetSelectBallType(OperationType ballType)
    {
        _onRequest.OnNext(new ControlPanelRequestSelectBall(ballType));
        _hybridButton.SetHighLight(ballType == OperationType.Hybrid);
        _selectBallButton.SetHighLight(ballType == OperationType.Ball);
        _selectLineButton?.SetHighLight(ballType == OperationType.SelectLine);
        _pasteButton.SetHighLight(ballType == OperationType.Paste);
    }
}
