using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private Image _mainImage;
    [SerializeField] private HomePanelType _buttonType;
    [SerializeField] private Button _button;

    public HomePanelType ButtonType => _buttonType;

    private Color32 _activeButtonColor = new Color32(255, 255, 255, 255);
    private Color32 _nonActiveButtonColor = new Color32(255, 255, 255, 140);

    public Subject<Unit> OnWhenClicked = new Subject<Unit>();

    void Awake()
    {
        if (_button != null)
        {
            _button.OnClickAsObservable().Subscribe(_ =>
            {
                OnWhenClicked.OnNext(default);
            });
        }
    }

    public void OnClick()
    {
        SEManager.instance.PlayButtonSe();
        SetLight(true);
    }

    public void SetLight(bool isActive)
    {
        var buttonImage = _mainImage.GetComponent<Image>();
        buttonImage.color = isActive ? _activeButtonColor : _nonActiveButtonColor;
    }
}
