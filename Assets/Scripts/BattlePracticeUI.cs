using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class BattlePracticeUI : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private GameObject _timeUI;
    [SerializeField] private List<TimeJumpButton> _timeJumpButtonList = new();
    [SerializeField] private Transform _buttonIconArea;
    [SerializeField] private Slider _timeSlider;

    private bool _isPause;
    private const string _buttonIconPrefabPath = "JumpButtonIcon";
    private const float _sliderWidth = 780;

    public Subject<bool> OnClickPauseButton = new();
    public Subject<float> OnSliderValueChange = new();
    public Subject<float> OnTimeJump = new();

    public void Init()
    {
        _isPause = false;
        _timeUI.gameObject.SetActive(false);
        _pauseButton.OnClickAsObservable().Subscribe(_ =>
        {
            _isPause = !_isPause;
            OnClickPauseButton.OnNext(_isPause);
            _timeUI.SetActive(_isPause);
        }).AddTo(this);
        _timeSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            OnSliderValueChange.OnNext(x);
        }).AddTo(this);
        for (int i = 0; i < _timeJumpButtonList.Count; i++)
        {
            var jumpButton = _timeJumpButtonList[i];
            int buttonNumber = i + 1;
            jumpButton.OnClickMainButton.Subscribe(timeRate =>
            {
                OnTimeJump.OnNext(timeRate);
            });
            jumpButton.OnClickRegisterButton.Subscribe(_ =>
            {
                jumpButton.SetTimeRate(_timeSlider.value);
                var icon = Instantiate(ResourceManager.LoadPrefab<JumpButtonIcon>(_buttonIconPrefabPath), _buttonIconArea);
                icon.SetSprite(buttonNumber);
                icon.transform.SetAnchoredPositionX(_sliderWidth * _timeSlider.value);
            });
        }
    }
}
