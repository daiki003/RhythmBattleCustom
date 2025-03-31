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
    Title,
    Battle,
    Home,
    ScoreMaker
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _panelTransform;
    [SerializeField] private Transform _additionalPanelTransform;
    [SerializeField] private Image _loadPanel;

    private const string _titleScenePath = "TitlePanel";
    private const string _homeScenePath = "HomePanel";
    private const string _battleScenePath = "BattlePanel";
    private const string _scoreMakerScenePath = "ScoreMaker/ScoreMaker";

    private ClickHandler _clickHandler;
    public ClickHandler ClickHandler => _clickHandler;

    private IScene _currentScene;
    private IScene _additionalScene;
    private SceneInfoBase _currentSceneInfo;

    private UniTaskCompletionSource _loadPlayfabTask = new();
    private bool _isDuaringTransitionScene;

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
        _clickHandler?.Update();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ScreenShot.CaptureScreenShot();
        }
#endif
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
        AdsManager.InitAds();

        SaveDataManager.CreateSettingData();
        BGMManager.instance.PreloadBgm();
        SEManager.instance.PreloadSe();
        _clickHandler = new ClickHandler();
        await _loadPanel.DOFade(1, 0f);
        await OpenScene(SceneType.Title, new TitleSceneInfo(), isPlaySe: false);
        LoadFromPlayfab().Forget();
	}

    private async UniTask LoadFromPlayfab()
    {
        await PlayFabController.LoginAsync();
        await MasterManager.GetAllMasterData();
        _loadPlayfabTask.TrySetResult();
    }

    // 次のシーンを開く
    public async UniTask OpenScene(SceneType sceneType, SceneInfoBase nextSceneInfo, bool isPlaySe = true)
    {
        if (_isDuaringTransitionScene)
        {
            return;
        }
        _isDuaringTransitionScene = true;
        if (sceneType != SceneType.Title)
        {
            AdsManager.HideBanner();
        }
        BGMManager.instance.ResetHomeBgmTime();
        BGMManager.instance.Stop();
        if (isPlaySe)
        {
            SEManager.instance.PlaySe(sceneType == SceneType.Battle ? SeName.BattleStart : SeName.ChangeScene);
        }
        // 前シーンを破棄
        if (_currentScene != null)
        {
            await _currentScene.DisposeAsync();
        }
        // タイトル以外への遷移の場合、ロードが終わるまで待つ
        if (sceneType != SceneType.Title && _loadPlayfabTask != null)
        {
            await _loadPlayfabTask.Task;
        }
        // 新しいシーンを作成
        _currentScene = CreateScene(sceneType, isAdditional: false);
        await _currentScene.InitAsync(_currentSceneInfo, nextSceneInfo, _loadPanel);
        _currentSceneInfo = nextSceneInfo;
        await _currentScene.StartSceneAsync();

        if (sceneType == SceneType.Title)
        {
            AdsManager.ShowBanner();
        }
        _isDuaringTransitionScene = false;
    }

    // 追加のシーンを開く
    public async UniTask OpenAdditionalScene(SceneType sceneType, SceneInfoBase nextSceneInfo)
    {
        SEManager.instance.PlaySe(SeName.ChangeScene);
        // 現在のシーンはいったん停止
        await _currentScene.Pause();
        // 既に追加シーンがあれば破棄
        if (_additionalScene != null)
        {
            await _additionalScene.DisposeAsync();
        }
        // 新しいシーンを作成
        _additionalScene = CreateScene(sceneType, isAdditional: true);
        await _additionalScene.InitAsync(new SceneInfoBase(), nextSceneInfo, _loadPanel);
        await _additionalScene.StartSceneAsync();
    }

    private IScene CreateScene(SceneType sceneType, bool isAdditional)
    {
        var parent = isAdditional ? _additionalPanelTransform : _panelTransform;
        return sceneType switch
        {
            SceneType.Title => Instantiate(ResourceManager.LoadPrefab<TitleScene>(_titleScenePath), parent),
            SceneType.Battle => Instantiate(ResourceManager.LoadPrefab<BattleScene>(_battleScenePath), parent),
            SceneType.Home => Instantiate(ResourceManager.LoadPrefab<HomeScene>(_homeScenePath), parent),
            SceneType.ScoreMaker => Instantiate(ResourceManager.LoadPrefab<ScoreMakerScene>(_scoreMakerScenePath), parent),
            _ => throw new Exception("想定外のsceneTypeです")
        };
    }

    public async UniTask BackToMainScene()
    {
        SEManager.instance.PlaySe(SeName.ChangeScene);
        await _additionalScene.DisposeAsync();
        _additionalScene = null;
        await _currentScene.Restart();
    }
}
