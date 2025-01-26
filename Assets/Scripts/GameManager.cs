using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using R3;
using System;
using PlayFab.Json;

public enum HitType
{
    None,
    Hit,
    Critical
}

public enum SceneType
{
    None,
    Battle,
    Title,
    ScoreMaker
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _panelTransform;
    [SerializeField] private GameObject _loadPanel;

    private ClickHandler _clickHandler;
    public ClickHandler ClickHandler => _clickHandler;
    public float SettingOffset;
    private int _lastBattleLevel;

    private const string _titlePrefab = "Prefabs/TitlePanel";
    private const string _battlePrefabPath = "Prefabs/BattlePanel";
    private const string _scoreMakerPrefab = "Prefabs/ScoreMaker";

    public static GameManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
        // フレームレート設定（FPS60にしたい場合）
        Application.targetFrameRate = 60;
	}

    void Start()
    {
        GameStart().Forget();
    }

    void Update()
    {
        _clickHandler.Update();
    }

    public void SetLoadPanel(bool isActive)
    {
        _loadPanel.SetActive(isActive);
    }

    public void ResetPanel()
    {
        foreach (var panel in _panelTransform)
        {
            Destroy(((Transform)panel).gameObject);
        }
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
        _clickHandler = new ClickHandler();
        SetLoadPanel(true);
        await PlayFabController.LoginAsync();
        await MasterManager.GetAllMasterData();
        GoToTitle();
	}

    public void GoToTitle()
    {
        ResetPanel();
        var titlePrefab = Resources.Load<TitleManager>(_titlePrefab);
        var titleManager = Instantiate(titlePrefab, _panelTransform);
        titleManager.Init(_lastBattleLevel);
        SetLoadPanel(false);
    }

    public void StartScoreMaker(string stageId)
    {
        ResetPanel();
        var scoreNakerPrefab = Resources.Load<ScoreMaker>(_scoreMakerPrefab);
        var scoreMaker = Instantiate(scoreNakerPrefab, _panelTransform);
        scoreMaker.Init();
        BGMManager.instance.SetClip(stageId, isLoop: true);
        BGMManager.instance.Play();
        SetLoadPanel(false);
        scoreMaker.StartMake(stageId);
    }

    public async UniTask StartBattle(string stageId, int level)
    {
        _lastBattleLevel = level;
        ResetPanel();
        SetLoadPanel(true);
        var battlePrefab = Resources.Load<BattlePresenter>(_battlePrefabPath);
        var battlePresenter = Instantiate(battlePrefab, _panelTransform);
        SEManager.instance.PlayBattleStartSe();
        BGMManager.instance.Stop();
        var stageMaster = MasterManager.GetSingleStageMaster(stageId, level);
        battlePresenter.Init(stageMaster);
        await UniTask.WaitForSeconds(2f);
        SetLoadPanel(false);
        battlePresenter.StartBattle();
    }
}
