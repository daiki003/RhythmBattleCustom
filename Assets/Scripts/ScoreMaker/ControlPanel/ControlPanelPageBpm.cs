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
    [SerializeField] private GameObject _messageMask;
    [SerializeField] private Text _messageText;

    private ReactiveProperty<float> _bpm = new(0f);
    private ReactiveProperty<float> _startTime = new(0f);
    private ReactiveProperty<float> _endTime = new(0f);

    public override string PageName => "テンポ";
    private const int _minDecimalPlaces = 2;

    public override void Init()
    {
        _bpm.Subscribe(value =>
        {
            _onRequest.OnNext(new ControlPanelRequestChangeBpm(value));
        }).AddTo(this);
        _startTime.Subscribe(value =>
        {
            _onRequest.OnNext(new ControlPanelRequestChangeStartTime(value));
        }).AddTo(this);
        _endTime.Subscribe(value =>
        {
            _onRequest.OnNext(new ControlPanelRequestChangeEndTime(value));
        }).AddTo(this);

        _bpmInput.Init(_bpm.Value, 0.1f, _minDecimalPlaces);
        _bpmInput.CurrentValue.Subscribe(value =>
        {
            _bpm.Value = value;
        }).AddTo(this);;
        _startTimeInput.Init(_startTime.Value, 0.1f, _minDecimalPlaces);
        _startTimeInput.CurrentValue.Subscribe(value =>
        {
            _startTime.Value = value;
        }).AddTo(this);;
        _endTimeInput.Init(_endTime.Value, 0.1f, _minDecimalPlaces);
        _endTimeInput.CurrentValue.Subscribe(value =>
        {
            _endTime.Value = value;
        }).AddTo(this);;
        _estimateBpmButton.OnClickAsObservable().Subscribe(async _ =>
        {
            var audioClip = BGMManager.instance.CurrentClip;
            // EstimateBPMに時間がかかるので先にSEを鳴らす
            SEManager.instance.PlaySe(SeName.Button1);

            // ライフ消費確認
            // var isConsumed = await DialogManager.instance.ConfirmConsumeLifeDialogAsync(
            //     title: "BPM計測",
            //     message: "BPM計測にはライフを1つ消費します。\n計測しますか？",
            //     buttonText: "計測する",
            //     shortageText: "BPMを計測しますか？"
            // );
            // if (!isConsumed) return;

            // 広告再生確認
            var dialogResult = await DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(
                new MessageDialogOption
                {
                    TitleText = "BPM計測",
                    MessageText = "BPMを計測しますか？\n（計測中に広告が流れます）",
                    OkButtonText = "計測する",
                }
            );
            if (dialogResult.ResultType != DialogResultType.Ok) return;
            var adsTask = AdsManager.ShowInterstitialAsync();
            SetMessageMask(true, "計測中...");
            await UniTask.NextFrame();
            var bpm = AudioClipUtility.EstimateBPM(audioClip);
            var startTime = AudioClipUtility.GetStartSoundTime(audioClip);
            _bpmInput.SetValue(bpm);
            _startTimeInput.SetValue(startTime);
            SetMessageMask(false);
            await adsTask;
            await DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(
                new MessageDialogOption
                {
                    TitleText = "BPM計測",
                    MessageText = "BPMの計測が完了しました。",
                    OkButtonText = "OK",
                    HideCancelButton = true
                }
            );
        }).AddTo(this);

        SetMessageMask(false);
    }

    public override void SetParameter(StageHeader stageHeader, bool isTutorial)
    {
        _bpmInput.SetValue(stageHeader.BPM);
        _startTimeInput.SetValue(stageHeader.StartTime);
        _endTimeInput.SetValue(stageHeader.EndTime);
        bool canEstimate = stageHeader.IsMyMusic || isTutorial;
        _estimateBpmButton.gameObject.SetActive(canEstimate);
    }

    private void SetMessageMask(bool isActive, string message = "")
    {
        _messageMask.SetActive(isActive);
        if (isActive)
        {
            _messageText.text = message;
        }
    }
}
