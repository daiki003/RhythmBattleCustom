using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelPageBpm : ControlPanelPageBase
{
    [SerializeField] private ValueAdjuster _bpmInput;
    [SerializeField] private ValueAdjuster _startTimeInput;
    [SerializeField] private ValueAdjuster _endTimeInput;
    [SerializeField] private Button _estimateBpmButton;

    private ReactiveProperty<float> _bpm = new(0f);
    private ReactiveProperty<float> _startTime = new(0f);
    private ReactiveProperty<float> _endTime = new(0f);

    public override void Init()
    {
        _bpm.Subscribe(value =>
        {
            _bpmInput.SetValue(value);
            _onRequest.OnNext(new ControlPanelRequestChangeBpm(value));
        }).AddTo(this);
        _startTime.Subscribe(value =>
        {
            _startTimeInput.SetValue(value);
            _onRequest.OnNext(new ControlPanelRequestChangeStartTime(value));
        }).AddTo(this);
        _endTime.Subscribe(value =>
        {
            _endTimeInput.SetValue(value);
            _onRequest.OnNext(new ControlPanelRequestChangeEndTime(value));
        }).AddTo(this);

        _bpmInput.Init(_bpm.Value, 0.1f);
        _bpmInput.CurrentValue.Subscribe(value =>
        {
            _bpm.Value = value;
        }).AddTo(this);;
        _startTimeInput.Init(_startTime.Value, 0.1f);
        _startTimeInput.CurrentValue.Subscribe(value =>
        {
            _startTime.Value = value;
        }).AddTo(this);;
        _endTimeInput.Init(_endTime.Value, 0.1f);
        _endTimeInput.CurrentValue.Subscribe(value =>
        {
            _endTime.Value = value;
        }).AddTo(this);;
        _estimateBpmButton.OnClickAsObservable().Subscribe(async _ =>
        {
            var audioClip = BGMManager.instance.CurrentClip;
            // EstimateBPMに時間がかかるので先にSEを鳴らす
            SEManager.instance.PlaySe(SeName.Button1);
            _onRequest.OnNext(new ControlPanelRequestStartEstimate());
            await UniTask.NextFrame();
            var bpm = AudioClipUtility.EstimateBPM(audioClip);
            var startTime = AudioClipUtility.GetStartSoundTime(audioClip);
            _bpmInput.SetValue(bpm);
            _startTimeInput.SetValue(startTime);
            _onRequest.OnNext(new ControlPanelRequestFinishEstimate());
        }).AddTo(this);
    }

    public override void SetMusicParameter(MusicParameter musicParameter)
    {
        _bpm.Value = musicParameter.Bpm;
        _startTime.Value = musicParameter.StartTime;
        _endTime.Value = musicParameter.EndTime;
    }
}
