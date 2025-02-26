using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBase : MonoBehaviour
{
    [SerializeField] private Text _text;
    [SerializeField] private Button _button;
    public Button Button => _button;
    public Subject<Unit> OnClickAsObservable = new();

    void Awake()
    {
        _button.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayBeatSe();
            OnClickAsObservable.OnNext(default);
        });
    }
    public void SetText(string text)
    {
        _text.text = text;
    }
}
