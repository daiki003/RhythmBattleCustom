using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStrip : MonoBehaviour
{
    [SerializeField] private Button _stripButton;
    [SerializeField] private Text _titleText;

    private Subject<Unit> _onClickedStrip = new Subject<Unit>();
    public Observable<Unit> OnClickedStrip => _onClickedStrip;

    public void Init(string titleText)
    {
        _titleText.text = titleText;
        _stripButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickedStrip.OnNext(default);
        }).AddTo(this);
    }
}
