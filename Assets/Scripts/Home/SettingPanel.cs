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
    [SerializeField] private InputField _offsetSetting;
    [SerializeField] private Button _deleteDataButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _bgButton;

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
        _offsetSetting.text = GameManager.instance.SettingOffset.ToString();
        _offsetSetting.OnValueChangedAsObservable().Subscribe(x =>
        {
            GameManager.instance.SettingOffset = float.Parse(x);
        });
        _deleteDataButton.OnClickAsObservable().Subscribe(_ =>
        {
            // データ削除
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

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        SaveDataManager.UpdateSettingData(_bgmSlider.value, _seSlider.value);
    }
}
