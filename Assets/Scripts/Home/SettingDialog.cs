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

    private const float _changeOffsetUnit = 0.01f;
    private const float _changeSpeedUnit = 0.1f;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        _bgmSlider.value = SaveDataManager.SettingData.BgmVolume;
        _bgmSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            BGMManager.instance.AdjustVolume(x);
        });

        _seSlider.value = SaveDataManager.SettingData.SeVolume;
        _seSlider.OnValueChangedAsObservable().Subscribe(x =>
        {
            SEManager.instance.AdjustVolume(x);
        });

        _offsetAdjuster.Init(SaveDataManager.SettingData.Offset, _changeOffsetUnit);
        _speedAdjuster.Init(SaveDataManager.SettingData.BallSpeed, _changeSpeedUnit);
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        SaveDataManager.UpdateSettingData(_bgmSlider.value, _seSlider.value, _offsetAdjuster.CurrentValue, _speedAdjuster.CurrentValue);
        base.ClosePanel(resultType);
    }
}
