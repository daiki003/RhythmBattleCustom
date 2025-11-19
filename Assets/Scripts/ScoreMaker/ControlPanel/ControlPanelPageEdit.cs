using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageEdit : ControlPanelPageBase
{
    [SerializeField] private Button _copyButton; // コピーボタン
    [SerializeField] private Button _pasteButton; // ペーストボタン
    [SerializeField] private Button _selectCancelButton; // 選択解除ボタン
    [SerializeField] private Button _inversionButton; // 左右反転ボタン
    [SerializeField] private Button _deleteRangeButton; // 範囲削除ボタン
    [SerializeField] private Button _stageDuplicateButton; // ステージ複製ボタン

    private bool _isSelectedLine;
    private bool _isCopiedLine;
    private bool _isExsistPastLine;

    public override void Init()
    {
        _copyButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestCopy());
        }).AddTo(this);
        _selectCancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestSelectCancel());
        }).AddTo(this);
        _inversionButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestInversion());
        }).AddTo(this);
        _deleteRangeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestDeleteRange());
        }).AddTo(this);
        _stageDuplicateButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestStageDuplicate());
        }).AddTo(this);
    }

    public override void SetButtonState(bool isSelectedLine, bool isCopiedLine, bool isExsistPastLine)
    {
        _isSelectedLine = isSelectedLine;
        _isCopiedLine = isCopiedLine;
        _isExsistPastLine = isExsistPastLine;
    }

    void Update()
    {
        _copyButton.interactable = _isSelectedLine;
        _inversionButton.interactable = _isSelectedLine;
        _selectCancelButton.interactable = _isSelectedLine;
        _deleteRangeButton.interactable = _isSelectedLine;
        _pasteButton.interactable = _isCopiedLine;
    }
}
