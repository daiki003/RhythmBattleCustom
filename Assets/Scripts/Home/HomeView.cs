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
            _ => MasterManager.MinCustomLevelId
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
    public string StageId;
    public int Level;
    public GetStageKey(string stageId, int level)
    {
        StageId = stageId;
        Level = level;
    }
}

public class HomeView : MonoBehaviour
{
    [SerializeField] private Text _totalScoreText;
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
    private int _currentLevel => _currentPanelType.GetLevel() < MasterManager.MinCustomLevelId ? _currentPanelType.GetLevel() : _selectedStrip.LevelId;

    public void Init(List<StageInfo> stageList, int lastLevel)
    {
        CreateStripList(stageList);
        SetButtonInteractable(false);
        _practiceModeToggle.isOn = false;
        for (int i = 0; i < _menuButtonList.Count; i++)
        {
            var menuButton = _menuButtonList[i];
            menuButton.OnWhenClicked.Subscribe(_ =>
            {
                SetLevelPanel(menuButton.ButtonType);
            });
            if (i == lastLevel - 1)
            {
                SetLevelPanel(menuButton.ButtonType);
            }
        }
        if (lastLevel >= MasterManager.MinCustomLevelId)
        {
            SetLevelPanel(HomePanelType.Custom);
        }
        BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        // 開始ボタン
        _playStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _clickPlayStageButton.OnNext((new GetStageKey(_selectedStrip.StageId, _currentLevel), _practiceModeToggle.isOn));
        }).AddTo(this);
        // 編集ボタン
        _editStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            _clickEditStageButton.OnNext(new GetStageKey(_selectedStrip.StageId, _currentLevel));
        }).AddTo(this);
        // 削除ボタン
        _deleteStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            DeleteStage();
        }).AddTo(this);
        // 新規ステージ作成ボタン
        _newCreateButton.OnClickAsObservable().Subscribe(_ =>
        {
            CreateStage();
        }).AddTo(this);

        // 設定ボタン
        _settingButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.OpenSettingDialog();
        }).AddTo(this);
        _helpButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.OpenHelpDialog(HelpDialogPageType.Home);
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
    public void CreateStripList(List<StageInfo> stageList)
    {
        DestroyAllStrip();
        foreach (var stageInfo in stageList)
        {
            CreateStageStrip(stageInfo.StageHeader, level: 0, _stripTransform);
            var customStageList = stageInfo.LevelList.Where(l => l.Level >= MasterManager.MinCustomLevelId);
            foreach (var customLevel in customStageList)
            {
                CreateStageStrip(stageInfo.StageHeader, level: customLevel.Level, _customStripTransform, customLevel.StageNameOverride);
            }
        }
        _totalScoreText.text = SaveDataManager.GetTotalScore().ToString();
    }

    // ステージの短冊1枚を作成
    private StageStrip CreateStageStrip(StageHeader stageHeader, int level, Transform transform, string overrideName = "")
    {
        var strip = Instantiate(_stageStripPrefab, transform);
        _stageStripList.Add(strip);
        strip.Init(stageHeader, level, overrideName);
        strip.OnClickedStrip.Subscribe(_ =>
        {
            if (_selectedStrip != strip)
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                BGMManager.instance.SetClip(strip.StageId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
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
                await MasterManager.DeleteCustomStage(_selectedStrip.StageId, _currentLevel);
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
        });
    }

    private void CreateStage()
    {
        CancelSelectStrip();
        var option = new DialogOptionBase
        {
            TitleText = "ステージ選択",
            OkButtonText = "作成",
            CancelButtonText = "キャンセル"
        };
        var dialog = DialogManager.instance.CreateDialog<NewCreateListDialog>(DialogManager.NewCreateListDialogPrefabName, option);
        dialog.OnCloseDialog.Subscribe(result =>
        {
            if (result is NewCreateDialogResult dialogResult)
            {
                if (dialogResult.ResultType == DialogResultType.Ok)
                {
                    _clickNewCreateStageButton.OnNext(dialogResult.SelectedStageId);
                }
                else
                {
                    CancelSelectStrip();
                }
            }
        });
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
