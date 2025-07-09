using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum HomePanelType
{
    Level1,
    Level2,
    Level3,
    Custom,
}

public static class HomePanelTypeExtension
{
    public static int GetLevel(this HomePanelType panelType)
    {
        return panelType switch
        {
            HomePanelType.Level1 => 1,
            HomePanelType.Level2 => 2,
            HomePanelType.Level3 => 3,
            _ => MasterManager.MinStageId
        };
    }

    public static bool IsStage(this HomePanelType panelType)
    {
        return panelType switch
        {
            HomePanelType.Level1 or
            HomePanelType.Level2 or
            HomePanelType.Level3 => true,
            HomePanelType.Custom => false,
            _ => false
        };
    }
}

public class GetStageKey
{
    public string MusicId;
    public int StageId;
    public GetStageKey(string musicId, int stageId)
    {
        MusicId = musicId;
        StageId = stageId;
    }
}

public class HomeView : MonoBehaviour
{
    [SerializeField] private Text _achievementRateText;
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private Transform _stripTransform;
    [SerializeField] private Transform _customStripTransform;
    [SerializeField] private List<MenuButton> _menuButtonList;
    [SerializeField] private GameObject _stageStripPanel;
    [SerializeField] private GameObject _customStripPanel;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _helpButton;

    [SerializeField] private Button _playStageButton;
    [SerializeField] private Toggle _practiceModeToggle;
    [SerializeField] private Button _editStageButton;
    [SerializeField] private Button _deleteStageButton;
    [SerializeField] private Button _newCreateButton;

    private List<StageStrip> _stageStripList = new List<StageStrip>();

    private Subject<(GetStageKey stageKey, bool isPractice)> _clickPlayStageButton = new();
    public Observable<(GetStageKey stageKey, bool isPractice)> ClickPlayStageButton => _clickPlayStageButton;
    private Subject<GetStageKey> _clickEditStageButton = new();
    public Observable<GetStageKey> ClickEditStageButton => _clickEditStageButton;
    private Subject<string> _clickNewCreateStageButton = new();
    public Observable<string> ClickNewCreateStageButton => _clickNewCreateStageButton;

    private StageStrip _selectedStrip;
    private HomePanelType _currentPanelType;

    public void Init(List<SingleStageMaster> stageList, int lastLevel)
    {
        CreateStripList(stageList);
        SetButtonInteractable(false);
        for (int i = 0; i < _menuButtonList.Count; i++)
        {
            var menuButton = _menuButtonList[i];
            menuButton.OnWhenClicked.Subscribe(_ =>
            {
                SetLevelPanel(menuButton.ButtonType);
            }).AddTo(this);
            if (i == lastLevel - 1)
            {
                SetLevelPanel(menuButton.ButtonType);
            }
        }
        if (lastLevel >= MasterManager.MinStageId)
        {
            SetLevelPanel(HomePanelType.Custom);
        }
        BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        // 開始ボタン
        _playStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _clickPlayStageButton.OnNext((new GetStageKey(_selectedStrip.MusicIdId, _selectedStrip.StageId), _practiceModeToggle.isOn));
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
        // 編集ボタン
        _editStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _clickEditStageButton.OnNext(new GetStageKey(_selectedStrip.MusicIdId, _selectedStrip.StageId));
        }).AddTo(this);
        // 削除ボタン
        _deleteStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            DeleteStage();
        }).AddTo(this);
        // 新規ステージ作成ボタン
        _newCreateButton.OnClickAsObservable().Subscribe(_ =>
        {
            CreateStage().Forget();
        }).AddTo(this);

        // 設定ボタン
        _settingButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.OpenSettingDialog();
        }).AddTo(this);
        _helpButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.OpenHelpDialog(_currentPanelType.IsStage() ? HelpDialogPageType.Home : HelpDialogPageType.Home2);
        }).AddTo(this);
    }

    public void UpdateStrip()
    {
        foreach (var strip in _stageStripList)
        {
            strip.UpdateScore(_currentPanelType.GetLevel());
        }
    }

    // ステージの短冊を全て作成
    public void CreateStripList(List<SingleStageMaster> stageList)
    {
        DestroyAllStrip();
        foreach (var stageInfo in stageList)
        {
            CreateStageStrip(stageInfo.StageHeader, stageInfo.StageId, _customStripTransform);
        }
        float achievementRate = SaveDataManager.CalculateAchievementRate().RoundDown(1);
        _achievementRateText.text = (achievementRate >= 100f ? achievementRate.ToString() : achievementRate.ToString("F1")) + "%" ;
    }

    // ステージの短冊1枚を作成
    private StageStrip CreateStageStrip(StageHeader stageHeader, int stageId, Transform transform)
    {
        var strip = Instantiate(_stageStripPrefab, transform);
        _stageStripList.Add(strip);
        strip.Init(stageHeader, stageId);
        strip.OnClickedStrip.Subscribe(async _ =>
        {
            if (_selectedStrip != strip)
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                await BGMManager.instance.SetClipFromLibrary(strip.MusicIdId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
                SetButtonInteractable(true);
            }
            else
            {
                CancelSelectStrip();
            }
        }).AddTo(strip);
        return strip;
    }

    private void CancelSelectStrip()
    {
        if (_selectedStrip != null)
        {
            _selectedStrip.SetSelected(false);
            _selectedStrip = null;
            BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
            SetButtonInteractable(false);
        }
    }

    private void DestroyAllStrip()
    {
        while (_stageStripList.Count > 0)
        {
            var strip = _stageStripList[0];
            DestroyStrip(strip);
        }
    }

    private void DestroyStrip(StageStrip strip)
    {
        _stageStripList.Remove(strip);
        Destroy(strip.gameObject);
    }

    private void DeleteStage()
    {
        // 確認ダイアログ
        var option = new MessageDialogOption
        {
            TitleText = "ステージ削除",
            MessageText = "本当に削除しますか？",
            OkButtonText = "削除",
            CancelButtonText = "キャンセル"
        };
        var dialog = DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
        dialog.OnCloseDialog.Subscribe(async result =>
        {
            if (result.ResultType == DialogResultType.Ok)
            {
                await MasterManager.DeleteCustomStage(_selectedStrip.MusicIdId, _selectedStrip.StageId);
                // 短冊の選択をキャンセルしてからを削除
                var selectedStrip = _selectedStrip;
                CancelSelectStrip();
                DestroyStrip(selectedStrip);
                // 削除通知ダイアログ
                var option = new MessageDialogOption
                {
                    TitleText = "ステージ削除",
                    MessageText = "削除しました",
                    OkButtonText = "OK",
                    HideCancelButton = true
                };
                var dialog = DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
            }
        }).AddTo(this);
    }

    private async UniTask CreateStage()
    {
        CancelSelectStrip();
        string musicId = await MediaController.instance.MusicExpote();
        if (string.IsNullOrEmpty(musicId))
        {
            // 曲選択がキャンセルされた場合は何もしない
            return;
        }
        _clickNewCreateStageButton.OnNext(musicId);
    }

    private void SetButtonInteractable(bool isActive)
    {
        _playStageButton.interactable = isActive;
        _editStageButton.interactable = isActive;
        _deleteStageButton.interactable = isActive;
    }

    private void SetLevelPanel(HomePanelType titlePanelType)
    {
        foreach (var button in _menuButtonList)
        {
            button.SetLight(button.ButtonType == titlePanelType);
        }
        if (_currentPanelType.IsStage() != titlePanelType.IsStage())
        {
            CancelSelectStrip();
        }
        _currentPanelType = titlePanelType;
        bool isStage = titlePanelType.IsStage();
        // _editStageButton.gameObject.SetActive(!isStage);
        _deleteStageButton.gameObject.SetActive(!isStage);
        _newCreateButton.gameObject.SetActive(!isStage);
        _stageStripPanel.SetActive(titlePanelType.IsStage());
        _customStripPanel.SetActive(!titlePanelType.IsStage());
        UpdateStrip();
    }
}
