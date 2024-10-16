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

public class SettingMaster
{
    public float NoteTimeOffset;
    public float NoteTimeBuffer;
    public float BallTimeOffset;
    public float BallSpeed;
    public float BPM;
    public float TestNoteTimeBuffer;
    public List<float> NoteList = new List<float>();
    public List<List<NoteMaster>> notes = new List<List<NoteMaster>>();
}

public class NoteMaster
{
    public int lpb;
    public int num;
    public int block;
    public int type;
    public List<NoteMaster> notes = new List<NoteMaster>();
    public int noteNumber => num * (4 / lpb);
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftTransform;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _leftStartTransform;
    [SerializeField] private RectTransform _rightStartTransform;

    [SerializeField] private SingleBall _ballPrefab;
    [SerializeField] private LongBall _longBallPrefab;

    [SerializeField] private Text _countText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _testButton;
    [SerializeField] private Button _debugButton;
    [SerializeField] private Image _testButtonImage;

    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private GameObject _debugPanel;

    private List<SingleBall> _leftBallList = new List<SingleBall>();
    private List<SingleBall> _rightBallList = new List<SingleBall>();
    private int _count = 0;

    private SettingMaster _settingMaster;
    private bool _finishGetMaster;
    private bool _isTest;
    private float _lastCriticalTime;
    private int _currentLevel;

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

    public void GetAllMasterData()
	{
		PlayFabController.GetTitleData(SetMasterData);
	}

    public void SetMasterData(SettingMaster settingMaster)
	{
		_settingMaster = settingMaster;
		_finishGetMaster = true;
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

    public void Reset()
    {
        DestroyAllObjectInList(_leftBallList);
        DestroyAllObjectInList(_rightBallList);
        _count = 0;
        _countText.text = _count.ToString();
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
        return (noteMaster.noteNumber + _settingMaster.NoteTimeOffset) * (60f / _settingMaster.BPM);
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
		await UniTask.WaitWhile(() => !_finishGetMaster);
        StartMusic();
	}

    // 曲開始時の共通処理
    private void StartMusic(float startTime = 0f)
	{
        // 曲が始まる前にGC.Collect
        GC.Collect();
        foreach (NoteMaster noteMaster in _settingMaster.notes[_currentLevel])
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
                newBall.Init(noteMaster, noteTime, _settingMaster.BallTimeOffset, BallType.Single);
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
                longBall.Init(noteMaster, noteTime, endNoteTime, _settingMaster.BallTimeOffset);
                SetBallToList(longBall.StartBall);
                SetBallToList(longBall.EndBall);
            }
        }
        _bgmSource.Play();
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
            });
        }
    }

    private void CreateMoveTween(SingleBall ball, Action action)
    {
        var tween = ball.transform.DOMove(ball.IsLeft ? _leftTransform.transform.position : _rightTransform.transform.position, _settingMaster.BallSpeed).SetEase(Ease.Linear).OnComplete(() =>
        {
            action();
        });
        ball.SetTween(tween);
    }

    void Update()
    {
        if (!_finishGetMaster)
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
            if (leftFirstBall != null && leftFirstBall.CriticalTime > _bgmSource.time - _settingMaster.TestNoteTimeBuffer && leftFirstBall.CriticalTime < _bgmSource.time + _settingMaster.TestNoteTimeBuffer)
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
            if (rightFirstBall != null && rightFirstBall.CriticalTime > _bgmSource.time - _settingMaster.TestNoteTimeBuffer && rightFirstBall.CriticalTime < _bgmSource.time + _settingMaster.TestNoteTimeBuffer)
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
        _clickHandler.Update();
    }

    private void OnClickButton(bool isLeft)
    {
        float time = _bgmSource.time;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstActiveBall = targetList.FirstOrDefault(b => IsBallClitical(b));
        if (firstActiveBall != null)
        { 
            if (firstActiveBall.BallType == BallType.LongEnd)
            {
                return;
            }
            _count++;
            _countText.text = _count.ToString();
            if (_lastCriticalTime != firstActiveBall.CriticalTime)
            {
                _seSource.PlayOneShot(_beatSe);
            }
            _lastCriticalTime = firstActiveBall.CriticalTime;
            targetList.Remove(firstActiveBall);
            firstActiveBall.OnWhenClicked.OnNext(default);
            if (firstActiveBall.BallType == BallType.Single)
            {
                Destroy(firstActiveBall.gameObject);
            }
            else if (firstActiveBall.BallType == BallType.LongStart)
            {
                Debug.Log("Tween破棄" + firstActiveBall.CriticalTime);
                firstActiveBall.MoveTween.Kill();
            }
        }
    }

    private void OnReleaseButton(bool isLeft)
    {
        float time = _bgmSource.time;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstBall = targetList.FirstOrDefault();
        // ロングノーツの終端の前で離したらそれを破棄
        if (firstBall.BallType == BallType.LongEnd && !IsBallClitical(firstBall))
        {
            targetList.Remove(firstBall);
            Destroy(firstBall.gameObject);
        }
        var firstActiveBall = targetList.FirstOrDefault(b => IsBallClitical(b));
        if (firstActiveBall != null)
        {
            if (firstActiveBall.BallType != BallType.LongEnd)
            {
                return;
            }
            _count++;
            _countText.text = _count.ToString();
            if (_lastCriticalTime != firstActiveBall.CriticalTime)
            {
                _seSource.PlayOneShot(_beatSe);
            }
            _lastCriticalTime = firstActiveBall.CriticalTime;
            targetList.Remove(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
    }

    private bool IsBallClitical(SingleBall ball)
    {
        return ball.CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && ball.CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer;
    }
}
