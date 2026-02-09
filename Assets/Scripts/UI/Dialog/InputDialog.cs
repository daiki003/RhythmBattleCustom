using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class InputDialogOption : DialogOptionBase
{
    public string MessageText;
    public string PlaceHolderText;
    public string InitialInputText;
}

public class InputDialogResult : DialogResultBase
{
    public string StageName;
}

public class InputDialog : DialogBase
{
    [SerializeField] private Text _messageText;
    [SerializeField] private InputField _inputField;
    [SerializeField] private Text _placeHolderText;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        if (dialogOption is InputDialogOption inputDialogOption)
        {
            _messageText.text = inputDialogOption.MessageText;
            _placeHolderText.text = inputDialogOption.PlaceHolderText;
            _inputField.text = inputDialogOption.InitialInputText;
        }
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        // Okの場合はボタン側で鳴らしたい
        if (resultType != DialogResultType.Ok)
        {
            SEManager.instance.PlaySe(SeName.Cancel);
        }
        _onCloseDialog.OnNext(new InputDialogResult
        {
            ResultType = resultType,
            StageName = _inputField.text
        });
        Destroy(gameObject);
    }
}
