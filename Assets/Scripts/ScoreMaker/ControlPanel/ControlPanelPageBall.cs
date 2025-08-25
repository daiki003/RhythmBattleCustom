using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum OperationType
{
    Single,
    Long,
    Rotation,
    SelectLine,
    Inversion,
    Up,
    Down,
}

public class ControlPanelPageBall : ControlPanelPageBase
{
    [SerializeField] private CustomButton _selectSingleBallButton; // シングルボール選択ボタン
    [SerializeField] private CustomButton _selectLongBallButton; // ロングボール選択ボタン
    [SerializeField] private CustomButton _rotationBallButton; // ボール選択ローテーションボタン
    [SerializeField] private CustomButton _selectLineButton; // ライン選択ボタン
    [SerializeField] private CustomButton _inversionButton; // 左右反転ボタン
    [SerializeField] private CustomButton _upButton; // 上移動
    [SerializeField] private CustomButton _downButton; // 下移動

    public override void Init()
    {
        _selectSingleBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Single);
        }).AddTo(this);
        _selectLongBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Long);
        }).AddTo(this);
        _rotationBallButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Rotation);
        }).AddTo(this);
        _selectLineButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.SelectLine);
        }).AddTo(this);
        _inversionButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Inversion);
        }).AddTo(this);
        _upButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Up);
        }).AddTo(this);
        _downButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(OperationType.Down);
        }).AddTo(this);
    }

    public void SetSelectBallType(OperationType ballType)
    {
        _onRequest.OnNext(new ControlPanelRequestSelectBall(ballType));
        _selectSingleBallButton.SetHighLight(ballType == OperationType.Single);
        _selectLongBallButton.SetHighLight(ballType == OperationType.Long);
        _rotationBallButton?.SetHighLight(ballType == OperationType.Rotation);
        _selectLineButton?.SetHighLight(ballType == OperationType.SelectLine);
        _inversionButton?.SetHighLight(ballType == OperationType.Inversion);
        _upButton?.SetHighLight(ballType == OperationType.Up);
        _downButton?.SetHighLight(ballType == OperationType.Down);
    }
}
