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
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftButton;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _startTransform;

    [SerializeField] private Ball _ballPrefab;

    [SerializeField] private Text _countText;

    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;
    [SerializeField] private AudioSource _bgmSource;

    private List<Ball> _leftBallList = new List<Ball>();
    private List<Ball> _rightBallList = new List<Ball>();
    private int _count = 0;
    private const int _ballCount = 300;

    private List<float> _noteList = new List<float>();
    private const float _noteTimeOffset = 0.46f;
    private const float _noteTimeBuffer = 0.04f;
    private const float _ballTimeOffset = 1.68f;
    private SettingMaster _settingMaster;
    private bool _finishGetMaster;
    private float _justBeforeTime;
    private float _justBeforeRealTime;

    private ClickHandler _clickHandler;

    public static GameManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
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
        _clickHandler.OnClickLeftButton.Subscribe(_ =>
        {
            OnClickLeftButton();
        });
        _clickHandler.OnClickRightButton.Subscribe(_ =>
        {
            OnClickRightButton();
        });
        for (int i = 4; i < 100; i++)
        {
            _noteList.Add(i * 2);
        }
        PlayFabController.login();
        GameStart().Forget();
    }

    // ゲームスタート時の処理
	private async UniTask GameStart()
	{
		await UniTask.WaitWhile(() => !_finishGetMaster);
		_bgmSource.Play();
	}

    void Update()
    {
        if (!_finishGetMaster)
        {
            return;
        }
        if (_settingMaster.NoteList.Count > 0)
        {
            float noteTime = (_settingMaster.NoteList[0] + _settingMaster.NoteTimeOffset) * (60f / _settingMaster.BPM);
            if (_bgmSource.time >= noteTime - _settingMaster.BallTimeOffset)
            {
                var newBall = Instantiate(_ballPrefab, _startTransform.parent);
                var cts = new CancellationTokenSource();  
                bool isLeft = UnityEngine.Random.Range(0, 2) == 0;
                newBall.Init(isLeft, noteTime, cts);
                _settingMaster.NoteList.RemoveAt(0);
                if (isLeft)
                {
                    _leftBallList.Add(newBall);
                }
                else
                {
                    _rightBallList.Add(newBall);
                }

                var tween = newBall.transform.DOMove(newBall.IsLeft ? _leftButton.transform.position : _rightTransform.transform.position, 2f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    if (newBall.IsLeft)
                    {
                        _leftBallList.Remove(newBall);
                    }
                    else
                    {
                        _rightBallList.Remove(newBall);
                    }
                    Destroy(newBall.gameObject);
                });
                newBall.SetTween(tween);
            }
        }
        if (_settingMaster.IsTestMode)
        {
            if (_leftBallList.Count > 0 && _leftBallList[0].CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && _leftBallList[0].CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer)
            {
                OnClickLeftButton();
            }
            if (_rightBallList.Count > 0 && _rightBallList[0].CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && _rightBallList[0].CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer)
            {
                OnClickRightButton();
            }
        }
        _clickHandler.Update();
    }

    private void OnClickLeftButton()
    {
        float time = _bgmSource.time;
        float realTime = _leftBallList[0].CriticalTime;
        Debug.Log("BGM：" + (time - _justBeforeTime) + "実時間：" +  (realTime - _justBeforeRealTime));
        _justBeforeTime = time;
        _justBeforeRealTime = realTime;
        var firstActiveBall = _leftBallList.FirstOrDefault(b => b.CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && b.CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer);
        if (firstActiveBall != null)
        {
            _count++;
            _countText.text = _count.ToString();
            _seSource.PlayOneShot(_beatSe);
            firstActiveBall.Cts.Cancel();
            _leftBallList.Remove(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
    }

    private void OnClickRightButton()
    {
        float time = _bgmSource.time;
        float realTime = _rightBallList[0].CriticalTime;
        Debug.Log("BGM：" + (time - _justBeforeTime) + "実時間：" +  (realTime - _justBeforeRealTime));
        _justBeforeTime = time;
        _justBeforeRealTime = realTime;
        var firstActiveBall = _rightBallList.FirstOrDefault(b => b.CriticalTime > _bgmSource.time - _settingMaster.NoteTimeBuffer && b.CriticalTime < _bgmSource.time + _settingMaster.NoteTimeBuffer);
        if (firstActiveBall != null)
        {
            _count++;
            _countText.text = _count.ToString();
            _seSource.PlayOneShot(_beatSe);
            firstActiveBall.Cts.Cancel();
            _rightBallList.Remove(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
    }
}
