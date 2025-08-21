using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum SelectBallType
{
    Single,
    Long,
    Rotation,
}

public class ControlPanelPageBall : ControlPanelPageBase
{
    [SerializeField] private CustomButton _selectSingleBallButton; // シングルボール選択ボタン
    [SerializeField] private CustomButton _selectLongBallButton; // ロングボール選択ボタン
    [SerializeField] private CustomButton _rotationBallButton; // ボール選択ローテーションボタン
    [SerializeField] private Button _practiceButton; // 練習ボタン

    public override void Init()
    {
        _selectSingleBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(SelectBallType.Single);
        }).AddTo(this);
        _selectLongBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(SelectBallType.Long);
        }).AddTo(this);
        _rotationBallButton?.OnClickAsObservable().Subscribe(_ =>
        {
            SetSelectBallType(SelectBallType.Rotation);
        }).AddTo(this);
        _practiceButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestPractice());
        }).AddTo(this);
    }

    private void SetSelectBallType(SelectBallType ballType)
    {
        _onRequest.OnNext(new ControlPanelRequestSelectBall(ballType));
        _selectSingleBallButton.SetHighLight(ballType == SelectBallType.Single);
        _selectLongBallButton.SetHighLight(ballType == SelectBallType.Long);
        _rotationBallButton?.SetHighLight(ballType == SelectBallType.Rotation);
    }
}
