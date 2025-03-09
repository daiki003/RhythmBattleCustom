using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class DialogOptionBase
{
    public string TitleText;
    public string OkButtonText = "OK";
    public string CancelButtonText = "キャンセル";
    public ButtonSeType OkButtonSeType = ButtonSeType.Button1;
    public ButtonSeType CancelButtonSeType = ButtonSeType.None;
    public bool HideCancelButton;
    public bool HideOkButton;
}

public class DialogResultBase
{
    public DialogResultType ResultType;
}

public enum DialogResultType
{
    None,
    Cancel,
    Ok,
}

public class DialogBase : MonoBehaviour
{
    [SerializeField] private DialogCommonParts _dialogCommonParts;
    [SerializeField] private Button _bgButton;

    protected Subject<DialogResultBase> _onCloseDialog = new();
    public virtual Observable<DialogResultBase> OnCloseDialog => _onCloseDialog;

    public virtual void Init(DialogOptionBase dialogOption)
    {
        _dialogCommonParts.Init(dialogOption);
        _dialogCommonParts.OkButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel(DialogResultType.Ok);
        }).AddTo(this);
        _dialogCommonParts.CancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel(DialogResultType.Cancel);
        }).AddTo(this);
        _dialogCommonParts.CloseButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel(DialogResultType.None);
        }).AddTo(this);
        _bgButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel(DialogResultType.None);
        }).AddTo(this);

        _dialogCommonParts.CancelButton.gameObject.SetActive(!dialogOption.HideCancelButton);
        _dialogCommonParts.OkButton.gameObject.SetActive(!dialogOption.HideOkButton);
    }

    public virtual void ClosePanel(DialogResultType resultType)
    {
        // Okの場合はボタン側で鳴らしたい
        if (resultType != DialogResultType.Ok)
        {
            SEManager.instance.PlaySe(SeName.Cancel);
        }        
        _onCloseDialog.OnNext(new DialogResultBase
        {
            ResultType = resultType,
        });
        Destroy(gameObject);
    }
}
