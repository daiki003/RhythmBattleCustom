using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

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

public class HomeManager : MonoBehaviour
{
    [SerializeField] private Text _totalScoreText;
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private Transform _stripTransform;
    [SerializeField] private Transform _customStripTransform;
    [SerializeField] private List<MenuButton> _menuButtonList;
    [SerializeField] private GameObject _stageStripPanel;
    [SerializeField] private GameObject _customStripPanel;
    [SerializeField] private Button _settingButton;
    [SerializeField] private SettingPanel _settingPanel;

    [SerializeField] private Button _playStageButton;
    [SerializeField] private Button _practiceStageButton;
    [SerializeField] private Button _editStageButton;
    [SerializeField] private Button _deleteStageButton;
    [SerializeField] private Button _newCreateButton;

    private List<StageStrip> _stageStripList = new List<StageStrip>();

    private StageStrip _selectedStrip;
    private int _currentLevel;

    public void Init(int lastLevel)
    {
        CreateStripList();
        DarkeningMenuButton();
        for (int i = 0; i < _menuButtonList.Count; i++)
        {
            var menuButton = _menuButtonList[i];
            menuButton.OnWhenClicked.Subscribe(_ =>
            {
                DarkeningMenuButton();
                menuButton.OnClick();
                SetLevelPanel(menuButton.ButtonType);
            });
            if (i == lastLevel - 1)
            {
                menuButton.SetLight(true);
                SetLevelPanel(menuButton.ButtonType);
            }
        }
        if (lastLevel >= MasterManager.MinCustomLevelId)
        {
            SetLevelPanel(HomePanelType.Custom);
        }
        BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        _playStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartBattle(isPractice: false);
        }).AddTo(this);
        _practiceStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartBattle(isPractice: true);
        }).AddTo(this);
        _editStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartScoreMaker(_selectedStrip.StageId, isNewCreate: false);
        }).AddTo(this);
        _deleteStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            // ステージ削除
        }).AddTo(this);
        _newCreateButton.OnClickAsObservable().Subscribe(_ =>
        {
            var option = new DialogOptionBase
            {
                TitleText = "ステージ選択",
                OkButtonText = "作成",
                CancelButtonText = "キャンセル"
            };
            var dialog = DialogManager.instance.CreateDialog<NewCreateListDialog>("NewCreateListDialog", option);
            dialog.OnCloseDialog.Subscribe(result =>
            {
                if (result is NewCreateDialogResult dialogResult)
                {
                    if (dialogResult.ResultType == DialogResultType.Ok)
                    {
                        StartScoreMaker(dialogResult.SelectedStageId, isNewCreate: true);
                    }
                    else
                    {
                        CancelSelectStrip();
                    }
                }
            });
        }).AddTo(this);

        _settingPanel.Init();
        _settingPanel.gameObject.SetActive(false);
        _settingButton.OnClickAsObservable().Subscribe(_ =>
        {
            _settingPanel.gameObject.SetActive(true);
        }).AddTo(this);
    }

    public void UpdateStrip()
    {
        foreach (var strip in _stageStripList)
        {
            strip.UpdateScore(_currentLevel);
        }
    }

    public void CreateStripList()
    {
        DestroyAllStrip();
        for (int i = 0; i < MasterManager.StageMasterList.Count; i++)
        {
            var stageMaster = MasterManager.StageMasterList[i];
            var strip = CreateStageStrip(stageMaster.StageHeader, _stripTransform);
            strip.SetLevelAndId(stageMaster.StageId, levelId: 0);
        }
        for (int i = 0; i < MasterManager.CustomStageList.Count; i++)
        {
            var stageMaster = MasterManager.CustomStageList[i];
            var strip = CreateStageStrip(stageMaster.StageHeader, _customStripTransform);
            strip.SetLevelAndId(stageMaster.StageId, stageMaster.LevelId);
        }
        _totalScoreText.text = SaveDataManager.GetTotalScore().ToString();
    }

    private StageStrip CreateStageStrip(StageHeader stageHeader, Transform parent)
    {
        var strip = Instantiate(_stageStripPrefab, parent);
        _stageStripList.Add(strip);
        strip.Init(stageHeader);
        strip.OnClickedStrip.Subscribe(_ =>
        {
            if (_selectedStrip != strip)
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                BGMManager.instance.SetClip(strip.StageId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
            }
            SetButtonInteractable(_selectedStrip != null);
        }).AddTo(strip);
        SetButtonInteractable(false);
        return strip;
    }

    private void CancelSelectStrip()
    {
        if (_selectedStrip != null)
        {
            _selectedStrip.SetSelected(false);
            _selectedStrip = null;
            BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        }
    }

    public void DestroyAllStrip()
    {
        while (_stageStripList.Count > 0)
        {
            var strip = _stageStripList[0];
            _stageStripList.RemoveAt(0);
            Destroy(strip.gameObject);
        }
    }

    private void SetButtonInteractable(bool isActive)
    {
        _playStageButton.interactable = isActive;
        _practiceStageButton.interactable = isActive;
        _editStageButton.interactable = isActive;
        _deleteStageButton.interactable = isActive;
    }

    private void DarkeningMenuButton()
    {
        foreach (MenuButton menuButton in _menuButtonList)
        {
            menuButton.SetLight(false);
        }
    }

    private void SetLevelPanel(HomePanelType titlePanelType)
    {
        _currentLevel = titlePanelType.GetLevel();
        bool isStage = titlePanelType.IsStage();
        _practiceStageButton.gameObject.SetActive(isStage);
        // _editStageButton.gameObject.SetActive(!isStage);
        _deleteStageButton.gameObject.SetActive(!isStage);
        _newCreateButton.gameObject.SetActive(!isStage);
        _stageStripPanel.SetActive(titlePanelType.IsStage());
        _customStripPanel.SetActive(!titlePanelType.IsStage());
        UpdateStrip();
        if (!titlePanelType.IsStage())
        {
            CancelSelectStrip();
        }
    }

    private void StartBattle(bool isPractice)
    {
        int level = _currentLevel < MasterManager.MinCustomLevelId ? _currentLevel : _selectedStrip.LevelId;
        var sceneInfo = new BattleSceneInfo
        {
            StageMaster = MasterManager.GetSingleStageMaster(_selectedStrip.StageId, level),
            IsPractice = isPractice
        };
        SEManager.instance.PlayBattleStartSe();
        GameManager.instance.OpenScene(SceneType.Battle, sceneInfo).Forget();
    }

    private void StartScoreMaker(string stageId, bool isNewCreate)
    {
        var sceneInfo = new ScoreMakerSceneInfo
        {
            StageId = stageId,
            FirstLevel = isNewCreate ? 1 : _currentLevel,
            IsNewCreate = isNewCreate
        };
        SEManager.instance.PlayButtonSe();
        GameManager.instance.OpenScene(SceneType.ScoreMaker, sceneInfo).Forget();
    }
}
