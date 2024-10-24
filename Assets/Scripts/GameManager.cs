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

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftTransform;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _leftStartTransform;
    [SerializeField] private RectTransform _rightStartTransform;
    [SerializeField] private RectTransform _leftLetterTransform;
    [SerializeField] private RectTransform _rightLetterTransform;

    [SerializeField] private SingleBall _ballPrefab;
    [SerializeField] private LongBall _longBallPrefab;
    [SerializeField] private Critical _criticalPrefab;

    [SerializeField] private Text _criticalCountText;
    [SerializeField] private Text _hitCountText;
    [SerializeField] private Text _missCountText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _testButton;
    [SerializeField] private Button _debugButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _testButtonImage;

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] private GameObject _debugPanel;
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private TitleManager _titleManager;

    private List<SingleBall> _leftBallList = new List<SingleBall>();
    private List<SingleBall> _rightBallList = new List<SingleBall>();
    private int _criticalCount = 0;
    private int _hitCount = 0;
    private int _missCount = 0;

    private bool _isTest;
    private float _lastBeatTime;
    private int _currentStage;
    private int _currentLevel;
    private bool _startFinish;
    private float _diffTotal;

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
            Reset();
            _titlePanel.SetActive(true);
        });
        PlayFabController.login();
        _currentStage = 1;
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

    public void Reset()
    {
        DestroyAllObjectInList(_leftBallList);
        DestroyAllObjectInList(_rightBallList);
        _criticalCount = 0;
        _hitCount = 0;
        _missCount = 0;
        _criticalCountText.text = _criticalCount.ToString();
        _hitCountText.text = _hitCount.ToString();
        _missCountText.text = _missCount.ToString();
        _bgmSource.Stop();
    }

    public void MoveTime(float time)
    {
        Reset();
        _bgmSource.time = time;
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
        return (noteMaster.noteNumber + MasterManager.SettingMaster.NoteTimeOffset) * (60f / MasterManager.SettingMaster.BPM);
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
		await UniTask.WaitWhile(() => !MasterManager.FinishGetMaster);
        _titleManager.Init();
        _titlePanel.SetActive(true);
	}

    public void StartBattle(int stageId, int level)
    {
        _titlePanel.SetActive(false);
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
        foreach (NoteMaster noteMaster in MasterManager.StageMasterList.First(s => s.StageId == _currentStage).notes[_currentLevel])
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
                var newBall = Instantiate(_ballPrefab, startTransform.parent);
                newBall.transform.localPosition = startTransform.localPosition;
                newBall.gameObject.SetActive(false);
                newBall.Init(noteMaster, noteTime, BallType.Single);
                SetBallToList(newBall);
            }
            else
            {
                var longBall = Instantiate(_longBallPrefab, startTransform.parent);
                longBall.transform.position = startTransform.position;
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
        _bgmSource.Play();
        _startFinish = false;
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
        if (launchBall != null && _bgmSource.time >= launchBall.LaunchTime)
        {
            launchBall.gameObject.SetActive(true);
            launchBall.OnWhenLaunched.OnNext(default);
            launchBall.BallState = BallState.Launched;
            CreateMoveTween(launchBall, () =>
            {
                RemoveBallFromList(launchBall);
                Destroy(launchBall.gameObject);
                CreateLetter(launchBall.IsLeft, HitType.None);
                CountUpText(HitType.None, count: launchBall.BallType == BallType.LongStart ? 2 : 1);
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
            if (leftFirstBall != null && leftFirstBall.CriticalTime > _bgmSource.time - MasterManager.SettingMaster.TestNoteTimeBuffer && leftFirstBall.CriticalTime < _bgmSource.time + MasterManager.SettingMaster.TestNoteTimeBuffer)
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
            if (rightFirstBall != null && rightFirstBall.CriticalTime > _bgmSource.time - MasterManager.SettingMaster.TestNoteTimeBuffer && rightFirstBall.CriticalTime < _bgmSource.time + MasterManager.SettingMaster.TestNoteTimeBuffer)
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
        if (_bgmClip.length <= _bgmSource.time && !_startFinish)
        {
            _startFinish = true;
            FinishBattle().Forget();
        }
        _clickHandler.Update();
    }

    private async UniTask FinishBattle()
    {
        await UniTask.WaitForSeconds(2.5f);
        SaveDataManager.UpdateClearState(_currentStage, _currentLevel, _criticalCount, _hitCount, _missCount);
        Reset();
        _titlePanel.SetActive(true);
    }

    private void OnClickButton(bool isLeft)
    {
        float time = _bgmSource.time;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstActiveBall = targetList.FirstOrDefault(b => JudgeBall(b) >= HitType.Hit);
        if (firstActiveBall != null)
        { 
            if (firstActiveBall.BallType == BallType.LongEnd)
            {
                return;
            }
            BeatBall(firstActiveBall, JudgeBall(firstActiveBall));
            if (firstActiveBall.BallType == BallType.Single)
            {
                Destroy(firstActiveBall.gameObject);
            }
            else if (firstActiveBall.BallType == BallType.LongStart)
            {
                Debug.Log("Tween破棄" + firstActiveBall.CriticalTime);
                firstActiveBall.MoveTween.Kill();
            }
            targetList.Remove(firstActiveBall);
        }
    }

    private void OnReleaseButton(bool isLeft)
    {
        float time = _bgmSource.time;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstBall = targetList.FirstOrDefault();
        // ロングノーツの終端の前で離したらそれを破棄
        if (firstBall != null && firstBall.BallType == BallType.LongEnd && JudgeBall(firstBall) == HitType.None)
        {
            targetList.Remove(firstBall);
            Destroy(firstBall.gameObject);
            CreateLetter(isLeft, HitType.None);
            CountUpText(HitType.None);
        }
        var firstActiveBall = targetList.FirstOrDefault(b => JudgeBall(b) >= HitType.Hit);
        if (firstActiveBall != null)
        {
            if (firstActiveBall.BallType != BallType.LongEnd)
            {
                return;
            }
            BeatBall(firstActiveBall, JudgeBall(firstActiveBall));
            Destroy(firstActiveBall.gameObject);
        }
    }

    private HitType JudgeBall(SingleBall ball)
    {
        var diff = ball.CriticalTime - _bgmSource.time;
        _diffTotal += diff;
        Debug.Log("判定合計" + _diffTotal);
        if (ball.CriticalTime > _bgmSource.time - MasterManager.SettingMaster.CriticalTimeBuffer && ball.CriticalTime < _bgmSource.time + MasterManager.SettingMaster.CriticalTimeBuffer)
        {
            return HitType.Critical;
        }
        else if (ball.CriticalTime > _bgmSource.time - MasterManager.SettingMaster.HitTimeBuffer && ball.CriticalTime < _bgmSource.time + MasterManager.SettingMaster.HitTimeBuffer)
        {
            return HitType.Hit;
        }
        return HitType.None;
    }

    // ボールを打った時の処理
    private void BeatBall(SingleBall ball, HitType hitType)
    {
        if (_lastBeatTime != ball.CriticalTime)
        {
            SEManager.instance.PlayBeatSe();
        }
        _lastBeatTime = ball.CriticalTime;
        ball.OnWhenClicked.OnNext(default);
        CreateLetter(ball.IsLeft, JudgeBall(ball));
        CountUpText(hitType);
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
        }
        else if (hitType == HitType.Critical)
        {
            _criticalCount += count;
            _criticalCountText.text = _criticalCount.ToString();
        }
        else
        {
            _missCount += count;
            _missCountText.text = _missCount.ToString();
        }
    }
}
