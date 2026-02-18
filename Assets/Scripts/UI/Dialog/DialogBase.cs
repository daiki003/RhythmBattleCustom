using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;
using Cysharp.Threading.Tasks;

public class DialogOptionBase
{
    public string TitleText;
    public string OkButtonText = "OK";
    public string OkButton2Text = "OK";
    public string CancelButtonText = "キャンセル";
    public ButtonSeType OkButtonSeType = ButtonSeType.Button1;
    public ButtonSeType OkButton2SeType = ButtonSeType.Button1;
    public ButtonSeType CancelButtonSeType = ButtonSeType.None;
    public bool HideCancelButton;
    public bool HideOkButton;
    public bool HideOkButton2 = true;
    public bool UseYellowCancelButton;
    public Vector2 PositionOffset;
    public Vector2 SizeOffset;
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
    Ok2,
}

public class DialogBase<T> : MonoBehaviour where T : DialogResultBase
{
    [SerializeField] private DialogCommonParts _dialogCommonParts;
    [SerializeField] private Button _bgButton;

    protected Subject<DialogResultBase> _onCloseDialog = new();

    private T _dialogResult;

    public virtual void Init(DialogOptionBase dialogOption)
    {
        _dialogCommonParts.Init(dialogOption);
        _dialogCommonParts.OkButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel(DialogResultType.Ok);
        }).AddTo(this);
        _dialogCommonParts.OkButton2.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel(DialogResultType.Ok2);
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
        _dialogCommonParts.OkButton2.gameObject.SetActive(!dialogOption.HideOkButton2);
    }

    public virtual async UniTask<T> ShowAsync(DialogOptionBase dialogOption)
    {
        Init(dialogOption);
        await UniTask.WaitUntil(() => _dialogResult != null);
        return _dialogResult;
    }

    public virtual void ClosePanel(DialogResultType resultType)
    {
        // Okの場合はボタン側で鳴らしたい
        if (resultType != DialogResultType.Ok)
        {
            SEManager.instance.PlaySe(SeName.Cancel);
        }
        _dialogResult = CreateDialogResult(resultType);
        Destroy(gameObject);
    }

    protected virtual T CreateDialogResult(DialogResultType resultType)
    {
        return (T)new DialogResultBase
        {
            ResultType = resultType,
        };
    }
}
