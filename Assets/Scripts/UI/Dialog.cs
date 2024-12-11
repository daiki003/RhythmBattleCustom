using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _button;
    [SerializeField] private Text _buttonText;

    public void Init(string messageText, string buttonText)
    {
        _messageText.text = messageText;
        _buttonText.text = buttonText;
        _button.OnClickAsObservable().Subscribe(_ =>
        {
            Destroy(gameObject);
        });
    }
}
