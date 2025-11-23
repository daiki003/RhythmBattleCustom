using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageCopyPaste : ControlPanelPageBase
{
    [SerializeField] private CustomButton _copyButton;
    [SerializeField] private CustomButton _pasteButton;

    public bool IsPasteMode { get;  private set; }

    public override void Init()
    {
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
