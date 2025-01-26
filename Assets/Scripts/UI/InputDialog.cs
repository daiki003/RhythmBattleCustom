using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class InputDialog : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    [SerializeField] private InputField _inputField;
    [SerializeField] private Text _placeHolderText;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Button _cancelButton;

    private Subject<string> _onSubmit = new();
    public Observable<string> OnSubmit => _onSubmit;

    public void Init(string messageText, string placeHolderText, string initText = "")
    {
        _messageText.text = messageText;
        _placeHolderText.text = placeHolderText;
        _inputField.text = initText;
        _submitButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onSubmit.OnNext(_inputField.text);
            Destroy(gameObject);
        });
        _cancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            Destroy(gameObject);
        });
    }
}
