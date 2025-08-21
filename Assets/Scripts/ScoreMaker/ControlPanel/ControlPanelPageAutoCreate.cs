using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageAutoCreate : ControlPanelPageBase
{
    [SerializeField] private Button _evenlySpacedButton; // 均等配置ボタン
    [SerializeField] private Button _autoCreateButton; // 自動生成ボタン
    [SerializeField] private Button _playMakeButton; // 演奏作成ボタン
    [SerializeField] private Button _allClearButton; // 全削除ボタン

    public override void Init()
    {
        _evenlySpacedButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestEvenlySpaced());
        }).AddTo(this);
        _autoCreateButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestAutoCreate());
        }).AddTo(this);
        _playMakeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestPlayMake());
        }).AddTo(this);
        _allClearButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestAllClear());
        }).AddTo(this);
    }
}
