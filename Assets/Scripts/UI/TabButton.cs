using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] private CustomButton _button;
    [SerializeField] private GameObject _normalImage;
    [SerializeField] private GameObject _activeImage;

    private Subject<Unit> _onClick = new();
    public Observable<Unit> OnClick => _onClick;

    public void Init()
    {
        _normalImage.SetActive(true);
        _activeImage.SetActive(false);
        _button.OnClickAsObservable().Subscribe(_ =>
        {
            _onClick.OnNext(default);
        }).AddTo(this);
    }

    public void SetActive(bool isActive)
    {
        _normalImage.SetActive(!isActive);
        _activeImage.SetActive(isActive);
    }

    public void SetText(string text)
    {
        _button.SetText(text);
    }

    void OnDestroy()
    {
        _onClick.Dispose();
        _onClick = null;
    }
}
