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
    [SerializeField] private InputField _beatInput;
    [SerializeField] private Button _modulationButton;
    [SerializeField] private Button _modulationResetButton;

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
        });
        _modulationButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (int.TryParse(_measureInput.text, out int measure) && int.TryParse(_beatInput.text, out int beat))
            {
                _onRequest.OnNext(new ControlPanelRequestModulation(measure, beat));
            }
        }).AddTo(this);
        _modulationResetButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestResetModulation());
        }).AddTo(this);
    }

    public override void SetMusicParameter(MusicParameter musicParameter)
    {
        _beatsNumber = new ReactiveProperty<int>(musicParameter.BeatsNumber);
    }
}
