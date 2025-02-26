using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private Button _seTestButton;
    [SerializeField] private Text _offsetText;
    [SerializeField] private Button _minusOffsetButton;
    [SerializeField] private Button _plusOffsetButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _bgButton;

    private float _currentOffset;
    private const float _changeOffsetUnit = 0.01f;

    public void Init()
    {
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
        _seTestButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
        });

        _currentOffset = SaveDataManager.SettingData.Offset;
        _offsetText.text = _currentOffset.ToString();
        _minusOffsetButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeOffset(-1 * _changeOffsetUnit);
        });
        _plusOffsetButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeOffset(_changeOffsetUnit);
        });

        _closeButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel();
        });
        _bgButton.OnClickAsObservable().Subscribe(_ =>
        {
            ClosePanel();
        });
    }

    private void ChangeOffset(float diff)
    {
        _currentOffset += diff;
        _offsetText.text = string.Format("{0:F2}", _currentOffset);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        SaveDataManager.UpdateSettingData(_bgmSlider.value, _seSlider.value, _currentOffset);
    }
}
