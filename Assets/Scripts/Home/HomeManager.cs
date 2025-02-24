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
    Setting,
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
            _ => 1
        };
    }

    public static bool IsStage(this HomePanelType panelType)
    {
        return panelType switch
        {
            HomePanelType.Level1 or
            HomePanelType.Level2 or
            HomePanelType.Level3 => true,
            HomePanelType.Setting => false,
            _ => false
        };
    }
}

public class HomeManager : MonoBehaviour
{
    [SerializeField] private Text _totalScoreText;
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private Transform _stripTransform;
    [SerializeField] private List<MenuButton> _menuButtonList;
    [SerializeField] private GameObject _stageStripPanel;
    [SerializeField] private InputField _offsetSetting;
    [SerializeField] private Button _playStageButton;
    [SerializeField] private Button _practiceStageButton;
    [SerializeField] private Button _scoreMakerButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private SettingPanel _settingPanel;

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
        BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        _offsetSetting.text = GameManager.instance.SettingOffset.ToString();
        _offsetSetting.onValueChanged.AddListener(x =>
        {
            GameManager.instance.SettingOffset = float.Parse(x);
        });
        _playStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartBattle(isPractice: false);
        });
        _practiceStageButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartBattle(isPractice: true);
        });
        _scoreMakerButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartScoreMaker();
        });

        _settingPanel.Init();
        _settingPanel.gameObject.SetActive(false);
        _settingButton.OnClickAsObservable().Subscribe(_ =>
        {
            _settingPanel.gameObject.SetActive(true);
        });
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
            CreateStageStrip(stageMaster, _stripTransform);
        }
        _totalScoreText.text = SaveDataManager.GetTotalScore().ToString();
    }

    private void CreateStageStrip(StageMaster stageMaster, Transform parent)
    {
        var strip = Instantiate(_stageStripPrefab, parent);
        _stageStripList.Add(strip);
        strip.Init(stageMaster);
        strip.OnClickedStrip.Subscribe(stageId =>
        {
            if (_selectedStrip != strip)
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                BGMManager.instance.SetClip(strip.StageId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
            }
            else
            {
                _selectedStrip.SetSelected(false);
                _selectedStrip = null;
                BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
            }
        });
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
        _stageStripPanel.SetActive(titlePanelType.IsStage());
        UpdateStrip();
    }

    private void StartBattle(bool isPractice)
    {
        var sceneInfo = new BattleSceneInfo
        {
            StageMaster = MasterManager.GetSingleStageMaster(_selectedStrip.StageId, _currentLevel),
            IsPractice = isPractice
        };
        SEManager.instance.PlayBattleStartSe();
        GameManager.instance.OpenScene(SceneType.Battle, sceneInfo).Forget();
    }

    private void StartScoreMaker()
    {
        var sceneInfo = new ScoreMakerSceneInfo
        {
            StageId = _selectedStrip.StageId,
            FirstLevel = _currentLevel
        };
        SEManager.instance.PlayButtonSe();
        GameManager.instance.OpenScene(SceneType.ScoreMaker, sceneInfo).Forget();
    }
}
