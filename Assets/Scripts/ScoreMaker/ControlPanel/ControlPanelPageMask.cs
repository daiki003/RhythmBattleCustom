using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageMask : ControlPanelPageBase
{
    [SerializeField] private CustomButton _closeButton; // 閉じるボタン
    [SerializeField] private CustomButton _inversionButton; // 左右反転ボタン
    [SerializeField] private CustomButton _upButton; // 上移動
    [SerializeField] private CustomButton _downButton; // 下移動
    [SerializeField] private CustomButton _deleteButton; // 削除
    [SerializeField] private CustomButton _copyButton; // コピー
    [SerializeField] private CustomButton _pasteButton; // ペースト

    public bool IsPasteMode { get;  private set; }

    public override void Init()
    {
        _closeButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestCloseMask());
        }).AddTo(this);
        _inversionButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestInversion());
        }).AddTo(this);
        _upButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestUp());
        }).AddTo(this);
        _downButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestDown());
        }).AddTo(this);
        _deleteButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestDeleteRange());
        }).AddTo(this);
        _copyButton?.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestCopy());
        }).AddTo(this);
        _pasteButton?.OnClickAsObservable().Subscribe(_ =>
        {
            IsPasteMode = !IsPasteMode;
            _onRequest.OnNext(new ControlPanelRequestStartPaste(IsPasteMode));
            _pasteButton.SetHighLight(IsPasteMode);
        }).AddTo(this);
    }

    public void FinishPaste()
    {
        IsPasteMode = false;
        _pasteButton.SetHighLight(false);
    }

    public override void SetButtonState(bool isSelectedLine, bool isCopiedLine)
    {
        _copyButton.interactable = isSelectedLine;
        _pasteButton.interactable = isCopiedLine;
    }
}
