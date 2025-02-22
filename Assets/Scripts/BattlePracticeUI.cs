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
    private List<JumpButtonIcon> _buttonIconList = new();

    public Subject<bool> OnClickPauseButton = new();
    public Subject<float> OnSliderValueChange = new();
    public Subject<float> OnTimeJump = new();

    public float SliderValue => _timeSlider.value;

    public void Init()
    {
        _isPause = false;
        _timeUI.gameObject.SetActive(false);
        _pauseButton.OnClickAsObservable().Subscribe(_ =>
        {
            Pause(!_isPause);
        }).AddTo(this);
        _timeSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            OnSliderValueChange.OnNext(x);
        }).AddTo(this);
        for (int i = 0; i < _timeJumpButtonList.Count; i++)
        {
            var jumpButton = _timeJumpButtonList[i];
            int index = i;
            jumpButton.OnClickMainButton.Subscribe(timeRate =>
            {
                OnTimeJump.OnNext(timeRate);
            });
            jumpButton.OnClickRegisterButton.Subscribe(_ =>
            {
                RegisterTime(index);
            });
        }
    }

    public void Pause(bool isPause)
    {
        _isPause = isPause;
        OnClickPauseButton.OnNext(_isPause);
        _timeUI.SetActive(_isPause);
    }

    public void MoveSlider(float value)
    {
        _timeSlider.value += value;
    }

    public void SetSlider(float value)
    {
        _timeSlider.value = value;
    }

    public void RegisterTime(int index)
    {
        var jumpButton = _timeJumpButtonList[index];
        jumpButton.SetTimeRate(_timeSlider.value);

        int number = index + 1;
        foreach (var buttonIcon in _buttonIconList)
        {
            if (buttonIcon.Number == number)
            {
                Destroy(buttonIcon.gameObject);
            }
        }

        var icon = Instantiate(ResourceManager.LoadPrefab<JumpButtonIcon>(_buttonIconPrefabPath), _buttonIconArea);
        icon.SetNumber(number);
        icon.transform.SetAnchoredPositionX(_sliderWidth * _timeSlider.value);
        _buttonIconList.Add(icon);
    }
}
