using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Linq;
using DG.Tweening;
using System.Threading;

public class Score
{
    public int CriticalCount;
    public int HitCount;
    public int MissCount;
    public int ComboCount;
    public int MaxComboCount;

    public void CountUp(HitType hitType, int count = 1)
    {
        if (hitType == HitType.Hit)
        {
            HitCount += count;
            ComboCount += count;
        }
        else if (hitType == HitType.Critical)
        {
            CriticalCount += count;
            ComboCount += count;
        }
        else
        {
            MissCount += count;
            MaxComboCount = Math.Max(ComboCount, MaxComboCount);
            ComboCount = 0;
        }
    }
}

public class BattleView : MonoBehaviour
{
    [SerializeField] private Image _enemyImage;
    [SerializeField] private Transform _leftTargetPoint;
    [SerializeField] private Transform _rightTargetPoint;
    [SerializeField] private Transform _leftStartTransform;
    [SerializeField] private Transform _rightStartTransform;
    [SerializeField] private Transform _leftLetterTransform;
    [SerializeField] private Transform _rightLetterTransform;
    [SerializeField] private Transform _ballTransform;

    [SerializeField] private SingleBall _ballPrefab;
    [SerializeField] private LongBall _longBallPrefab;
    [SerializeField] private Critical _criticalPrefab;

    [SerializeField] private Text _criticalCountText;
    [SerializeField] private Text _hitCountText;
    [SerializeField] private Text _missCountText;
    [SerializeField] private Text _comboText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _testButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _testButtonImage;

    [SerializeField] private BattlePracticeUI _practiceUi;
    [SerializeField] private ResultView _resultView;

    private List<SingleBall> _ballList = new();
    private List<SingleBall> _launchedBallStashList = new();

    private bool _isTest;
    public bool IsTest => _isTest;
    private bool _isStartBattle;
    private bool _isFinishBattle;
    private bool _isStartBgm;
    private bool _isPausedBgm;
    private float _startBgmTime;
    private float _lastBeatTime;
    private bool _isPractice;
    private CancellationTokenSource _cts;
    private SingleStageMaster _stageMaster;

    private Vector3 _startToTargetVectorLeft => _leftTargetPoint.localPosition - _leftStartTransform.localPosition;
    private Vector3 _startToTargetVectorRight => _rightTargetPoint.localPosition - _rightStartTransform.localPosition;
    private Vector3 _leftEndPosition;
    private Vector3 _rightEndPosition;
    private float _targetDistance;
    private float _surplusDistance;
    private float _ballSpeed => _stageMaster?.BPM * MasterManager.SettingMaster.BallSpeedCoefficient ?? 1000f;
    private float _ballTimeOffset => _targetDistance / _ballSpeed;
    private float _currentTime
    {
        get
        {
            if (_isStartBgm)
            {
                return BGMManager.instance.IsPlaying ? BGMManager.instance.CurrentTime : BGMManager.instance.Length * _practiceUi.SliderValue;
            }
            return Time.time - _startBgmTime;
        }
    }

    // スコア
    private Score _currentScore;

    public Subject<Unit> OnWhenClickedBack { get; private set; } = new Subject<Unit>();
    public Subject<Score> OnWhenFinishBattle { get; private set; } = new Subject<Score>();

    private const float _beforeReultWaitTime = 1f;

    public void Init(SingleStageMaster singleStageMaster, bool isPractice)
    {
        _cts = new CancellationTokenSource();
        _stageMaster = singleStageMaster;
        _isPractice = isPractice;

        _leftEndPosition = _leftStartTransform.localPosition + _startToTargetVectorLeft * 1.5f;
        _rightEndPosition = _rightStartTransform.localPosition + _startToTargetVectorRight * 1.5f;
        _targetDistance = _startToTargetVectorLeft.magnitude;
        _surplusDistance = Vector3.Distance(_leftTargetPoint.localPosition, _leftEndPosition);

        GameManager.instance.ClickHandler.OnClickButton.Subscribe(isLeft =>
        {
            OnClickButton(isLeft);
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnReleaseButton.Subscribe(isLeft =>
        {
            OnReleaseButton(isLeft);
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnDragBattleBg.Subscribe(move =>
        {
            if(_isPausedBgm)
            {
                _practiceUi.MoveSlider(move / -100000f);
            }
        }).AddTo(this);
        _resetButton.OnClickAsObservable().Subscribe(_ =>
        {
            PrepareBattle();
            BattleStart().Forget();
        }).AddTo(this);
        _testButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeTest();
        }).AddTo(this);
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnWhenClickedBack.OnNext(default);
        }).AddTo(this);
        _resultView.OnWhenPushRestart.Subscribe(_ =>
        {
            PrepareBattle();
            BattleStart().Forget();
        }).AddTo(this);
        _resultView.OnWhenPushGoHome.Subscribe(_ =>
        {
            OnWhenClickedBack.OnNext(default);
        }).AddTo(this);

        _practiceUi.gameObject.SetActive(_isPractice);
        if (_isPractice)
        {
            _practiceUi.Init();
            _practiceUi.OnClickPauseButton.Subscribe(isPause =>
            {
                _isPausedBgm = isPause;
                if (isPause)
                {
                    BGMManager.instance.Pause();
                    // 曲を止める場合はボールの動きを止める
                    StopLaunchedBall();
                    _practiceUi.SetSlider(BGMManager.instance.CurrentTimeLate);
                }
                else
                {
                    // 始める場合は動きを再開
                    AttachMoveTween();
                    BGMManager.instance.Play();
                }
            }).AddTo(this);
            _practiceUi.OnSliderValueChange.Subscribe(x =>
            {
                float time = BGMManager.instance.Length * x;
                BGMManager.instance.SetTime(time);
                RefreshBalls(time);
            }).AddTo(this);
            _practiceUi.OnTimeJump.Subscribe(timeRate =>
            {
                _practiceUi.SetSlider(timeRate);
            }).AddTo(this);
        }

        _resultView.gameObject.SetActive(false);
        _enemyImage.sprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + _stageMaster.StageId);
    }

    public void DestroyAllObjectInList(bool withoutLaunched = false)
    {
        while (_ballList.Count > 0)
        {
            var ball = _ballList[0];
            _ballList.RemoveAt(0);
            if (withoutLaunched && ball.BallState == BallState.Launched)
            {
                // 発射されたボールを破棄しない場合、スタッシュしておく
                _launchedBallStashList.Add(ball);
            }
            else
            {
                Destroy(ball.gameObject);
            }
        }
    }

    private void StopLaunchedBall()
    {
        foreach (var ball in _ballList)
        {
            if (ball.BallState == BallState.Launched)
            {
                ball.StopMove();
            }
        }
    }

    public void Reset()
    {
        DestroyAllObjectInList();
        _currentScore = new Score();
        UpdateScoreText(_currentScore);
        BGMManager.instance.Stop();
    }

    public void ChangeTest()
    {
        _isTest = !_isTest;
        _testButtonImage.color = _isTest ? Color.black : Color.white;
    }

    private float CalcNoteTime(NoteMaster noteMaster)
    {
        int noteNumber = noteMaster.num * (_stageMaster.LPB / noteMaster.lpb);
        return noteNumber * (60f / _stageMaster.BPM) + _stageMaster.NoteTimeOffset + GameManager.instance.SettingOffset;
    }

    public void PrepareBattle()
    {
        Reset();
        BGMManager.instance.SetClip(_stageMaster.StageId, immediatePlay: false);
        CreateBalls(_stageMaster.notes);
    }

    public async UniTask BattleStart()
    {
        _isStartBattle = true;
        _isFinishBattle = false;
        _isStartBgm = false;
        // BallTimeOffset分遅れてBGMスタート
        _startBgmTime = Time.time + MasterManager.SettingMaster.BallTimeOffset;
        await UniTask.WaitUntil(() => Time.time >= _startBgmTime);
        BGMManager.instance.Play();
        _isStartBgm = true;
    }

    public void BattleStartFromMiddle(float timeRate)
    {
        _isStartBattle = true;
        _isFinishBattle = false;
        _isStartBgm = true;
        _practiceUi.Pause(true);
        _practiceUi.SetSlider(timeRate);
        _practiceUi.RegisterTime(0);
    }

    public void CreateBalls(List<NoteMaster> notes, float startTime = 0f)
    {
        // スタッシュしておいたボールをリストに入れる
        foreach (var ball in _launchedBallStashList)
        {
            _ballList.Add(ball);
        }
        foreach (NoteMaster noteMaster in notes)
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
                newBall.Init(noteMaster, noteTime, BallType.Single, _ballTimeOffset);
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
                longBall.Init(noteMaster, noteTime, endNoteTime, _ballTimeOffset);
                SetBallToList(longBall.StartBall);
                SetBallToList(longBall.EndBall);
            }
        }
    }

    // ボールをリストに入れる
    private void SetBallToList(SingleBall ball, bool isInsert = false)
    {
        if (isInsert)
        {
            _ballList.Insert(0, ball);
        }
        else
        {
            _ballList.Add(ball);
        }
        // ボールを打ち損ねたらミス判定
        ball.OnWhenMiss.Subscribe(_ => 
        {
            CreateLetter(ball.IsLeft, HitType.None);
            _currentScore.CountUp(HitType.None, ball.BallType == BallType.LongStart ? 2 : 1);
            UpdateScoreText(_currentScore);
        });
    }

    private void AttachMoveTween()
    {
        foreach (var ball in _ballList)
        {
            if (ball.BallState == BallState.Launched)
            {
                CreateMoveTween(ball);
            }
        }
    }

    private void RefreshBalls(float time)
    {
        var currentTime = time;
        foreach (var ball in _ballList)
        {
            var startTransform = ball.IsLeft ? _leftStartTransform : _rightStartTransform;
            var startToTargetVector = ball.IsLeft ? _startToTargetVectorLeft : _startToTargetVectorRight;
            var ballState = ball.GetShouldBeState(currentTime);
            ball.SetBallState(ballState);
            if (ballState >= BallState.Holded)
            {
                ball.transform.localPosition = startTransform.localPosition + startToTargetVector;
            }
            else if (ballState == BallState.Launched)
            {
                ball.transform.localPosition = startTransform.localPosition + startToTargetVector * GetPositionRate(ball);
            }
            else if (ballState == BallState.Wait)
            {
                ball.transform.localPosition = startTransform.localPosition;
            }
        }
    }

#region ボールの移動関連

    private void LaunchBall()
    {
        var launchBallList = _ballList.Where(b => b.BallState == BallState.Wait && _currentTime >= b.LaunchTime);
        foreach (var ball in launchBallList)
        {
            ball.SetBallState(BallState.Launched);
            CreateMoveTween(ball);
        }
    }

    private void CreateMoveTween(SingleBall ball)
    {
        if (ball.MoveTween == null)
        {
            float distance = _targetDistance * (1 - GetPositionRate(ball)) + _surplusDistance;
            var tween = ball.transform.DOLocalMove(ball.IsLeft ? _leftEndPosition : _rightEndPosition, distance / _ballSpeed).SetEase(Ease.Linear);
            ball.SetTween(tween);
        }
    }

    private float GetPositionRate(SingleBall ball)
    {
        return (_currentTime - ball.LaunchTime) / _ballTimeOffset;;
    }

#endregion

    void Update()
    {
        if (!_isStartBattle)
        {
            return;
        }
        if (BGMManager.instance.IsFinishBgm && !_isFinishBattle && !_isPractice)
        {
            _isFinishBattle = true;
            OnWhenFinishBattle.OnNext(_currentScore);
            return;
        }
        foreach (var ball in _ballList)
        {
            ball.ViewUpdate(BGMManager.instance.CurrentTime, _isPausedBgm);
        }
        if (_isPausedBgm)
        {
            return;
        }
        if (_ballList.Count > 0)
        {
            LaunchBall();
        }
        if (_isTest)
        {
            var activeBallList = _ballList.Where(b => Mathf.Abs(b.CriticalTime - BGMManager.instance.CurrentTime) < MasterManager.SettingMaster.TestNoteTimeBuffer);
            foreach (var ball in activeBallList)
            {
                if (ball.BallType == BallType.LongEnd)
                {
                    OnReleaseButton(ball.IsLeft);
                }
                else
                {
                    OnClickButton(ball.IsLeft);
                }
            }
        }
    }

    public async UniTask StartResultAsync(ClearState clearState, ClearState highScoreClearState, float criticalMultiple, float hitMultiple, float missMultiple)
    {
        await UniTask.WaitForSeconds(_beforeReultWaitTime, cancellationToken: _cts.Token);
        _resultView.SetScore(clearState, highScoreClearState, criticalMultiple, hitMultiple, missMultiple);
        _resultView.gameObject.SetActive(true);
        BGMManager.instance.SetClip(BgmName.Result, isLoop: true);
    }

    private void OnClickButton(bool isLeft)
    {
        if (_isPausedBgm)
        {
            return;
        }
        float time = BGMManager.instance.CurrentTime;
        var firstActiveBall = _ballList.FirstOrDefault(b => b.IsActive && b.IsLeft == isLeft);
        if (firstActiveBall != null)
        {
            if (firstActiveBall.BallType == BallType.LongEnd)
            {
                return;
            }
            BeatBall(firstActiveBall, isRelease: false);
        }
    }

    private void OnReleaseButton(bool isLeft)
    {
        if (_isPausedBgm)
        {
            return;
        }
        float time = BGMManager.instance.CurrentTime;
        var firstBall = _ballList.FirstOrDefault(b => b.IsAlive && b.IsLeft == isLeft);
        // ロングノーツの終端の前で離したらそれを破棄
        if (firstBall != null && firstBall.BallType == BallType.LongEnd && firstBall.JudgeBall() == HitType.None)
        {
            firstBall.SetBallState(BallState.End);
            CreateLetter(isLeft, HitType.None);
            _currentScore.CountUp(HitType.None);
            UpdateScoreText(_currentScore);
        }
        var firstActiveBall = _ballList.FirstOrDefault(b => b.IsActive && b.IsLeft == isLeft);
        if (firstActiveBall != null)
        {
            if (firstActiveBall.BallType != BallType.LongEnd)
            {
                return;
            }
            BeatBall(firstActiveBall, isRelease: true);
        }
    }

    // ボールを打った時の処理
    private void BeatBall(SingleBall ball, bool isRelease)
    {
        if (_lastBeatTime != ball.CriticalTime)
        {
            SEManager.instance.PlayBeatSe();
        }
        _lastBeatTime = ball.CriticalTime;
        CreateLetter(ball.IsLeft, ball.JudgeBall());
        _currentScore.CountUp(ball.JudgeBall());
        UpdateScoreText(_currentScore);
        ball.AfterBeat(isRelease);
    }

    private void CreateLetter(bool isLeft, HitType hitType)
    {
        var letter = Instantiate(_criticalPrefab, isLeft ? _leftLetterTransform : _rightLetterTransform);
        letter.InitAndStart(hitType, _cts.Token).Forget();
    }

    public void UpdateScoreText(Score score)
    {
        _criticalCountText.text = score.CriticalCount.ToString();
        _hitCountText.text = score.HitCount.ToString();
        _missCountText.text = score.MissCount.ToString();
        _comboText.text = score.ComboCount.ToString();
    }

    void OnDestroy()
    {
        _cts.Cancel();
        _cts = null;
    }
}
