using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogCommonParts : MonoBehaviour
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Text _titleText;
    [SerializeField] private CustomButton _okButton;
    [SerializeField] private CustomButton _cancelButton;
    public Button CloseButton => _closeButton;
    public Button OkButton => _okButton;
    public Button CancelButton => _cancelButton;

    public virtual void Init(DialogOptionBase dialogOption)
    {
        _titleText.text = dialogOption.TitleText;
        _okButton.SetText(dialogOption.OkButtonText);
        _cancelButton.SetText(dialogOption.CancelButtonText);
        _okButton.SetSeType(dialogOption.OkButtonSeType);
        _cancelButton.SetSeType(dialogOption.CancelButtonSeType);
    }
}
