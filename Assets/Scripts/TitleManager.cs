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
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private List<Transform> _stripTransformList;
    [SerializeField] private Transform _scoreMakerTransform;
    [SerializeField] private List<MenuButton> _menuButtonList;
    [SerializeField] private GameObject _Level1Panel;
    [SerializeField] private GameObject _Level2Panel;
    [SerializeField] private GameObject _Level3Panel;
    [SerializeField] private GameObject _scoreMakerPanel;

    private List<GameObject> _stripList = new List<GameObject>();

    public void Init()
    {
        RecreateStrip();
        DarkeningMenuButton();
        for (int i = 0; i < _menuButtonList.Count; i++)
        {
            var menuButton = _menuButtonList[i];
            var button = menuButton.GetComponent<Button>();
            button.OnClickAsObservable().Subscribe(_ =>
            {
                DarkeningMenuButton();
                menuButton.OnClick();
                SetLevelPanel(menuButton.ButtonType);
            });
            if (i == 0)
            {
                menuButton.SetLight(true);
                SetLevelPanel(menuButton.ButtonType);
            }
        }
    }

    public void RecreateStrip()
    {
        DestroyAllStrip();
        for (int i = 0; i < MasterManager.StageMasterList.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var strip = Instantiate(_stageStripPrefab, _stripTransformList[j]);
                _stripList.Add(strip.gameObject);
                strip.Init(MasterManager.StageMasterList[i].StageId, j);
            }
            var scoreMakerStrip = Instantiate(_stageStripPrefab, _scoreMakerTransform);
            _stripList.Add(scoreMakerStrip.gameObject);
            scoreMakerStrip.Init(MasterManager.StageMasterList[i].StageId, 2, isScoreMaker: true);
        }
        _totalScoreText.text = SaveDataManager.GetTotalScore().ToString();
    }

    public void DestroyAllStrip()
    {
        while (_stripList.Count > 0)
        {
            var gameObject = _stripList[0];
            _stripList.RemoveAt(0);
            Destroy(gameObject);
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
    }
}
