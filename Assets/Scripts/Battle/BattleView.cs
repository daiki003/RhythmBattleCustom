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

    public void CountUp(HitType hitType, int count = 1)
    {
        if (hitType == HitType.Hit)
        {
            HitCount += count;
        }
        else if (hitType == HitType.Critical)
        {
            CriticalCount += count;
        }
        else
        {
            MissCount += count;
        }
    }

    public void Reset()
    {
        CriticalCount = 0;
        HitCount = 0;
        MissCount = 0;
    }
}

public enum BattleState
{
    None,
    StartBattle,
    DuringBgm,
    Result
}

public class BattleView : MonoBehaviour
{
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
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _backButton;

    [SerializeField] private BattlePracticeUI _practiceUi;
    [SerializeField] private ResultView _resultView;

    [SerializeField] private Image _leftTargetImage;
    [SerializeField] private Image _rightTargetImage;

    private List<SingleBall> _ballList = new();
    private List<SingleBall> _launchedBallStashList = new();

    private bool _isPausedBgm;
    private BattleState _currentState;
    private float _startBgmTime; // Bgm開始予定時間
    private float _startPauseTime; // ポーズを開始した時間
    private float _lastBeatTime = -1f;
    private bool _isPractice;
    private bool _isAdditional;
    private CancellationTokenSource _cts;
    private CancellationTokenSource _bgmStartCts;
    private StageHeader _stageHeader;
    private List<NoteMaster> _notes;
    // これが大きいと全体的に叩くのが遅い
    private float _beatDiffTime;
    private float[] _beatDiffTimeList = new float[10];

    private Vector3 _startToTargetVectorLeft => _leftTargetPoint.localPosition - _leftStartTransform.localPosition;
    private Vector3 _startToTargetVectorRight => _rightTargetPoint.localPosition - _rightStartTransform.localPosition;
    private Vector3 _leftEndPosition;
    private Vector3 _rightEndPosition;
    private float _targetDistance;
    private float _surplusDistance;
    private float _ballSpeed => (_stageHeader?.BPM * 4 + _stageHeader?.AdditionalBallSpeed) * (MasterManager.SettingMaster.BallSpeedCoefficient + SaveDataManager.SettingData.BallSpeed) ?? 1000f;
    // ボールが出現してからターゲットに到達するまでの時間
    private float _ballTimeOffset => _targetDistance / _ballSpeed;
    private float _currentTime
    {
        get
        {
            if (_currentState >= BattleState.DuringBgm)
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

    public void Init(StageHeader stageHeader, List<NoteMaster> notes, bool isPractice, bool isAdditional)
    {
        _cts = new CancellationTokenSource();
        _stageHeader = stageHeader;
        _notes = notes;
        _isPractice = isPractice;
        _isAdditional = isAdditional;

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
        // やり直しボタン
        _resetButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (_currentState == BattleState.Result)
            {
                return;
            }
            // 確認ダイアログ
            var option = new MessageDialogOption
            {
                TitleText = "やり直す",
                MessageText = "始めからやり直しますか？",
                OkButtonText = "やり直す",
            };
            DisplayDialog(option, async () =>
            {
                Pause(false);
                await PrepareBattle();
                BattleStart().Forget();
            });
        }).AddTo(this);
        // 戻るボタン
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (_currentState == BattleState.Result)
            {
                return;
            }
            // 確認ダイアログ
            var option = new MessageDialogOption
            {
                TitleText = "戻る",
                MessageText = _isAdditional ? "ステージ作成に戻りますか？" : "ホームに戻りますか？",
                OkButtonText = "戻る",
            };
            DisplayDialog(option, () =>
            {
                OnWhenClickedBack.OnNext(default);
            });
        }).AddTo(this);

        // リザルトパネルのボタン
        _resultView.Init();
        _resultView.OnWhenPushRestart.Subscribe(async _ =>
        {
            await PrepareBattle();
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
                if (_bgmStartCts != null)
                {
                    _bgmStartCts.Cancel();
                    _bgmStartCts.Dispose();
                    _bgmStartCts = null;
                    _currentState = BattleState.DuringBgm;
                }
                if (_currentState == BattleState.Result)
                {
                    _currentState = BattleState.DuringBgm;
                }
                Pause(isPause);
            }).AddTo(this);
            _practiceUi.OnClickScoreReset.Subscribe(_ =>
            {
                _currentScore.Reset();
                UpdateScoreText(_currentScore);
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
    }

    private void DisplayDialog(MessageDialogOption option, Action okAction)
    {
        Pause(isPause: true);
        var dialog = DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
        dialog.OnCloseDialog.Subscribe(result =>
        {
            if (result.ResultType == DialogResultType.Ok)
            {
                okAction();
            }
            else
            {
                Pause(isPause: false);
            }
        }).AddTo(this);
    }

    private void Pause(bool isPause)
    {
        if (_isPausedBgm == isPause) return;

        if (isPause)
        {
            _isPausedBgm = true;
            _startPauseTime = Time.time;
            BGMManager.instance.Pause();
            // 曲を止める場合はボールの動きを止める
            StopLaunchedBall();
            _practiceUi.SetSlider(BGMManager.instance.CurrentTimeLate);
        }
        else
        {
            if (_currentState < BattleState.DuringBgm)
            {
                // ポーズしてた時間分曲開始時間を遅らせる
                _startBgmTime += Time.time - _startPauseTime;
            }
            else
            {
                BGMManager.instance.Restart();
            }
            // 始める場合は動きを再開
            AttachMoveTween();
            _isPausedBgm = false;
        }
    }

    public void SetSliderForAdditional(float timeRate)
    {
        _currentState = BattleState.DuringBgm;
        _practiceUi.Pause(true);
        _practiceUi.SetSlider(timeRate);
        _practiceUi.RegisterTime(0);
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

    private float CalcNoteTime(NoteMaster noteMaster)
    {
        int noteNumber = noteMaster.num * (_stageHeader.LPB / noteMaster.lpb);
        return noteNumber * (60f / (_stageHeader.BPM * 4)) + _stageHeader.StartTime + SaveDataManager.SettingData.Offset;
    }

    public async UniTask PrepareBattle()
    {
        Reset();
        await BGMManager.instance.SetClipFromLibrary(_stageHeader.MusicId, immediatePlay: false);
        CreateBalls(_notes);
    }

    public async UniTask BattleStart()
    {
        _leftTargetImage.color = new Color(1, 1, 1, 0.5f);
        _rightTargetImage.color = new Color(1, 1, 1, 0.5f);
        _currentState = BattleState.StartBattle;
        _beatDiffTime = 0f;
        _beatDiffTimeList = new float[10];
        // BallTimeOffset分遅れてBGMスタート
        _startBgmTime = Time.time + _ballTimeOffset;
        _bgmStartCts = new CancellationTokenSource();
        await UniTask.WaitUntil(() => !_isPausedBgm && Time.time >= _startBgmTime, cancellationToken: _bgmStartCts.Token);
        BGMManager.instance.Play();
        _currentState = BattleState.DuringBgm;
        _bgmStartCts.Dispose();
        _bgmStartCts = null;
        // var leftFadeTask = _leftTargetImage.DOFade(SaveDataManager.SettingData.Target, 0.5f).SetEase(Ease.Linear);
        // var rightFadeTask = _rightTargetImage.DOFade(SaveDataManager.SettingData.Target, 0.5f).SetEase(Ease.Linear);
    }

    public void BattleStartFromMiddle()
    {
        _currentState = BattleState.DuringBgm;
    }

    public void CreateBalls(List<NoteMaster> notes)
    {
        // スタッシュしておいたボールをリストに入れる
        foreach (var ball in _launchedBallStashList)
        {
            _ballList.Add(ball);
        }
        foreach (NoteMaster noteMaster in notes)
        {
            float noteTime = CalcNoteTime(noteMaster);
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
        }).AddTo(this);
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
        var launchBallList = _ballList.Where(b => b.BallState == BallState.Wait && _currentTime.IsBetween(b.LaunchTime, b.CriticalTime + MasterManager.SettingMaster.HitTimeBuffer, isIncludeBound: true));
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
        return (_currentTime - ball.LaunchTime) / _ballTimeOffset;
    }

#endregion

    void Update()
    {
        if (_currentState < BattleState.StartBattle || _currentState == BattleState.Result || _isPausedBgm)
        {
            return;
        }
        if (_currentState == BattleState.DuringBgm && BGMManager.instance.CurrentTime >= _stageHeader.EndTime)
        {
            _currentState = BattleState.Result;
            if (_isPractice)
            {
                _practiceUi.Pause(true);
                _practiceUi.SetSlider(1f);
            }
            else
            {
                UniTask.Void(async () =>
                {
                    await BGMManager.instance.FadeOut(1f);
                    OnWhenFinishBattle.OnNext(_currentScore);
                });
            }
            return;
        }
        foreach (var ball in _ballList)
        {
            ball.ViewUpdate(_currentTime, _isPausedBgm);
        }
        if (_ballList.Count > 0)
        {
            LaunchBall();
        }
        if (_practiceUi.IsAuto)
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
        DebugPanel.instance.AddLog("BeatDiffTime " + _beatDiffTime);
        DebugPanel.instance.AddLog("BDTs " + _beatDiffTimeList[0].RoundDown(5) + " " + _beatDiffTimeList[1].RoundDown(5) + " " + _beatDiffTimeList[2].RoundDown(5) + " " + _beatDiffTimeList[3].RoundDown(5) + " " + _beatDiffTimeList[4].RoundDown(5) + " " + _beatDiffTimeList[5].RoundDown(5) + " " + _beatDiffTimeList[6].RoundDown(5) + " " + _beatDiffTimeList[7].RoundDown(5) + " " + _beatDiffTimeList[8].RoundDown(5) + " " + _beatDiffTimeList[9].RoundDown(5));
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
        var firstActiveBall = _ballList.FirstOrDefault(b => b.IsActive(_currentTime) && b.IsLeft == isLeft);
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
        if (firstBall != null && firstBall.BallType == BallType.LongEnd && firstBall.JudgeBall(_currentTime) == HitType.None)
        {
            firstBall.SetBallState(BallState.End);
            CreateLetter(isLeft, HitType.None);
            _currentScore.CountUp(HitType.None);
            UpdateScoreText(_currentScore);
        }
        var firstActiveBall = _ballList.FirstOrDefault(b => b.IsActive(_currentTime) && b.IsLeft == isLeft);
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
            SEManager.instance.PlaySe(SeName.Beat);
        }
        _lastBeatTime = ball.CriticalTime;
        float diffTime = _currentTime - ball.CriticalTime;
        _beatDiffTime += diffTime;
        _beatDiffTimeList[(int)_currentTime / 15] += diffTime;
        CreateLetter(ball.IsLeft, ball.JudgeBall(_currentTime));
        _currentScore.CountUp(ball.JudgeBall(_currentTime));
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
    }

    void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        if (_bgmStartCts != null)
        {
            _bgmStartCts.Cancel();
            _bgmStartCts.Dispose();
            _bgmStartCts = null;
        }
    }
}
