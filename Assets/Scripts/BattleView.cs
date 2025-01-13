using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Linq;
using DG.Tweening;
using Cysharp.Threading.Tasks.Triggers;
using System.Threading;

public class BattleView : MonoBehaviour
{
    [SerializeField] private Image _enemyImage;
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

    [SerializeField] private DebugPanel _debugPanel;
    [SerializeField] private ResultView _resultView;

    public DebugPanel DebugPanel => _debugPanel;

    private List<SingleBall> _leftBallList = new List<SingleBall>();
    private List<SingleBall> _rightBallList = new List<SingleBall>();

    private bool _isTest;
    public bool IsTest => _isTest;
    private float _lastBeatTime;
    private CancellationTokenSource _cts;

    public Subject<Unit> OnReset { get; private set; } = new Subject<Unit>();
    public Subject<Unit> OnWhenClickedBack { get; private set; } = new Subject<Unit>();
    public Subject<(HitType, int)> OnCountUp { get; private set; } = new Subject<(HitType, int)>();

    private const float _beforeReultWaitTime = 1f;

    public void Init(string stageId)
    {
        _cts = new CancellationTokenSource();
        GameManager.instance.ClickHandler.OnClickButton.Subscribe(isLeft =>
        {
            OnClickButton(isLeft);
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnReleaseButton.Subscribe(isLeft =>
        {
            OnReleaseButton(isLeft);
        }).AddTo(this);
        _resetButton.OnClickAsObservable().Subscribe(_ =>
        {
            Reset();
            OnReset.OnNext(default);
        }).AddTo(this);
        _testButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeTest();
        }).AddTo(this);
        _debugButton.OnClickAsObservable().Subscribe(_ =>
        {
            _debugPanel.gameObject.SetActive(true);
        }).AddTo(this);
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnWhenClickedBack.OnNext(default);
        }).AddTo(this);
        _resultView.OnWhenPushRestart.Subscribe(_ =>
        {
            Reset();
            OnReset.OnNext(default);
        }).AddTo(this);
        _resultView.OnWhenPushGoHome.Subscribe(_ =>
        {
            OnWhenClickedBack.OnNext(default);
        }).AddTo(this);

        _resultView.gameObject.SetActive(false);
        Reset();
        _enemyImage.sprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + stageId);
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
        _criticalCountText.text = "0";
        _hitCountText.text = "0";
        _missCountText.text = "0";
        _comboText.text = "0";
        BGMManager.instance.Stop();
    }

    public void ChangeTest()
    {
        _isTest = !_isTest;
        _testButtonImage.color = _isTest ? Color.black : Color.white;
    }

    private float CalcNoteTime(NoteMaster noteMaster, StageMaster stageMaster)
    {
        int noteNumber = noteMaster.num * (stageMaster.LPB / noteMaster.lpb);
        return noteNumber * (60f / stageMaster.BPM) + stageMaster.NoteTimeOffset + GameManager.instance.SettingOffset;
    }

    public void CreateBalls(List<NoteMaster> notes, StageMaster stageMaster, float startTime = 0f)
    {
        foreach (NoteMaster noteMaster in notes)
        {
            float noteTime = CalcNoteTime(noteMaster, stageMaster);
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
                float endNoteTime = CalcNoteTime(endNote, stageMaster);
                longBall.Init(noteMaster, noteTime, endNoteTime);
                SetBallToList(longBall.StartBall);
                SetBallToList(longBall.EndBall);
            }
        }
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
            OnCountUp.OnNext((HitType.None, ball.BallType == BallType.LongStart ? 2 : 1));
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
            CreateMoveTween(launchBall);
        }
    }

    private void CreateMoveTween(SingleBall ball)
    {
        var tween = ball.transform.DOMove(ball.IsLeft ? _leftTransform.transform.position : _rightTransform.transform.position, MasterManager.SettingMaster.BallSpeed).SetEase(Ease.Linear);
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
    }

    public async UniTask StartResultAsync(ClearState clearState, ClearState highScoreClearState, float criticalMultiple, float hitMultiple, float missMultiple)
    {
        await UniTask.WaitForSeconds(_beforeReultWaitTime);
        _resultView.SetScore(clearState, highScoreClearState, criticalMultiple, hitMultiple, missMultiple);
        _resultView.gameObject.SetActive(true);
        BGMManager.instance.SetClip("Result", isLoop: true);
        BGMManager.instance.Play();
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
            OnCountUp.OnNext((HitType.None, 1));
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
        OnCountUp.OnNext((ball.JudgeBall(), 1));
    }

    private void CreateLetter(bool isLeft, HitType hitType)
    {
        var letter = Instantiate(_criticalPrefab, isLeft ? _leftLetterTransform : _rightLetterTransform);
        letter.InitAndStart(hitType, _cts.Token).Forget();
    }

    public void CountUpText(int criticalCount, int hitCount, int missCount, int comboCount)
    {
        _criticalCountText.text = criticalCount.ToString();
        _hitCountText.text = hitCount.ToString();
        _missCountText.text = missCount.ToString();
        _comboText.text = comboCount.ToString();
    }

    void OnDestroy()
    {
        _cts.Cancel();
        _cts = null;
    }
}
