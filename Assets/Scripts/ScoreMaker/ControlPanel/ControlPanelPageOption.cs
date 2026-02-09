using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageAutoCreate : ControlPanelPageBase
{
    [SerializeField] private Button _evenlySpacedButton; // 均等配置ボタン
    [SerializeField] private Button _playMakeButton; // 演奏作成ボタン
    [SerializeField] private Button _practiceButton; // 試し打ちボタン
    [SerializeField] private Button _saveButton; // 上書き保存ボタン
    [SerializeField] private Button _newSaveButton; //  新規保存ボタン
    [SerializeField] private Button _allClearButton; // 全削除ボタン

    public override string PageName => "一般";

    public override void Init()
    {
        _evenlySpacedButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestEvenlySpaced());
        }).AddTo(this);
        _playMakeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestPlayMake());
        }).AddTo(this);
        _practiceButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestPractice());
        }).AddTo(this);
        _saveButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestSave());
        }).AddTo(this);
        _newSaveButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestNewSave());
        }).AddTo(this);
        _allClearButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestAllClear());
        }).AddTo(this);
    }

    public void SetCanOverriteSave(bool isCanOverriteSave)
    {
        _saveButton.interactable = isCanOverriteSave;
    }
}
