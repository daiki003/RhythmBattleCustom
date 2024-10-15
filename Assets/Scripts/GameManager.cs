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
    public float BPM;
    public bool IsTestMode;
    public float TestNoteTimeBuffer;
    public List<float> NoteList = new List<float>();
    public List<NoteMaster> notes = new List<NoteMaster>();
}

public class NoteMaster
{
    public int lpb;
    public int num;
    public int block;
    public int type;
    public List<NoteMaster> notes = new List<NoteMaster>();
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftTransform;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _leftStartTransform;
    [SerializeField] private RectTransform _rightStartTransform;

    [SerializeField] private Ball _ballPrefab;
    [SerializeField] private LongBall _longBallPrefab;

    [SerializeField] private Text _countText;
    [SerializeField] private Text _touchCountText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _testButton;
    [SerializeField] private Button _debugButton;
    [SerializeField] private Image _testButtonImage;

    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private GameObject _debugPanel;

    private List<Ball> _leftBallList = new List<Ball>();
    private List<Ball> _rightBallList = new List<Ball>();
    private List<LongBall> _longBallList = new List<LongBall>();
    private int _count = 0;

    private List<NoteMaster> _currentNoteList = new List<NoteMaster>();
    private SettingMaster _settingMaster;
    private bool _finishGetMaster;
    private bool _isTest;
    private float _lastCriticalTime;

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

    public void Reset()
    {
        _currentNoteList = new List<NoteMaster>(_settingMaster.notes);
        while (_leftBallList.Count > 0)
        {
            var ball = _leftBallList[0];
            _leftBallList.RemoveAt(0);
            Destroy(ball.gameObject);
        }
        while (_rightBallList.Count > 0)
        {
            var ball = _rightBallList[0];
            _rightBallList.RemoveAt(0);
            Destroy(ball.gameObject);
        }
        while (_longBallList.Count > 0)
        {
            var ball = _longBallList[0];
            _longBallList.RemoveAt(0);
            Destroy(ball.gameObject);
        }
        _count = 0;
        _countText.text = _count.ToString();
        _bgmSource.Stop();
        _bgmSource.Play();
    }

    public void MoveTime(float time)
    {
        Reset();
        _bgmSource.Pause();
        while (_currentNoteList.Count > 0 && CalcNoteTime(_currentNoteList[0]) < time + _settingMaster.BallTimeOffset)
        {
            _currentNoteList.RemoveAt(0);
        }
        _bgmSource.time = time;
        _bgmSource.Play();
    }

    public void ChangeTest()
    {
        _isTest = !_isTest;
        _testButtonImage.color = _isTest ? Color.black : Color.white;
    }

    private float CalcNoteTime(NoteMaster noteMaster)
    {
        return (noteMaster.num * (4 / noteMaster.lpb) + _settingMaster.NoteTimeOffset) * (60f / _settingMaster.BPM);
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
		await UniTask.WaitWhile(() => !_finishGetMaster);
		_currentNoteList = new List<NoteMaster>(_settingMaster.notes);
        _isTest = _settingMaster.IsTestMode;
        _testButtonImage.color = _isTest ? Color.black : Color.white;
        _bgmSource.Play();
	}

    void Update()
    {
        if (!_finishGetMaster)
        {
            return;
        }
        if (_currentNoteList.Count > 0)
        {
            var firstNote = _currentNoteList[0];
            float noteTime = CalcNoteTime(firstNote);
            if (_bgmSource.time >= noteTime - _settingMaster.BallTimeOffset)
            {
                bool isLeft = firstNote.block <= 3;
                var startTransform = isLeft ? _leftStartTransform : _rightStartTransform;
                if (firstNote.type == 1)
                {
                    var newBall = Instantiate(_ballPrefab, startTransform.parent);
                    newBall.OnWhenDestroy.Subscribe(ball => 
                    {
                        RemoveBallFromList(ball);
                    });
                    newBall.transform.localPosition = startTransform.localPosition;
                    var cts = new CancellationTokenSource();  
                    newBall.Init(isLeft, noteTime, BallType.Single, cts);
                    _currentNoteList.RemoveAt(0);
                    if (isLeft)
                    {
                        _leftBallList.Add(newBall);
                    }
                    else
                    {
                        _rightBallList.Add(newBall);
                    }

                    var tween = newBall.transform.DOMove(newBall.IsLeft ? _leftTransform.transform.position : _rightTransform.transform.position, 2f).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        RemoveBallFromList(newBall);
                        Destroy(newBall.gameObject);
                    });
                    newBall.SetTween(tween);
                }
                else
                {
                    var longBall = Instantiate(_longBallPrefab, startTransform.parent);
                    longBall.transform.position = startTransform.position;
                    longBall.StartBall.transform.position = startTransform.position;
                    longBall.EndBall.transform.position = startTransform.position;
                    var cts = new CancellationTokenSource();
                    var endNote = firstNote.notes[0];
                    float endNoteTime = (endNote.num * (4 / endNote.lpb) + _settingMaster.NoteTimeOffset) * (60f / _settingMaster.BPM);
                    longBall.Init(isLeft, noteTime, endNoteTime, cts);
                    longBall.StartBall.OnWhenDestroy.Subscribe(ball => 
                    {
                        RemoveBallFromList(ball);
                    });
                    longBall.EndBall.OnWhenDestroy.Subscribe(ball => 
                    {
                        RemoveBallFromList(ball);
                    });
                    _currentNoteList.RemoveAt(0);
                    if (isLeft)
                    {
                        _leftBallList.Add(longBall.StartBall);
                    }
                    else
                    {
                        _rightBallList.Add(longBall.StartBall);
                    }
                    _longBallList.Add(longBall);
                    longBall.OnWhenDestroyed.Subscribe(_ =>
                    {
                        _longBallList.Remove(longBall);
                    });

                    var startBallTween = longBall.StartBall.transform.DOMove(longBall.StartBall.IsLeft ? _leftTransform.transform.position : _rightTransform.transform.position, 2f).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        RemoveBallFromList(longBall.StartBall);
                        Destroy(longBall.gameObject);
                    });
                    longBall.StartBall.SetTween(startBallTween);
                }
            }
        }
        if (_longBallList.Count > 0)
        {
            var firstBall = _longBallList.FirstOrDefault(b => !b.IsEndBallLaunched);
            if (firstBall != null && _bgmSource.time >= firstBall.EndBall.CriticalTime - _settingMaster.BallTimeOffset)
            {
                firstBall.IsEndBallLaunched = true;
                if (firstBall.EndBall.IsLeft)
                {
                    _leftBallList.Add(firstBall.EndBall);
                }
                else
                {
                    _rightBallList.Add(firstBall.EndBall);
                }
                var startBallTween = firstBall.EndBall.transform.DOMove(firstBall.EndBall.IsLeft ? _leftTransform.transform.position : _rightTransform.transform.position, 2f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    RemoveBallFromList(firstBall.EndBall);
                    Destroy(firstBall.gameObject);
                });
                firstBall.EndBall.SetTween(startBallTween);
            }
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

    private void RemoveBallFromList(Ball ball)
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

    private void OnClickButton(bool isLeft)
    {
        float time = _bgmSource.time;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstActiveBall = targetList.FirstOrDefault(b => b.CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && b.CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer);
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
            firstActiveBall.Cts.Cancel();
            targetList.Remove(firstActiveBall);
            firstActiveBall.OnWhenClicked.OnNext(default);
            if (firstActiveBall.BallType == BallType.Single)
            {
                Destroy(firstActiveBall.gameObject);
            }
            else if (firstActiveBall.BallType == BallType.LongStart)
            {
                firstActiveBall.MoveTween.Kill();
            }
        }
    }

    private void OnReleaseButton(bool isLeft)
    {
        float time = _bgmSource.time;
        var targetList = isLeft ? _leftBallList : _rightBallList;
        var firstBall = targetList.FirstOrDefault();
        var firstActiveBall = targetList.FirstOrDefault(b => b.CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && b.CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer);
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
            firstActiveBall.Cts.Cancel();
            targetList.Remove(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
        var outLongBallList = _longBallList.Where(b => b.IsStartBallClicked && b.EndBall.IsLeft == isLeft).ToList();
        while (outLongBallList.Count > 0)
        {
            var endBall = outLongBallList[0].EndBall;
            if (isLeft)
            {
                _leftBallList.Remove(endBall);
            }
            else
            {
                _rightBallList.Remove(endBall);
            }
            Destroy(outLongBallList[0].gameObject);
            outLongBallList.RemoveAt(0);
        }
    }
}
