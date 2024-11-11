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
    Title
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftTransform;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _leftStartTransform;
    [SerializeField] private RectTransform _rightStartTransform;
    [SerializeField] private RectTransform _leftLetterTransform;
    [SerializeField] private RectTransform _rightLetterTransform;
    [SerializeField] private RectTransform _ballTransform;

    [SerializeField] private SingleBall _ballPrefab;
    [SerializeField] private LongBall _longBallPrefab;
    [SerializeField] private Critical _criticalPrefab;

    [SerializeField] private Text _criticalCountText;
    [SerializeField] private Text _hitCountText;
    [SerializeField] private Text _missCountText;
    [SerializeField] private Text _comboText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _testButton;
    [SerializeField] private Button _debugButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _testButtonImage;

    [SerializeField] private GameObject _debugPanel;
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private GameObject _battlePanel;
    [SerializeField] private GameObject _loadPanel;
    [SerializeField] private TitleManager _titleManager;
    [SerializeField] private ResultView _resultView;

    private List<SingleBall> _leftBallList = new List<SingleBall>();
    private List<SingleBall> _rightBallList = new List<SingleBall>();
    private int _criticalCount = 0;
    private int _hitCount = 0;
    private int _missCount = 0;
    private int _comboCount = 0;
    private int _maxComboCount = 0;

    private bool _isTest;
    private float _lastBeatTime;
    private string _currentStage;
    private int _currentLevel;
    private bool _isDuaringBattle;
    private StageMaster _currentStageMaster;

    private ClickHandler _clickHandler;

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
        _clickHandler = new ClickHandler();
        _clickHandler.OnClickButton.Subscribe(isLeft =>
        {
            OnClickButton(isLeft);
        });
        _clickHandler.OnReleaseButton.Subscribe(isLeft =>
        {
            OnReleaseButton(isLeft);
        });
        _resetButton.OnClickAsObservable().Subscribe(_ =>
        {
            Reset();
            StartMusic();
        });
        _testButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeTest();
        });
        _debugButton.OnClickAsObservable().Subscribe(_ =>
        {
            _debugPanel.SetActive(true);
        });
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            GoToTitle();
        });
        _resultView.OnWhenPushGoHome.Subscribe(_ =>
        {
            GoToTitle();
        });
        ChangePanel(SceneType.None);
        PlayFabController.login();
        GameStart().Forget();
    }

    public void DestroyAllObjectInList<T>(List<T> ballList)
    {
        while (ballList.Count > 0)
        {
            var ball = ballList[0];
            ballList.RemoveAt(0);
            if (ball is MonoBehaviour monoBehaviour)
            {
                Destroy(monoBehaviour.gameObject);
            }
        }
    }

    public void ChangePanel(SceneType sceneType)
    {
        _loadPanel.SetActive(sceneType == SceneType.None);
        _battlePanel.SetActive(sceneType == SceneType.Battle);
        _titlePanel.SetActive(sceneType == SceneType.Title);
    }

    public void Reset()
    {
        DestroyAllObjectInList(_leftBallList);
        DestroyAllObjectInList(_rightBallList);
        _criticalCount = 0;
        _hitCount = 0;
        _missCount = 0;
        _comboCount = 0;
        _maxComboCount = 0;
        _criticalCountText.text = _criticalCount.ToString();
        _hitCountText.text = _hitCount.ToString();
        _missCountText.text = _missCount.ToString();
        _comboText.text = _comboCount.ToString();
        BGMManager.instance.Stop();
    }

    public void MoveTime(float time)
    {
        Reset();
        BGMManager.instance.SetTime(time);
        StartMusic(time);
    }

    public void ChangeLevel(int level)
    {
        _currentLevel = level;
        Reset();
        StartMusic();
    }

    public void ChangeTest()
    {
        _isTest = !_isTest;
        _testButtonImage.color = _isTest ? Color.black : Color.white;
    }

    private float CalcNoteTime(NoteMaster noteMaster)
    {
        int noteNumber = noteMaster.num * (_currentStageMaster.LPB / noteMaster.lpb);
        return (noteNumber + _currentStageMaster.NoteTimeOffset) * (60f / _currentStageMaster.BPM);
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
		await UniTask.WaitWhile(() => !MasterManager.FinishGetMaster);
        _titleManager.Init();
        GoToTitle();
	}

    private void GoToTitle()
    {
        Reset();
        _titleManager.RecreateStrip();
        ChangePanel(SceneType.Title);
        BGMManager.instance.SetClip("WanderersCity", isLoop: true);
        BGMManager.instance.Play();
    }

    public async UniTask StartBattle(string stageId, int level)
    {
        ChangePanel(SceneType.None);
        SEManager.instance.PlayBattleStartSe();
        BGMManager.instance.Stop();
        await UniTask.WaitForSeconds(2f);
        ChangePanel(SceneType.Battle);
        _resultView.gameObject.SetActive(false);
        Reset();
        _currentStage = stageId;
        _currentLevel = level;
        StartMusic();
    }

    // 曲開始時の共通処理
    private void StartMusic(float startTime = 0f)
	{
        // 曲が始まる前にGC.Collect
        GC.Collect();
        _currentStageMaster = MasterManager.StageMasterList.First(s => s.StageId == _currentStage);
        BGMManager.instance.SetClip(_currentStageMaster.StageId);
        foreach (NoteMaster noteMaster in _currentStageMaster.notes[_currentLevel])
        {
            float noteTime = CalcNoteTime(noteMaster);
            // 途中から曲を始める場合それより前のボールは作らない
            if (noteTime < startTime)
            {
                continue;
            }
            bool isLeft = noteMaster.block <= 3;
            var startTransform = isLeft ? _leftStartTransform : _rightStartTransform;
            if (noteMaster.type == 1)
            {
                var newBall = Instantiate(_ballPrefab, _ballTransform);
                newBall.transform.localPosition = startTransform.localPosition;
                newBall.gameObject.SetActive(false);
                newBall.Init(noteMaster, noteTime, BallType.Single);
                SetBallToList(newBall);
            }
            else
            {
                var longBall = Instantiate(_longBallPrefab, _ballTransform);
                longBall.StartBall.transform.position = startTransform.position;
                longBall.EndBall.transform.position = startTransform.position;
                longBall.gameObject.SetActive(false);
                var endNote = noteMaster.notes[0];
                float endNoteTime = CalcNoteTime(endNote);
                longBall.Init(noteMaster, noteTime, endNoteTime);
                SetBallToList(longBall.StartBall);
                SetBallToList(longBall.EndBall);
            }
        }
        _isDuaringBattle = true;
        BGMManager.instance.PlayFromIntro().Forget();
	}

    // ボールをリストに入れる
    private void SetBallToList(SingleBall ball, bool isInsert = false)
    {
        if (ball.IsLeft)
        {
            if (isInsert)
            {
                _leftBallList.Insert(0, ball);
            }
            else
            {
                _leftBallList.Add(ball);
            }
        }
        else
        {
            if (isInsert)
            {
                _rightBallList.Insert(0, ball);
            }
            else
            {
                _rightBallList.Add(ball);
            }
        }
        // ボールが破棄されたときにリストから取り除く
        ball.OnWhenDestroyed.Subscribe(b => 
        {
            RemoveBallFromList(b);
        });
        // ボールを打ち損ねたらミス判定
        ball.OnWhenMiss.Subscribe(_ => 
        {
            RemoveBallFromList(ball);
            CreateLetter(ball.IsLeft, HitType.None);
            CountUpText(HitType.None, count: ball.BallType == BallType.LongStart ? 2 : 1);
        });
    }

    private void RemoveBallFromList(SingleBall ball)
    {
        if (ball.IsLeft)
        {
            _leftBallList.Remove(ball);
        }
        else
        {
            _rightBallList.Remove(ball);
        }
    }

    private void LaunchBall(List<SingleBall> ballList)
    {
        var launchBall = ballList.FirstOrDefault(b => b.BallState == BallState.Wait);
        if (launchBall != null && BGMManager.instance.CurrentTime >= launchBall.LaunchTime)
        {
            launchBall.gameObject.SetActive(true);
            launchBall.OnWhenLaunched.OnNext(default);
            launchBall.BallState = BallState.Launched;
            CreateMoveTween(launchBall, () =>
            {
                // RemoveBallFromList(launchBall);
                // Destroy(launchBall.gameObject);
                // CreateLetter(launchBall.IsLeft, HitType.None);
                // CountUpText(HitType.None, count: launchBall.BallType == BallType.LongStart ? 2 : 1);
            });
        }
    }

    private void CreateMoveTween(SingleBall ball, Action action)
    {
        var tween = ball.transform.DOMove(ball.IsLeft ? _leftTransform.transform.position : _rightTransform.transform.position, MasterManager.SettingMaster.BallSpeed).SetEase(Ease.Linear).OnComplete(() =>
        {
            action();
        });
        ball.SetTween(tween);
    }

    void Update()
    {
        if (!MasterManager.FinishGetMaster)
        {
            return;
        }
        if (_leftBallList.Count > 0)
        {
            LaunchBall(_leftBallList);
        }
        if (_rightBallList.Count > 0)
        {
            LaunchBall(_rightBallList);
        }
        if (_isTest)
        {
            var leftFirstBall = _leftBallList.FirstOrDefault();
            if (leftFirstBall != null && leftFirstBall.CriticalTime > BGMManager.instance.CurrentTime - MasterManager.SettingMaster.TestNoteTimeBuffer && leftFirstBall.CriticalTime < BGMManager.instance.CurrentTime + MasterManager.SettingMaster.TestNoteTimeBuffer)
            {
                if (leftFirstBall.BallType == BallType.LongEnd)
                {
                    OnReleaseButton(isLeft: true);
                }
                else
                {
                    OnClickButton(isLeft: true);
                }
            }
            var rightFirstBall = _rightBallList.FirstOrDefault();
            if (rightFirstBall != null && rightFirstBall.CriticalTime > BGMManager.instance.CurrentTime - MasterManager.SettingMaster.TestNoteTimeBuffer && rightFirstBall.CriticalTime < BGMManager.instance.CurrentTime + MasterManager.SettingMaster.TestNoteTimeBuffer)
            {
                if (rightFirstBall.BallType == BallType.LongEnd)
                {
                    OnReleaseButton(isLeft: false);
                }
                else
                {
                    OnClickButton(isLeft: false);
                }
            }
        }
        if (BGMManager.instance.IsFinishBgm && _isDuaringBattle)
        {
            _isDuaringBattle = false;
            FinishBattle().Forget();
        }
        _clickHandler.Update();
    }

    private async UniTask FinishBattle()
    {
        _maxComboCount = Math.Max(_comboCount, _maxComboCount);
        await UniTask.WaitForSeconds(2.5f);

        var clearState = new ClearState()
        {
            StageId = _currentStage,
            Level = _currentLevel,
        };
        float totalCount = _criticalCount + _hitCount + _missCount;
        float criticalMultiple = 100f / totalCount;
        float hitMultiple = 50f / totalCount;
        float missMultiple = -100f / totalCount;
        float realScore = Mathf.Max(0, _criticalCount * criticalMultiple + _hitCount * hitMultiple + _missCount * missMultiple);
        if (clearState != null && clearState.Score <= realScore)
        {
            clearState.CriticalNumber = _criticalCount;
            clearState.HitNumber = _hitCount;
            clearState.MissNumber = _missCount;
            clearState.Score = realScore;
            clearState.Combo = _maxComboCount;
        }

        _resultView.SetScore(clearState, SaveDataManager.GetClearState(_currentStage, _currentLevel), criticalMultiple, hitMultiple, missMultiple);
        _resultView.gameObject.SetActive(true);
        BGMManager.instance.SetClip("Result", isLoop: true);
        BGMManager.instance.Play();
        if (!_isTest)
        {
            SaveDataManager.UpdateClearState(clearState);
        }
    }

    private void OnClickButton(bool isLeft)
    {
        float time = BGMManager.instance.CurrentTime;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstActiveBall = targetList.FirstOrDefault(b => b.JudgeBall() >= HitType.Hit);
        if (firstActiveBall != null)
        {
            bool isSuccess = firstActiveBall.OnClickButton();
            if (!isSuccess)
            {
                return;
            }
            BeatBall(firstActiveBall);
            targetList.Remove(firstActiveBall);
        }
    }

    private void OnReleaseButton(bool isLeft)
    {
        float time = BGMManager.instance.CurrentTime;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstBall = targetList.FirstOrDefault();
        // ロングノーツの終端の前で離したらそれを破棄
        if (firstBall != null && firstBall.BallType == BallType.LongEnd && firstBall.JudgeBall() == HitType.None)
        {
            targetList.Remove(firstBall);
            Destroy(firstBall.gameObject);
            CreateLetter(isLeft, HitType.None);
            CountUpText(HitType.None);
        }
        var firstActiveBall = targetList.FirstOrDefault(b => b.JudgeBall() >= HitType.Hit);
        if (firstActiveBall != null)
        {
            if (firstActiveBall.BallType != BallType.LongEnd)
            {
                return;
            }
            BeatBall(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
    }

    // ボールを打った時の処理
    private void BeatBall(SingleBall ball)
    {
        if (_lastBeatTime != ball.CriticalTime)
        {
            SEManager.instance.PlayBeatSe();
        }
        _lastBeatTime = ball.CriticalTime;
        CreateLetter(ball.IsLeft, ball.JudgeBall());
        CountUpText(ball.JudgeBall());
    }

    private void CreateLetter(bool isLeft, HitType hitType)
    {
        var letter = Instantiate(_criticalPrefab, isLeft ? _leftLetterTransform : _rightLetterTransform);
        letter.InitAndStart(hitType).Forget();
    }

    private void CountUpText(HitType hitType, int count = 1)
    {
        if (hitType == HitType.Hit)
        {
            _hitCount += count;
            _hitCountText.text = _hitCount.ToString();
            _comboCount += count;
            _comboText.text = _comboCount.ToString();
        }
        else if (hitType == HitType.Critical)
        {
            _criticalCount += count;
            _criticalCountText.text = _criticalCount.ToString();
            _comboCount += count;
            _comboText.text = _comboCount.ToString();
        }
        else
        {
            _missCount += count;
            _missCountText.text = _missCount.ToString();
            _maxComboCount = Math.Max(_comboCount, _maxComboCount);
            _comboCount = 0;
            _comboText.text = _comboCount.ToString();
        }
    }
}
