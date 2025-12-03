using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HomeViewのユーザー操作のインターフェースクラス
/// </summary>
public class HomeViewInput : MonoBehaviour
{
    [SerializeField] private TabGroup _tabGroup;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _helpButton;

    [SerializeField] private Button _playStageButton;
    [SerializeField] private Toggle _practiceModeToggle;
    [SerializeField] private Button _editStageButton;
    [SerializeField] private Text _editButtonText;
    [SerializeField] private Button _deleteStageButton;
    [SerializeField] private Button _newCreateButton;

    private const string _editText = "編集";
    private const string _copyText = "コピー";

    private Subject<HomeViewButtonEventArgs> _onClickButton = new();
    public Observable<HomeViewButtonEventArgs> OnClickButton => _onClickButton;

    public void Init(HomePanelType firstPanelType)
    {
        _tabGroup.OnTabSelected.Subscribe(index =>
        {
            _onClickButton.OnNext(new MenuButtonArgs { PanelType = (HomePanelType)index });
        }).AddTo(this);
        _tabGroup.Init((int)firstPanelType);

        _playStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickButton.OnNext(new PlayStageButtonArgs { IsPracticeMode = _practiceModeToggle.isOn });
        }).AddTo(this);

        _editStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickButton.OnNext(new EditStageButtonArgs());
        }).AddTo(this);

        _deleteStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickButton.OnNext(new DeleteStageButtonArgs());
        }).AddTo(this);

        _newCreateButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickButton.OnNext(new NewCreateButtonArgs());
        }).AddTo(this);

        _settingButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickButton.OnNext(new SettingButtonArgs());
        }).AddTo(this);

        _helpButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onClickButton.OnNext(new HelpButtonArgs());
        }).AddTo(this);

        // 練習モード切替
        bool enableToggleSe = false;
        _practiceModeToggle.isOn = false;
        _practiceModeToggle.OnValueChangedAsObservable().Subscribe(isOn =>
        {
            // 初回は音を鳴らさない
            if (enableToggleSe)
            {
                SEManager.instance.PlaySe(isOn ? SeName.Button2 : SeName.Cancel);
            }
            enableToggleSe = true;
        }).AddTo(this);

        SetButtonInteractable(false);
    }

    public void ChangeButtonByCustomMode(bool isCustom)
    {
        _deleteStageButton.gameObject.SetActive(isCustom);
        _newCreateButton.gameObject.SetActive(isCustom);
        _editButtonText.text = isCustom ? _editText : _copyText;
    }

    public void SetButtonInteractable(bool isInteractable)
    {
        _playStageButton.interactable = isInteractable;
        _editStageButton.interactable = isInteractable;
        _deleteStageButton.interactable = isInteractable;
    }
}

public abstract class HomeViewButtonEventArgs {}

// 各ボタンの実装
public class MenuButtonArgs : HomeViewButtonEventArgs
{
    public HomePanelType PanelType { get; set; }
}
public class PlayStageButtonArgs : HomeViewButtonEventArgs
{
    public bool IsPracticeMode { get; set; }
}

public class EditStageButtonArgs : HomeViewButtonEventArgs {}

public class DeleteStageButtonArgs : HomeViewButtonEventArgs {}

public class NewCreateButtonArgs : HomeViewButtonEventArgs {}

public class SettingButtonArgs : HomeViewButtonEventArgs {}

public class HelpButtonArgs : HomeViewButtonEventArgs {}