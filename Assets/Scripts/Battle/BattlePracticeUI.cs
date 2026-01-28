using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using R3;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattlePracticeUI : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private CustomButton _autoButton;
    [SerializeField] private Button _helpButton;
    [SerializeField] private Button _scoreResetButton;
    [SerializeField] private GameObject _timeUI;
    [SerializeField] private List<TimeJumpButton> _timeJumpButtonList = new();
    [SerializeField] private Transform _buttonIconArea;
    [SerializeField] private Slider _timeSlider;

    private bool _isPause;
    public bool IsAuto { get; private set; }
    private const string _buttonIconPrefabPath = "JumpButtonIcon";
    private const float _sliderWidth = 780;
    private Dictionary<int, JumpButtonIcon> _buttonIconDict = new();

    private Subject<bool> _onClickPauseButton = new();
    public Observable<bool> OnClickPauseButton => _onClickPauseButton;
    private Subject<bool> _onClickScoreReset = new();
    public Observable<bool> OnClickScoreReset => _onClickScoreReset;
    private Subject<bool> _onClickHelpButton = new();
    public Observable<bool> OnClickHelpButton => _onClickHelpButton;
    public Subject<float> OnSliderValueChange = new();
    public Subject<float> OnTimeJump = new();

    public float SliderValue => _timeSlider.value;

    public void Init()
    {
        _isPause = false;
        _timeUI.gameObject.SetActive(false);
        _pauseButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickPauseButton.OnNext(!_isPause);
        }).AddTo(this);
        _autoButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeAuto(!IsAuto);
        }).AddTo(this);
        _helpButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickHelpButton.OnNext(true);
        }).AddTo(this);
        _scoreResetButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickScoreReset.OnNext(default);
        }).AddTo(this);
        _timeSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            OnSliderValueChange.OnNext(x);
        }).AddTo(this);
        for (int i = 0; i < _timeJumpButtonList.Count; i++)
        {
            var jumpButton = _timeJumpButtonList[i];
            int index = i;
            jumpButton.Init(index);
            jumpButton.OnClickMainButton.Subscribe(timeRate =>
            {
                OnTimeJump.OnNext(timeRate);
            }).AddTo(this);
            jumpButton.OnClickRegisterButton.Subscribe(_ =>
            {
                RegisterTime(index);
            }).AddTo(this);
            jumpButton.OnClickDeleteButton.Subscribe(_ =>
            {
                DeleteTime(index);
            }).AddTo(this);
            if (i == 0)
            {
                // 1つ目のボタンはチュートリアルマスク用に登録
                jumpButton.RegisterForTutorial();
            }
        }
    }

    public void OnPause(bool isPause)
    {
        _isPause = isPause;
        _timeUI.SetActive(_isPause);
    }

    public void ChangeAuto(bool isAuto)
    {
        IsAuto = isAuto;
        _autoButton.SetHighLight(IsAuto);
    }

    public void MoveSlider(float value)
    {
        SetSlider(Math.Max(0, _timeSlider.value + value));
    }

    public void SetSlider(float value)
    {
        _timeSlider.value = value;
    }

    public void RegisterTime(int index)
    {
        var jumpButton = _timeJumpButtonList[index];
        jumpButton.SetTimeRate(_timeSlider.value);

        if (_buttonIconDict.TryGetValue(index, out var icon))
        {
            icon.transform.SetAnchoredPositionX(_sliderWidth * _timeSlider.value);
        }
        else
        {
            var newIcon = Instantiate(ResourceManager.LoadPrefab<JumpButtonIcon>(_buttonIconPrefabPath), _buttonIconArea);
            newIcon.SetNumber(index + 1);
            newIcon.transform.SetAnchoredPositionX(_sliderWidth * _timeSlider.value);
            _buttonIconDict[index] = newIcon;
        }
    }

    private void DeleteTime(int index)
    {
        var jumpButton = _timeJumpButtonList[index];
        jumpButton.SetTimeRate(-1);
        var icon = _buttonIconDict[index];
        _buttonIconDict.Remove(index);
        icon.Destroy();
    }
}
