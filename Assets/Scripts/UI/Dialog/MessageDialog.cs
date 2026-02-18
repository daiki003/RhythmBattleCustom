using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class MessageDialogOption : DialogOptionBase
{
    public string MessageText;
}

public class MessageDialog : DialogBase<DialogResultBase>
{
    [SerializeField] private Text _messageText;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        if (dialogOption is MessageDialogOption messageDialogOption)
        {
            _messageText.text = messageDialogOption.MessageText;
        }
    }
}
