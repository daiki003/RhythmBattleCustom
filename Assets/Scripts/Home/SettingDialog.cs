using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class SettingDialog : DialogBase
{
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private ValueAdjuster _offsetAdjuster;
    [SerializeField] private ValueAdjuster _speedAdjuster;
    [SerializeField] private Button _creditButton;
    [SerializeField] private Slider _targetSlider;

    private const float _changeOffsetUnit = 0.01f;
    private const float _changeSpeedUnit = 0.1f;

    private const string _creditMessage = 
        "・音楽素材\n" +
        "  ・ユーフルカ 様\n" +
        "  ・魔王魂 様\n" +
        "  ・効果音ラボ 様\n" +
        "\n" +
        "・画像素材\n" +
        "  ・七三ゆきのアトリエ 様\n" +
        "  ・ARCEY 様\n";

    private MessageDialogOption _creditDialogOption = new()
    {
        TitleText = "クレジット",
        MessageText = _creditMessage,
        HideOkButton = true,
        CancelButtonText = "閉じる"
    };

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        _bgmSlider.value = SaveDataManager.SettingData.BgmVolume;
        _bgmSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            BGMManager.instance.AdjustVolume(x);
        }).AddTo(this);

        _seSlider.value = SaveDataManager.SettingData.SeVolume;
        _seSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            SEManager.instance.AdjustVolume(x);
        }).AddTo(this);

        _offsetAdjuster.Init(SaveDataManager.SettingData.Offset, _changeOffsetUnit);
        _speedAdjuster.Init(SaveDataManager.SettingData.BallSpeed, _changeSpeedUnit);

        _creditButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.CreditDialogPrefabName, _creditDialogOption);
        }).AddTo(this);

        _targetSlider.value = SaveDataManager.SettingData.Target;
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        SaveDataManager.UpdateSettingData(_bgmSlider.value, _seSlider.value, _offsetAdjuster.CurrentValue.Value, _speedAdjuster.CurrentValue.Value, _targetSlider.value);
        base.ClosePanel(resultType);
    }
}
