using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageModulation : ControlPanelPageBase
{
    [SerializeField] private ValueAdjuster _beatsAdjuster;
    [SerializeField] private InputField _measureInput;
    [SerializeField] private Button _modulationForwardButton;
    [SerializeField] private Button _modulationBackButton;
    [SerializeField] private Button _modulationResetButton;

    public override string PageName => "拍子";

    private ReactiveProperty<int> _beatsNumber = new(0);

    public override void Init()
    {
        _beatsNumber.Subscribe(value =>
        {
            _beatsAdjuster.SetValue(value);
        }).AddTo(this);

        _beatsAdjuster.Init(_beatsNumber.Value, 1);
        _beatsAdjuster.CurrentValue.Subscribe(value =>
        {
            _beatsNumber.Value = (int)value;
            _onRequest.OnNext(new ControlPanelRequestBeatsNumber(_beatsNumber.Value));
        }).AddTo(this);
        _modulationForwardButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (int.TryParse(_measureInput.text, out int measure))
            {
                _onRequest.OnNext(new ControlPanelRequestModulationChange(measure, isForward: true));
            }
        }).AddTo(this);
        _modulationBackButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (int.TryParse(_measureInput.text, out int measure))
            {
                _onRequest.OnNext(new ControlPanelRequestModulationChange(measure, isForward: false));
            }
        }).AddTo(this);
        _modulationResetButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestResetModulation());
        }).AddTo(this);
    }

    public override void SetParameter(StageHeader stageHeader)
    {
        _beatsNumber.Value = stageHeader.BeatsNumber;
    }
}
