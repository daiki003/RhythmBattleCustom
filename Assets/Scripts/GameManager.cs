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
    [SerializeField] private Image _loadPanel;

    private ClickHandler _clickHandler;
    public ClickHandler ClickHandler => _clickHandler;
    public float SettingOffset;
    private int _lastBattleLevel;

    private ScoreMaker _scoreMaker;
    private BattlePresenter _battlePresenter;

    private const string _titlePrefab = "Prefabs/TitlePanel";
    private const string _battlePrefabPath = "Prefabs/BattlePanel";
    private const string _scoreMakerPrefab = "Prefabs/ScoreMaker/ScoreMaker";

    public static GameManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
        // フレームレート設定（FPS60にしたい場合）
        Application.targetFrameRate = 120;
	}

    void Start()
    {
        GameStart().Forget();
    }

    void Update()
    {
        _clickHandler.Update();
    }

    public async UniTask FadeLoadPanel(bool isActive, float fadeTime)
    {
        if (isActive)
        {
            _loadPanel.gameObject.SetActive(true);
        }
        await _loadPanel.DOFade(isActive ? 1 : 0, fadeTime).ToUniTask();
        if (!isActive)
        {
            _loadPanel.gameObject.SetActive(false);
        }
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
        await FadeLoadPanel(true, 0f);
        await PlayFabController.LoginAsync();
        await MasterManager.GetAllMasterData();
        BGMManager.instance.PreloadBgm();
        GoToTitle();
	}

    public void GoToTitle()
    {
        ResetPanel();
        var titlePrefab = Resources.Load<TitleManager>(_titlePrefab);
        var titleManager = Instantiate(titlePrefab, _panelTransform);
        titleManager.Init(_lastBattleLevel);
        FadeLoadPanel(false, 1.5f).Forget();
    }

    public void StartScoreMaker(string stageId)
    {
        BGMManager.instance.Stop();
        BGMManager.instance.SetClip(stageId, immediatePlay: false);
        ResetPanel();
        var scoreMakerPrefab = Resources.Load<ScoreMaker>(_scoreMakerPrefab);
        _scoreMaker = Instantiate(scoreMakerPrefab, _panelTransform);
        _scoreMaker.Init();
        _scoreMaker.StartMake(stageId);
    }

    public async UniTask StartBattle(string stageId, int level)
    {
        _lastBattleLevel = level;
        SEManager.instance.PlayBattleStartSe();
        BGMManager.instance.Stop();
        await FadeLoadPanel(true, 0.5f);
        ResetPanel();
        var battlePrefab = Resources.Load<BattlePresenter>(_battlePrefabPath);
        _battlePresenter = Instantiate(battlePrefab, _panelTransform);
        var stageMaster = MasterManager.GetSingleStageMaster(stageId, level);
        _battlePresenter.Init(stageMaster);
        // 曲が始まる前にGC.Collect
        GC.Collect();
        await UniTask.WaitForSeconds(1f);
        await FadeLoadPanel(false, 0.5f);
        _battlePresenter.StartBattle();
    }

    public void StartBattleFromScoreMaker(SingleStageMaster stageMaster, float timeRate)
    {
        BGMManager.instance.Stop();
        var battlePrefab = Resources.Load<BattlePresenter>(_battlePrefabPath);
        _battlePresenter = Instantiate(battlePrefab, _panelTransform);
        _battlePresenter.Init(stageMaster);
        _battlePresenter.StartBattleFromScoreMaker(timeRate);
    }

    public void BackToScoreMaker()
    {
        Destroy(_battlePresenter.gameObject);
        _scoreMaker.RestartMake();
    }
}
