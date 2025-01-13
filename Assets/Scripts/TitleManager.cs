using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;

public enum TitlePanelType
{
    Level1,
    Level2,
    Level3,
    ScoreMaker,
    Setting,
}

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Text _totalScoreText;
    [SerializeField] private Text _titleText;
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private List<Transform> _stripTransformList;
    [SerializeField] private Transform _scoreMakerTransform;
    [SerializeField] private List<MenuButton> _menuButtonList;
    [SerializeField] private GameObject _Level1Panel;
    [SerializeField] private GameObject _Level2Panel;
    [SerializeField] private GameObject _Level3Panel;
    [SerializeField] private GameObject _scoreMakerPanel;
    [SerializeField] private GameObject _settingPanel;
    [SerializeField] private InputField _offsetSetting;

    private const string _titleBgmName = "WanderersCity";

    private List<StageStrip> _stripList = new List<StageStrip>();

    public void Init(int lastLevel)
    {
        RecreateStrip();
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
            if (i == lastLevel)
            {
                menuButton.SetLight(true);
                SetLevelPanel(menuButton.ButtonType);
            }
        }
        BGMManager.instance.SetClip(_titleBgmName, isLoop: true);
        BGMManager.instance.Play();
        _offsetSetting.text = GameManager.instance.SettingOffset.ToString();
        _offsetSetting.onValueChanged.AddListener(x =>
        {
            GameManager.instance.SettingOffset = float.Parse(x);
        });
    }

    public void RecreateStrip()
    {
        DestroyAllStrip();
        for (int i = 0; i < MasterManager.StageMasterList.Count; i++)
        {
            string stageId = MasterManager.StageMasterList[i].StageId;
            for (int j = 0; j < 3; j++)
            {
                CreateStageStrip(stageId, j, isScoreMaker: false, _stripTransformList[j]);
            }
            CreateStageStrip(stageId, 2, isScoreMaker: true, _scoreMakerTransform);
        }
        _totalScoreText.text = SaveDataManager.GetTotalScore().ToString();
    }

    private void CreateStageStrip(string stageId, int level, bool isScoreMaker, Transform parent)
    {
        var strip = Instantiate(_stageStripPrefab, parent);
        _stripList.Add(strip);
        strip.Init(stageId, level, isScoreMaker);
        strip.OnClickedBgmButton.Subscribe(x =>
        {
            ResetBgmButtonBacklight();
            BGMManager.instance.SetClip(x.isPlay ? x.stageId : _titleBgmName);
            BGMManager.instance.Play();
            strip.BgmButton.SetBacklight(x.isPlay);
        });
    }

    private void ResetBgmButtonBacklight()
    {
        foreach (var strip in _stripList)
        {
            strip.BgmButton.SetBacklight(false);
        }
    }

    public void DestroyAllStrip()
    {
        while (_stripList.Count > 0)
        {
            var strip = _stripList[0];
            _stripList.RemoveAt(0);
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

    private void SetLevelPanel(TitlePanelType titlePanelType)
    {
        _Level1Panel.SetActive(titlePanelType == TitlePanelType.Level1);
        _Level2Panel.SetActive(titlePanelType == TitlePanelType.Level2);
        _Level3Panel.SetActive(titlePanelType == TitlePanelType.Level3);
        _scoreMakerPanel.SetActive(titlePanelType == TitlePanelType.ScoreMaker);
        _settingPanel.SetActive(titlePanelType == TitlePanelType.Setting);
        _titleText.text = GetTitleText(titlePanelType);
    }

    private string GetTitleText(TitlePanelType titlePanelType)
    {
        switch (titlePanelType)
        {
            case TitlePanelType.Level1:
                return "レベル1";
            case TitlePanelType.Level2:
                return "レベル2";
            case TitlePanelType.Level3:
                return "レベル3";
            case TitlePanelType.ScoreMaker:
                return "譜面作成";
            case TitlePanelType.Setting:
                return "設定";
            default:
                return "";
        }
    }
}
