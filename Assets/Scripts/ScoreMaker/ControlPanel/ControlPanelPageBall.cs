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
    Paste,
}

public class ControlPanelPageBall : ControlPanelPageBase
{
    [SerializeField] private CustomButton _selectSingleBallButton; // シングルボール選択ボタン
    [SerializeField] private CustomButton _selectLongBallButton; // ロングボール選択ボタン
    [SerializeField] private CustomButton _selectLineButton; // ライン選択ボタン
    [SerializeField] private CustomButton _pasteButton; // ペーストボタン

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
        _selectSingleBallButton.SetHighLight(ballType == OperationType.Single);
        _selectLongBallButton.SetHighLight(ballType == OperationType.Long);
        _selectLineButton?.SetHighLight(ballType == OperationType.SelectLine);
        _pasteButton.SetHighLight(ballType == OperationType.Paste);

        // bool isEditMode = ballType == OperationType.SelectLine || ballType == OperationType.Paste;
        // _selectLongBallButton.gameObject.SetActive(!isEditMode);
        // _pasteButton.gameObject.SetActive(isEditMode);
    }
}
