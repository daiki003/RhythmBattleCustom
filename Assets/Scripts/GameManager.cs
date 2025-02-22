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
    [SerializeField] private Transform _additionalPanelTransform;
    [SerializeField] private Image _loadPanel;

    private const string _homeScenePath = "HomePanel";
    private const string _battleScenePath = "BattlePanel";
    private const string _scoreMakerScenePath = "ScoreMaker/ScoreMaker";

    private ClickHandler _clickHandler;
    public ClickHandler ClickHandler => _clickHandler;
    public float SettingOffset;

    private IScene _currentScene;
    private IScene _additionalScene;
    private SceneInfoBase _currentSceneInfo;

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
        await OpenScene(SceneType.Title, new TitleSceneInfo());
	}

    // 次のシーンを開く
    public async UniTask OpenScene(SceneType sceneType, SceneInfoBase nextSceneInfo)
    {
        BGMManager.instance.Stop();
        if (!_loadPanel.gameObject.activeSelf)
        {
            await FadeLoadPanel(true, 0.5f);
        }
        // 前シーンを破棄
        _currentScene?.Dispose();
        // 新しいシーンを作成
        _currentScene = CreateScene(sceneType, isAdditional: false);
        await _currentScene.InitAsync(_currentSceneInfo, nextSceneInfo);
        _currentSceneInfo = nextSceneInfo;
        await FadeLoadPanel(false, 0.5f);
        _currentScene.StartScene();
    }

    // 追加のシーンを開く
    public async UniTask OpenAdditionalScene(SceneType sceneType, SceneInfoBase nextSceneInfo)
    {
        if (!_loadPanel.gameObject.activeSelf)
        {
            await FadeLoadPanel(true, 0.5f);
        }
        // 現在のシーンはいったん停止
        _currentScene.Pause();
        // 既に追加シーンがあれば破棄
        _additionalScene?.Dispose();
        // 新しいシーンを作成
        _additionalScene = CreateScene(sceneType, isAdditional: true);
        await _additionalScene.InitAsync(new SceneInfoBase(), nextSceneInfo);
        await FadeLoadPanel(false, 0.5f);
        _additionalScene.StartScene();
    }

    private IScene CreateScene(SceneType sceneType, bool isAdditional)
    {
        var parent = isAdditional ? _additionalPanelTransform : _panelTransform;
        return sceneType switch
        {
            SceneType.Battle => Instantiate(ResourceManager.LoadPrefab<BattleScene>(_battleScenePath), parent),
            SceneType.Title => Instantiate(ResourceManager.LoadPrefab<HomeScene>(_homeScenePath), parent),
            SceneType.ScoreMaker => Instantiate(ResourceManager.LoadPrefab<ScoreMakerScene>(_scoreMakerScenePath), parent),
            _ => throw new Exception("想定外のsceneTypeです")
        };
    }

    public void GoToTitle()
    {
        OpenScene(SceneType.Title, new TitleSceneInfo()).Forget();
    }

    public async UniTask StartBattleFromScoreMaker(SingleStageMaster stageMaster, float timeRate)
    {
        var sceneInfo = new BattleSceneInfo
        {
            StageMaster = stageMaster,
            TimeRate = timeRate,
            IsAdditional = true
        };
        await OpenAdditionalScene(SceneType.Battle, sceneInfo);
    }

    public void BackToMainScene()
    {
        _additionalScene.Dispose();
        _additionalScene = null;
        _currentScene.Restart();
    }
}
