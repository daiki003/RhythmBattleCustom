using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogCommonParts : MonoBehaviour
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Text _titleText;
    [SerializeField] private ButtonBase _okButton;
    [SerializeField] private ButtonBase _cancelButton;
    public Button CloseButton => _closeButton;
    public Button OkButton => _okButton.Button;
    public Button CancelButton => _cancelButton.Button;

    public virtual void Init(DialogOptionBase dialogOption)
    {
        _titleText.text = dialogOption.TitleText;
        _okButton.SetText(dialogOption.OkButtonText);
        _cancelButton.SetText(dialogOption.CancelButtonText);
    }
}
