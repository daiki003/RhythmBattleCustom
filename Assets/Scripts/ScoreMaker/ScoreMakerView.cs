using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;

public class ChangeParameter
{
    public float? Bpm;
    public float? StartTime;
    public float? EndTime;
    public int? BeatNumber;
    public (int measure, int diff)? ModulationChange;
    public bool ResetModulation;
}

public class ScoreMakerView : MonoBehaviour
{
    [SerializeField] private Transform _scoreLineTransform;
    [SerializeField] private ScoreLine _scoreLinePrefab;
    [SerializeField] private ScrollRect _scoreScrollRect;
    [SerializeField] private Button _helpButton;
    [SerializeField] private Button _backButton;

    [SerializeField] private RectTransform _scoreAreaRect;
    [SerializeField] private VerticalLayoutGroup _scoreAreaLayoutGroup;
    [SerializeField] private Scrollbar _bgmScrollBar;

    [SerializeField] private JumpButtonIcon _jumpIconPrefab;
    [SerializeField] private RectTransform _moveButtonArea;
    [SerializeField] private Button _playBgmButton; // BGM再生ボタン
    [SerializeField] private ValueAdjuster _pitchAdjuster; // ピッチ変更
    private float _basePitch;

    // コントロールパネル
    [SerializeField] private ScoreMakerControlPanel _controlPanel;
    [SerializeField] private ControlPanelPageBall _pageBall;
    [SerializeField] private ControlPanelPageMask _maskControl;

    [SerializeField] private GameObject _selectMask;
    private List<ScoreLine> _selectedLineList = new();
    private LinePocket _selectedBallPocket;

    private bool _isPlayMakeMode;
    // 演奏作成モードでの開始位置
    private float _startPlayMakeLineNumber;
    private List<LineState> _startPlayMakeLineList;

    public class LineState
    {
        public ScoreMakerBallType LeftBallType;
        public ScoreMakerBallType RightBallType;
    }

    // コピペ関連
    private List<LineState> _copiedLineState = new();

    private List<ScoreLine> _scoreLineList = new();
    private List<List<LineState>> _pastScoreLineList = new(); // 過去のスコアラインの状態を保持したのリスト（Undo用）
    private List<ScoreMakerBallLine> _longBallLineList = new(); // 作ったロングボール間の線のリスト
    private Dictionary<int, JumpButtonIcon> _timeStampList = new(); // タイムジャンプの位置スタンプ

    private float _currentBpm;
    private float _currentStartTime;
    private float _currentEndTime;
    private int _beatsNumber;
    private Dictionary<int, int> _modulationDict = new(); // 変調する位置の指定パラメータ

    // 編集された状態がセーブされていないかどうか
    private bool _isEdited;
    private bool _isPause = true;

    private Subject<float> _clickPracticeButton = new();
    public Observable<float> ClickPracticeButton => _clickPracticeButton;
    private Subject<(string, bool)> _onSave = new();
    public Observable<(string, bool)> OnSave => _onSave;
    private Subject<bool> _clickSaveButton = new();
    public Observable<bool> ClickSaveButton => _clickSaveButton;
    private Subject<ChangeParameter> _onChangeParameter = new();
    public Observable<ChangeParameter> OnChangeParameter => _onChangeParameter;
    private Subject<TutorialCommandList> _startTutorial = new();
    public Observable<TutorialCommandList> StartTutorial => _startTutorial;

    private const float _scoreAreaBottom = -1660f;
    private const float _scoreLineHeight = 15f;
    private const float _selectMaskOffset = 50f;
    private const float _maxLineSpacing = 300f; // ライン間隔最大値
    private const int _maxLastPastLineCount = 20;

    private float _currentTime => BGMManager.instance.CurrentTime;

    public enum ScoreMakerBallType
    {
        None,
        Single,
        Long
    }
    private OperationType _currentOperationType;
    // 実質的な操作モードを取得
    private OperationType GetPracticallyOperationType()
    {
        // ハイブリッド以外はそのまま返す
        if (_currentOperationType != OperationType.Hybrid)
        {
            return _currentOperationType;
        }
        // 選択中の線があればライン選択モードとして扱う
        if (_selectedLineList.Count > 0)
        {
            return OperationType.SelectLine;
        }
        // 選択中のポケットがあればボール選択モードとして扱う
        if (_selectedBallPocket != null)
        {
            return OperationType.Ball;
        }
        return OperationType.Hybrid;
    }

    private float _singleBeatTime => 60f / (_currentBpm * 4);
    private bool _isStartMake;
    private bool _isTutorial;

    private Dictionary<bool, int> _lastBeatLineDict = new();

    public void Init(List<NoteMaster> notes, int lineNumber, bool isTutorial, bool isCanOverriteSave)
    {
        _isTutorial = isTutorial;
        BGMManager.instance.SetTime(_currentStartTime);

        _currentOperationType = OperationType.Hybrid;
        StartSubscribeMain();

        _controlPanel.OnRequest.Subscribe(request =>
        {
            RunControlPanelRequest(request);
        }).AddTo(this);

        _selectMask.SetActive(false);
        _controlPanel.Init(isCanOverriteSave);

        AdjustmentLineNumber(lineNumber);
        CreateLine(notes);
        _scoreScrollRect.verticalNormalizedPosition = 0;
        _isEdited = false;
    }

    public void SetHeaderParameter(StageHeader stageHeader)
    {
        _currentBpm = stageHeader.BPM;
        _currentStartTime = stageHeader.StartTime;
        _currentEndTime = stageHeader.EndTime;
        _beatsNumber = stageHeader.BeatsNumber;
        _modulationDict = stageHeader.ModulationDict?.Where(kv => int.TryParse(kv.Key, out _))
            .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value) ?? new Dictionary<int, int>();
        _controlPanel.SetParameter(stageHeader, _isTutorial);
        foreach (var timeJump in stageHeader.TimeJumpDict)
        {
            if (int.TryParse(timeJump.Key, out var index))
            {
                _controlPanel.SetJumpTimeRate(index, timeJump.Value);
                CreateTimeJumpStamp(index, timeJump.Value);
            }
        }
        SetFirstBeatNumberText();
    }

    private void StartSubscribeMain()
    {
        // 基本操作系
        GameManager.Instance.ClickHandler.OnClickScoreMakerNarrowPocket.Subscribe(x =>
        {
            OnClickLine(ClickLineType.NorrowPocket, x.number, x.isLeft);
        }).AddTo(this);
        GameManager.Instance.ClickHandler.OnClickScoreLinePocket.Subscribe(x =>
        {
            OnClickLine(ClickLineType.Pocket, x.number, x.isLeft);
        }).AddTo(this);
        GameManager.Instance.ClickHandler.OnClickScoreLine.Subscribe(number =>
        {
            OnClickLine(ClickLineType.Line, number);
        }).AddTo(this);
        GameManager.Instance.ClickHandler.OnClickButton.Subscribe(isLeft =>
        {
            if (!gameObject.activeSelf) return;
            if (!_isPlayMakeMode) return;
            int currentLine = Mathf.RoundToInt(GetCurrentLineNumber());
            _lastBeatLineDict[isLeft] = currentLine;
            CreateBall(currentLine, isLeft, ScoreMakerBallType.Single);
            SEManager.instance.PlaySe(SeName.Beat);
        }).AddTo(this);

        _playBgmButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePause(!_isPause);
        }).AddTo(this);
        _helpButton.OnClickAsObservable().Subscribe(async _ =>
        {
            var commandList = TutorialManager.Instance.GetTutorialCommandLists(TutorialType.ScoreMaker);
            var dialogResult = await DialogManager.instance.OpenTutorialDialogAsync(commandList);
            switch (dialogResult.ResultType)
            {
                case DialogResultType.Ok:
                    _startTutorial.OnNext(dialogResult.SelectedCommand);
                    break;
            }
        }).AddTo(this);
        _backButton.OnClickAsObservable().Subscribe(async _ =>
        {
            ChangePause(true);
            BGMManager.instance.Pause();
            if (_isEdited)
            {
                // 編集が保存されていなかったら確認ダイアログを出す
                var dialogResult = await DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(
                    new MessageDialogOption
                    {
                        TitleText = "ホームに戻る",
                        MessageText = "変更が保存されていませんが、このままホームに戻りますか？",
                        OkButtonText = "戻る",
                        IsBgCancel = false,
                    }
                );
                if (dialogResult.ResultType == DialogResultType.Ok)
                {
                    GameManager.Instance.OpenScene(SceneType.Home, new HomeSceneInfo()).Forget();
                }
                return;
            }
            GameManager.Instance.OpenScene(SceneType.Home, new HomeSceneInfo()).Forget();
        }).AddTo(this);

        _basePitch = BGMManager.instance.CurrentPitch;
        _pitchAdjuster.Init(1f, 0.1f, 1);
        _pitchAdjuster.CurrentValue.Subscribe(value =>
        {
            BGMManager.instance.SetPitch(_basePitch * value);
        }).AddTo(this);
    }

    private void OnClickLine(ClickLineType clickType, int lineNumber, bool isLeft = false)
    {
        if (!gameObject.activeSelf) return;

        // ペーストモードならペーストして終了
        if (_controlPanel.IsPasteMode)
        {
            SEManager.instance.PlaySe(SeName.Button2);
            PasteLine(lineNumber);
            return;
        }
        var operationType = GetPracticallyOperationType();
        switch (operationType)
        {
            case OperationType.SelectLine:
                // SelectLineとPasteはラインクリック固定
                ClickLine(lineNumber);
                break;
            case OperationType.Ball:
                if (clickType != ClickLineType.Line)
                {
                    // BallはLineクリック以外のみポケットとして拾う
                    ClickLinePocket(lineNumber, isLeft);
                }
                break;
            case OperationType.Hybrid:
                if (clickType == ClickLineType.NorrowPocket)
                {
                    // HybridはNorrowPocketのみポケットクリックとして扱う
                    ClickLinePocket(lineNumber, isLeft);
                }
                else
                {
                    // それ以外はラインクリックとして扱う
                    ClickLine(lineNumber);
                }
                break;
            default:
                return;
        }
    }

    private void ClickLinePocket(int lineNumber, bool isLeft)
    {
        TutorialManager.Instance.AdvanceStep("ClickLine" + (isLeft ? "Left" : "Right"));
        SEManager.instance.PlaySe(SeName.Button2);
        var ball = _scoreLineList[lineNumber].GetBall(isLeft);
        if (ball != null)
        {
            // ボールがあるところならボール選択
            ClickBall(lineNumber, isLeft);
        }
        else if (_selectedBallPocket != null)
        {
            UpdatePastScoreLineList();
            ScoreMakerBall pairBall = null;
            if (_selectedBallPocket.InstalledBall.BallType == ScoreMakerBallType.Long)
            {
                // 選択中のボールがロングだった場合は、後でつなぎなおすのでペアを保存
                pairBall = _selectedBallPocket.InstalledBall.PairBall;
                pairBall.ResetPair(isForce: true);
            }
            // ボール選択中なら、そのボールをここに移動する
            _selectedBallPocket.Clicked(ScoreMakerBallType.None);
            // クリックした列にペアがあるならつなぎなおし
            if (pairBall != null && pairBall.IsLeft == isLeft)
            {
                var newBall = CreateBall(lineNumber, isLeft, ScoreMakerBallType.Long);
                pairBall.ChangeBallType(ScoreMakerBallType.Long);
                ConnectBall(newBall, pairBall);
                // UniTask.Void(async () =>
                // {
                //     // ペア解消がOnDestroyで行われるので1フレーム待つ
                //     await UniTask.NextFrame();
                //     var newBall = CreateBall(lineNumber, isLeft, ScoreMakerBallType.Long);
                //     pairBall.ChangeBallType(ScoreMakerBallType.Long);
                //     ConnectBall(newBall, pairBall);
                // });
            }
            else
            {
                CreateBall(lineNumber, isLeft, ScoreMakerBallType.Single);
            }
            _selectedBallPocket.SelectBall(false);
            _selectedBallPocket = null;
        }
        else
        {
            UpdatePastScoreLineList();
            // ボール作成
            CreateBall(lineNumber, isLeft, ScoreMakerBallType.Single);
        }
    }

    private void ClickBall(int lineNumber, bool isLeft)
    {
        var targetLine = _scoreLineList[lineNumber];
        if (_selectedBallPocket != null)
        {
            // 同じラインのボールが選択されている場合
            if (lineNumber == _selectedBallPocket.Number)
            {
                if (isLeft == _selectedBallPocket.IsLeft)
                {
                    // 同じポケットが選択された場合は選択解除
                    _selectedBallPocket.SelectBall(false);
                    _selectedBallPocket = null;
                }
                else
                {
                    // 隣のポケットなら選択入れ替え
                    _selectedBallPocket.SelectBall(false);
                    _selectedBallPocket = isLeft ? targetLine.LeftPocket : targetLine.RightPocket;
                    _selectedBallPocket.SelectBall(true);
                }
                return;
            }
            UpdatePastScoreLineList();
            // 他のラインの選択されているボールがある場合、これとくっつける
            // 左右逆だった場合、今回選択したボールを逆サイドに移動
            if (isLeft != _selectedBallPocket.IsLeft)
            {
                InversionLine(targetLine);
            }
            var targetBall = targetLine.GetBall(_selectedBallPocket.IsLeft);
            // いったんペア解消してから、くっついていなければくっつける
            // 既にくっついているペアなら解消する
            if (_selectedBallPocket.InstalledBall.PairBall != null && _selectedBallPocket.InstalledBall.PairBall == targetBall)
            {
                _selectedBallPocket.InstalledBall.DestroyLine();
            }
            else
            {
                _selectedBallPocket.InstalledBall.DestroyLine();
                _selectedBallPocket.RecreateBall(ScoreMakerBallType.Long);
                targetLine.RecreateBall(ScoreMakerBallType.Long, _selectedBallPocket.IsLeft);
                ConnectBall(_selectedBallPocket.InstalledBall, targetBall);
            }
            _selectedBallPocket.SelectBall(false);
            _selectedBallPocket = null;
        }
        else
        {
            _selectedBallPocket = isLeft ? targetLine.LeftPocket : targetLine.RightPocket;
            _selectedBallPocket.SelectBall(true);
        }
    }

    private void ClickLine(int lineNumber)
    {
        TutorialManager.Instance.AdvanceStep("ClickLine");
        SEManager.instance.PlaySe(SeName.Button2);
        SelectLine(lineNumber);
    }

    #region ダイアログ系
    // セーブ確認のダイアログ表示
    public async UniTask DisplaySaveDialog(string stageName, bool isNewSave)
    {
        var dialogResult = await CreateSaveDialog(stageName, isNewSave);
        if (dialogResult is InputDialogResult inputResult)
        {
            stageName = inputResult.StageName;
        }
        if (dialogResult.ResultType == DialogResultType.Ok)
        {
            _onSave.OnNext((stageName, isNewSave));
            // 新規保存後は上書き保存可能にする
            _controlPanel.ChangeCanOverriteSave(true);
        }
    }

    // セーブ確認のダイアログ作成
    public async UniTask<DialogResultBase> CreateSaveDialog(string stageName, bool isNewSave)
    {
        if (isNewSave)
        {
            // 新規作成ならステージ名入力ダイアログを出す
            var option = new InputDialogOption
            {
                TitleText = "新規保存",
                OkButtonText = "決定",
                MessageText = "ステージ名を入力してください",
                PlaceHolderText = stageName,
                InitialInputText = stageName,
                IsBgCancel = false,
            };
            return await DialogManager.instance.ShowDialogAsync<InputDialog, InputDialogResult>(option);
        }
        else
        {
            // 確認ダイアログを出す
            var option = new MessageDialogOption
            {
                TitleText = "上書き保存",
                MessageText = "変更すると\nこのレベルのハイスコアは削除されます。\n変更を保存しますか？",
                OkButtonText = "保存する",
                IsBgCancel = false,
            };
            return await DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(option);
        }
    }

    // セーブ完了通知ダイアログ
    public async UniTask DisplaySaveFinishDialog()
    {
        await DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(
            new MessageDialogOption
            {
                TitleText = "保存完了",
                OkButtonText = "OK",
                HideCancelButton = true,
                MessageText = "保存しました",
            }
        );
        _isEdited = false;
    }
    #endregion

    private void RunControlPanelRequest(ControlPanelRequestBase request)
    {
        switch (request)
        {
            case ControlPanelRequestSelectBall selectBallRequest:
                _currentOperationType = selectBallRequest.OperationType;
                break;
            case ControlPanelRequestCopy _:
                CopyLine();
                break;
            case ControlPanelRequestCloseMask _:
                _selectedLineList.Clear();
                _selectMask.SetActive(false);
                _controlPanel.FinishPaste();
                break;
            case ControlPanelRequestInversion _:
                InversionSelectedLine();
                break;
            case ControlPanelRequestUp _:
                MoveSelectedLine(isUp: true);
                break;
            case ControlPanelRequestDown _:
                MoveSelectedLine(isUp: false);
                break;
            case ControlPanelRequestDeleteRange _:
                ClearLine();
                break;
            // オプションページ
            case ControlPanelRequestEvenlySpaced _:
                bool isLeft = false;
                for (int i = 0; i < _scoreLineList.Count; i++)
                {
                    _scoreLineList[i].ClearLine();
                    // _beatsNumberの倍数で配置
                    if (i % _beatsNumber != 0)
                    {
                        continue;
                    }
                    CreateBall(i, isLeft, ScoreMakerBallType.Single);
                    // 左右交互に配置
                    isLeft = !isLeft;
                }
                break;
            case ControlPanelRequestPlayMake _:
                _startPlayMakeLineNumber = GetCurrentLineNumber();
                _startPlayMakeLineList = GetCurrentLineList();
                _controlPanel.SetModePanel(isPlayMakeMode: true);
                _isPlayMakeMode = true;
                break;
            case ControlPanelRequestPractice _:
                _isStartMake = false;
                ChangePause(true);
                _clickPracticeButton.OnNext(_bgmScrollBar.value);
                break;
            case ControlPanelRequestSave _:
                _clickSaveButton.OnNext(false);
                break;
            case ControlPanelRequestNewSave _:
                _clickSaveButton.OnNext(true);
                break;
            case ControlPanelRequestAllClear _:
                for (int i = 0; i < _scoreLineList.Count; i++)
                {
                    _scoreLineList[i].ClearLine();
                }
                break;
            case ControlPanelRequestChangeBpm changeBpm:
                _onChangeParameter.OnNext(new ChangeParameter { Bpm = changeBpm.Bpm });
                break;
            case ControlPanelRequestChangeStartTime changeStartTime:
                _onChangeParameter.OnNext(new ChangeParameter { StartTime = changeStartTime.StartTime });
                break;
            case ControlPanelRequestChangeEndTime changeEndTime:
                _onChangeParameter.OnNext(new ChangeParameter { EndTime = changeEndTime.EndTime });
                break;
            case ControlPanelRequestBeatsNumber beatsNumber:
                _onChangeParameter.OnNext(new ChangeParameter { BeatNumber = beatsNumber.BeatsNumber });
                break;
            case ControlPanelRequestModulationChange modulationChange:
                int diff = modulationChange.IsForward ? -1 : 1;
                var modulationNum = _modulationDict.GetValueOrDefault(modulationChange.Measure) + diff;
                // 1つ前の拍子と同じところまでは下げられない
                if (modulationNum <= _beatsNumber * -1)
                {
                    return;
                }
                _onChangeParameter.OnNext(new ChangeParameter { ModulationChange = (modulationChange.Measure, diff) });
                break;
            case ControlPanelRequestResetModulation _:
                _onChangeParameter.OnNext(new ChangeParameter { ResetModulation = true });
                break;
            case ControlPanelRequestUndo _:
                Undo();
                break;

            case ControlPanelRequestFinishPlayMake _:
                UpdatePastScoreLineList();
                _controlPanel.SetModePanel(isPlayMakeMode: false);
                _isPlayMakeMode = false;
                break;
            case ControlPanelRequestResetPlayMake _:
                ChangePause(true);
                CreateBallFromLineState(_startPlayMakeLineList, startNumber: 0);
                SetPositionByLineNumber(_startPlayMakeLineNumber);
                break;

            case ControlPanelRequestTimeJump timeJump:
                _bgmScrollBar.value = timeJump.TimeRate;
                break;
            case ControlPanelRequestRegisterTimeJump registerTimeJump:
                _controlPanel.SetJumpTimeRate(registerTimeJump.Index, _bgmScrollBar.value);
                CreateTimeJumpStamp(registerTimeJump.Index, _bgmScrollBar.value);
                break;
            case ControlPanelRequestDeleteTimeJump deleteTimeJump:
                _controlPanel.SetJumpTimeRate(deleteTimeJump.Index, -1);
                if (_timeStampList.TryGetValue(deleteTimeJump.Index, out var targetButton))
                {
                    _timeStampList.Remove(deleteTimeJump.Index);
                    targetButton.Destroy();
                }
                break;
        }
    }

    private void CreateTimeJumpStamp(int index, float timeRate)
    {
        float width = _moveButtonArea.rect.width;
        if (_timeStampList.TryGetValue(index, out var existingButton))
        {
            existingButton.SetPosX(timeRate, width);
        }
        else
        {
            var stamp = Instantiate(_jumpIconPrefab, _moveButtonArea);
            _timeStampList[index] = stamp;
            stamp.SetNumber(index + 1);
            stamp.SetPosX(timeRate, width);
        }
    }

    private void AutoCreate()
    {
        bool isLeft = false;
        float clipLength = BGMManager.instance.Length;
        var beatList = AudioClipUtility.DetectNoteTimings(BGMManager.instance.CurrentClip);
        for (int i = 0; i < _scoreLineList.Count; i++)
        {
            _scoreLineList[i].ClearLine();
            float currentTime = i * _singleBeatTime + _currentStartTime;
            float nextTime = (i + 1) * _singleBeatTime + _currentStartTime;
            // 4の倍数で配置
            if (beatList.Count == 0)
            {
                break;
            }
            float beatTime = beatList.First();
            // 次の線がまだ次のビートより前なら、少なくともこの線はスキップ
            if (nextTime < beatTime)
            {
                continue;
            }
            // 次の線が次のビートを超えていても、次の線のほうが近ければスキップ
            if (Math.Abs(nextTime - beatTime) < Math.Abs(beatTime - currentTime))
            {
                continue;
            }
            if (Math.Abs(beatTime - currentTime) > 0.05f)
            {
                // ビートが近くなければそれを排除してスキップ
                beatList.RemoveAll(x => x <= currentTime);
                beatList.Remove(beatTime);
                continue;
            }
            CreateBall(i, isLeft, ScoreMakerBallType.Single);
            beatList.RemoveAll(x => x <= currentTime);
            // 左右交互に配置
            isLeft = !isLeft;
        }
    }

    private void ChangePause(bool isPause)
    {
        const float _roundZeroLineNumber = 0.001f;
        float lineNumber = GetCurrentLineNumber();
        // 一番下にスクロールしていても、若干_scoreAreaRectがずれていることがあるので補正する
        if (lineNumber < _roundZeroLineNumber)
        {
            lineNumber = 0;
        }
        _isPause = isPause;
        if (_isPause)
        {
            BGMManager.instance.Pause();
        }
        else
        {
            float currentTime = lineNumber * _singleBeatTime + _currentStartTime;
            BGMManager.instance.SetTime(currentTime);
            // BGMの現在時刻より前のラインは全て終わった判定にする
            foreach (var line in _scoreLineList)
            {
                line.IsEnd = line.GetLineTime(_currentBpm, _currentStartTime) < currentTime;
            }
            BGMManager.instance.Restart();
        }
    }

    public void CreateLine(List<NoteMaster> notes)
    {
        _selectMask.transform.SetAsLastSibling();
        if (notes == null)
        {
            return;
        }
        // ボール作成
        for (int i = 0; i < notes.Count; i++)
        {
            var targetNote = notes[i];
            bool isSingle = targetNote.type == 1;
            CreateBall(targetNote.num, targetNote.block < 3, isSingle ? ScoreMakerBallType.Single : ScoreMakerBallType.Long);
            if (!isSingle && targetNote.notes.Count > 0)
            {
                var endNoteMaster = targetNote.notes[0];
                CreateBall(endNoteMaster.num, endNoteMaster.block < 3, endNoteMaster.type == 1 ? ScoreMakerBallType.Single : ScoreMakerBallType.Long);
            }
        }
    }

    public void AdjustmentLineNumber(int lineNumber)
    {
        // ラインの数が多すぎる場合は削除
        if (_scoreLineList.Count > lineNumber)
        {
            for (int i = _scoreLineList.Count - 1; i >= lineNumber; i--)
            {
                var destroyLine = _scoreLineList[i];
                _scoreLineList.Remove(destroyLine);
                Destroy(destroyLine.gameObject);
            }
        }
        // ラインの数が少なすぎる場合は追加
        else if (_scoreLineList.Count < lineNumber)
        {
            for (int i = _scoreLineList.Count; i < lineNumber; i++)
            {
                var scoreLine = Instantiate(_scoreLinePrefab, _scoreLineTransform);
                scoreLine.transform.SetSiblingIndex(0);
                scoreLine.Init(i);
                _scoreLineList.Add(scoreLine);
                scoreLine.LeftPocket.OnClickDeleteButton.Subscribe(_ =>
                {
                    if (_selectedBallPocket == scoreLine.LeftPocket)
                    {
                        _selectedBallPocket = null;
                    }
                }).AddTo(this);
                scoreLine.RightPocket.OnClickDeleteButton.Subscribe(_ =>
                {
                    if (_selectedBallPocket == scoreLine.RightPocket)
                    {
                        _selectedBallPocket = null;
                    }
                }).AddTo(this);

                // ラインをチュートリアル用に登録
                if (_isTutorial)
                {
                    scoreLine.RegisterForTutorial(i);
                }
            }
        }
        SetFirstBeatNumberText();
    }

    private void SetFirstBeatNumberText()
    {
        int firstBeatNumber = 0;
        int currentBeat = 1;
        int beatNumber = _beatsNumber;
        int beatModulation = 0;
        for (int i = 0; i < _scoreLineList.Count; i++)
        {
            // if (CurrentMusicParameter.ModulationList.Contains(i))
            // {
            //     startNumber = i;
            //     _scoreLineList[i].SetFirstBeatText(firstBeatNumber);
            //     firstBeatNumber++;
            //     continue;
            // }
            // 最初のラインは必ず付ける
            if (i == 0 || currentBeat == beatNumber + beatModulation)
            {
                _scoreLineList[i].SetFirstBeatText(firstBeatNumber);
                firstBeatNumber++;
                beatModulation = _modulationDict.GetValueOrDefault(firstBeatNumber);
                currentBeat = 1;
                continue;
            }
            _scoreLineList[i].HideFirstBeatText();
            currentBeat++;
        }
    }

    private ScoreMakerBall CreateBall(int lineNumber, bool isLeft, ScoreMakerBallType ballType)
    {
        if (lineNumber < 0 || lineNumber >= _scoreLineList.Count)
        {
            return null;
        }
        _isEdited = true;
        var createdBall = _scoreLineList[lineNumber].CreateBall(ballType, isLeft);
        ConnectLongBall(lineNumber, isLeft, createdBall);
        return createdBall;
    }

    // ロングボールを線で繋げる
    private void ConnectLongBall(int lineNumber, bool isLeft, ScoreMakerBall createdBall)
    {
        if (createdBall == null || createdBall.BallType != ScoreMakerBallType.Long)
        {
            return;
        }
        // まず手前のボールを探す
        var frontLineList = _scoreLineList.GetRange(0, lineNumber);
        frontLineList.Reverse();
        var pairBall = SearchLonelyLongBall(frontLineList, isLeft);
        // なければ先のボールを探す
        if (pairBall == null)
        {
            var backLineList = _scoreLineList.GetRange(lineNumber + 1, _scoreLineList.Count - lineNumber - 1);
            pairBall = SearchLonelyLongBall(backLineList, isLeft);
        }
        if (pairBall != null)
        {
            ConnectBall(createdBall, pairBall);
        }
    }

    // 2つのボールを線で繋げる
    private void ConnectBall(ScoreMakerBall ball1, ScoreMakerBall ball2)
    {
        var linePrefab = ResourceManager.LoadPrefab<ScoreMakerBallLine>("ScoreMaker/ScoreMakerBallLine");
        var longBallLine = Instantiate(linePrefab);
        var firstBall = ball1.LineNumber <= ball2.LineNumber ? ball1 : ball2;
        var endBall = ball1.LineNumber > ball2.LineNumber ? ball1 : ball2;
        longBallLine.Init(firstBall, endBall, _scoreAreaLayoutGroup.spacing);
        _longBallLineList.Add(longBallLine);
        longBallLine.OnWhenDestroyed.Subscribe(line =>
        {
            _longBallLineList.Remove(line);
        }).AddTo(this);
    }

    // ラインのリストから最初の線でつながれていないロングボールを探す
    private ScoreMakerBall SearchLonelyLongBall(List<ScoreLine> scoreLineList, bool isLeft)
    {
        foreach (var line in scoreLineList)
        {
            var ball = line.GetBall(isLeft);
            if (ball != null)
            {
                if (ball.BallType == ScoreMakerBallType.Long && !ball.AttachedLine)
                {
                    return ball;
                }
                else
                {
                    break;
                }
            }
        }
        return null;
    }

    public void StartMake()
    {
        _isStartMake = true;
    }

    private List<LineState> CreateCurrentLineState()
    {
        var lineStateList = new List<LineState>();
        foreach (var line in _selectedLineList)
        {
            var leftBall = line.GetBall(isLeft: true);
            var rightBall = line.GetBall(isLeft: false);
            var state = new LineState()
            {
                LeftBallType = leftBall?.BallType ?? ScoreMakerBallType.None,
                RightBallType = rightBall?.BallType ?? ScoreMakerBallType.None,
            };
            lineStateList.Add(state);
        }
        return lineStateList;
    }

    private float GetCurrentLineNumber()
    {
        if (_isPause)
        {
            return (_scoreAreaBottom - _scoreAreaRect.anchoredPosition.y) / (_scoreAreaLayoutGroup.spacing + _scoreLineHeight);
        }
        return (_currentTime - _currentStartTime) / _singleBeatTime;
    }

    private void SetPositionByLineNumber(float lineNumber)
    {
        var positionY = _scoreAreaBottom - lineNumber * (_scoreAreaLayoutGroup.spacing + _scoreLineHeight);
        _scoreAreaRect.SetAnchoredPositionY(positionY);
    }

    public List<NoteMaster> CreateNoteList()
    {
        var noteList = new List<NoteMaster>();
        foreach (var line in _scoreLineList)
        {
            var leftMaster = line.GetMaster(isLeft: true);
            if (leftMaster != null)
            {
                noteList.Add(leftMaster);
            }
            var rightMaster = line.GetMaster(isLeft: false);
            if (rightMaster != null)
            {
                noteList.Add(rightMaster);
            }
        }
        return noteList;
    }

    public Dictionary<int, float> GetTimeJumpDict()
    {
        return _controlPanel.GetTimeJumpDict();
    }

    private void SelectLine(int number)
    {
        var line = _scoreLineList[number];
        if (_selectedLineList.Count == 0)
        {
            _selectedLineList.Add(line);
        }
        else
        {
            if (_selectedLineList.Contains(line))
            {
                // 1行だけなら選択解除
                if (_selectedLineList.Count == 1)
                {
                    _selectMask.SetActive(false);
                    _selectedLineList.Clear();
                }
                // 2行以上は反対側の1行だけ残す
                else if (_selectedLineList.IndexOf(line) == 0)
                {
                    RemoveUntilSingleLine(isLastRemove: false);
                }
                else if (_selectedLineList.IndexOf(line) == _selectedLineList.Count - 1)
                {
                    RemoveUntilSingleLine(isLastRemove: true);
                }
            }
            else
            {
                var oppositLine = GetOppositeLine(line);

                // 選択した行を全て選択リストに追加
                int start = Mathf.Min(line.LineNumber, oppositLine.LineNumber);
                int end = Mathf.Max(line.LineNumber, oppositLine.LineNumber);
                SetSelectedLine(start, end);
            }
        }
        SetSelectMask();
    }

    private ScoreLine GetOppositeLine(ScoreLine line)
    {
        if (_selectedLineList[0].LineNumber > line.LineNumber)
        {
            return _selectedLineList.LastOrDefault();
        }
        else if (_selectedLineList.Last().LineNumber < line.LineNumber)
        {
            return _selectedLineList.FirstOrDefault();
        }
        return null;
    }

    private void SetSelectedLine(int startLine, int endLine)
    {
        _selectedLineList.Clear();
        for (int i = startLine; i <= endLine; i++)
        {
            _selectedLineList.Add(_scoreLineList[i]);
        }
    }

    private void SetSelectMask()
    {
        if (_selectedLineList.Count == 0)
        {
            _selectMask.SetActive(false);
            return;
        }
        _selectMask.SetActive(true);
        var firstLineNumber = _selectedLineList.Min(l => l.LineNumber);
        var lastLineNumber = _selectedLineList.Max(l => l.LineNumber);
        float lineSpace = _scoreAreaLayoutGroup.spacing + _scoreLineHeight;
        _selectMask.transform.SetOffsetMinY(_scoreAreaLayoutGroup.padding.bottom - _selectMaskOffset + lineSpace * firstLineNumber);
        _selectMask.transform.SetOffsetMaxY(_scoreAreaLayoutGroup.padding.top - _selectMaskOffset + lineSpace * (_scoreLineList.Count - lastLineNumber - 1));
    }

    private void RemoveUntilSingleLine(bool isLastRemove)
    {
        while (_selectedLineList.Count > 1)
        {
            _selectedLineList.RemoveAt(isLastRemove ? _selectedLineList.Count - 1 : 0);
        }
        SetSelectMask();
    }

    // 選択中の列をコピーする
    private void CopyLine()
    {
        _copiedLineState.Clear();
        _copiedLineState = CreateCurrentLineState();
    }

    // コピーされた列情報を指定位置に貼り付ける
    private void PasteLine(int startLineNumber)
    {
        TutorialManager.Instance.AdvanceStep("Paste");
        // ペースト前に状態を保存しておく
        UpdatePastScoreLineList();
        CreateBallFromLineState(_copiedLineState, startLineNumber);
        _controlPanel.FinishPaste();
    }

    private void UpdatePastScoreLineList()
    {
        // _pastScoreLineListが増えすぎていたら減らす
        while (_pastScoreLineList.Count >= _maxLastPastLineCount)
        {
            _pastScoreLineList.RemoveAt(0);
        }
        _pastScoreLineList.Add(GetCurrentLineList());
    }

    private List<LineState> GetCurrentLineList()
    {
        var lastScoreLineList = new List<LineState>();
        foreach (var line in _scoreLineList)
        {
            var leftBall = line.GetBall(isLeft: true);
            var rightBall = line.GetBall(isLeft: false);
            var state = new LineState()
            {
                LeftBallType = leftBall?.BallType ?? ScoreMakerBallType.None,
                RightBallType = rightBall?.BallType ?? ScoreMakerBallType.None,
            };
            lastScoreLineList.Add(state);
        }
        return lastScoreLineList;
    }

    private void Undo()
    {
        var lastScoreLineList = _pastScoreLineList.Pop();
        if (lastScoreLineList == null)
        {
            return;
        }
        CreateBallFromLineState(lastScoreLineList, startNumber: 0);
    }

    // LineStateからボールを作成する
    private void CreateBallFromLineState(List<LineState> lineStateList, int startNumber)
    {
        for (int i = 0; i < lineStateList.Count; i++)
        {
            int lineNumber = startNumber + i;
            var targetLine = _scoreLineList[lineNumber];
            var state = lineStateList[i];
            // 先にボールを消しておく
            targetLine.ClearLine();
            CreateBall(lineNumber, isLeft: true, state.LeftBallType);
            CreateBall(lineNumber, isLeft: false, state.RightBallType);
        }
    }

    // 選択中の列を反転させる
    private void InversionSelectedLine()
    {
        UpdatePastScoreLineList();
        foreach (var line in _selectedLineList)
        {
            InversionLine(line);
        }
    }

    private void InversionLine(ScoreLine line)
    {
        var leftBall = line.GetBall(isLeft: true);
        var rightBall = line.GetBall(isLeft: false);
        line.ClearLine();
        CreateBall(line.LineNumber, isLeft: false, leftBall?.BallType ?? ScoreMakerBallType.None);
        CreateBall(line.LineNumber, isLeft: true, rightBall?.BallType ?? ScoreMakerBallType.None);
    }

    // 選択中の列を上下に移動させる
    private void MoveSelectedLine(bool isUp)
    {
        UpdatePastScoreLineList();
        if (isUp)
        {
            for (int i = _selectedLineList.Count - 1; i >= 0; i--)
            {
                MoveLine(_selectedLineList[i], isUp);
            }
            // 選択中の列の番号を更新
            SetSelectedLine(_selectedLineList.Min(l => l.LineNumber) + 1, _selectedLineList.Max(l => l.LineNumber) + 1);
            SetSelectMask();
        }
        else
        {
            for (int i = 0; i < _selectedLineList.Count; i++)
            {
                MoveLine(_selectedLineList[i], isUp);
            }
            // 選択中の列の番号を更新
            SetSelectedLine(_selectedLineList.Min(l => l.LineNumber) - 1, _selectedLineList.Max(l => l.LineNumber) - 1);
            SetSelectMask();
        }
    }

    private void MoveLine(ScoreLine line, bool isUp)
    {
        var leftBall = line.GetBall(isLeft: true);
        var rightBall = line.GetBall(isLeft: false);
        line.ClearLine();
        int targetLineNumber = isUp ? line.LineNumber + 1 : line.LineNumber - 1;
        CreateBall(targetLineNumber, isLeft: true, leftBall?.BallType ?? ScoreMakerBallType.None);
        CreateBall(targetLineNumber, isLeft: false, rightBall?.BallType ?? ScoreMakerBallType.None);
    }

    // 選択中の列のボールを削除する
    private void ClearLine()
    {
        foreach (var line in _selectedLineList)
        {
            line.ClearLine();
        }
    }

    public void PrepareTutorial(ScoreMakerStartParam startParam)
    {
        // タイムジャンプのないチュートリアル中はスクロール禁止
        if (startParam.TimeJumpList.Count == 0)
        {
            _scoreScrollRect.enabled = false;
        }
        SetScroll(startParam.ScrollPositionY);
        foreach (var arrangement in startParam.BallArrangementList)
        {
            CreateBall(arrangement.Number, isLeft: arrangement.IsLeft, ScoreMakerBallType.Single);
        }
        foreach (var timeJump in startParam.TimeJumpList)
        {
            _controlPanel.SetJumpTimeRate(timeJump.Index, timeJump.TimeRate);
            CreateTimeJumpStamp(timeJump.Index, timeJump.TimeRate);
        }
    }

    public void SetScroll(float posY)
    {
        _scoreAreaRect.anchoredPosition = new Vector2(0, posY);
    }

    void Update()
    {
        if (!_isStartMake)
        {
            return;
        }
        if (!_isPause)
        {
            if (BGMManager.instance.IsFinishBgm || BGMManager.instance.CurrentTime >= _currentEndTime)
            {
                // BGMが終わったら自動で止める
                ChangePause(true);
                return;
            }
            // 終了1秒前になったらフェードアウト開始
            else if (BGMManager.instance.CurrentTime >= _currentEndTime - 1f)
            {
                BGMManager.instance.FadeOut(1f).Forget();
            }
            float currentLineNumber = GetCurrentLineNumber();
            float anchorY = _scoreAreaBottom - (_scoreAreaLayoutGroup.spacing + _scoreLineHeight) * currentLineNumber;
            _scoreAreaRect.anchoredPosition = new Vector2(0, anchorY);
            var nextLine = _scoreLineList.FirstOrDefault(l => !l.IsEnd);
            if (nextLine != null && _currentTime > nextLine.GetLineTime(_currentBpm, _currentStartTime) - MasterManager.SettingMaster.ScoreMakerNoteTimeBuffer)
            {
                if (!_isPlayMakeMode)
                {
                    nextLine.Beat();
                }
                nextLine.IsEnd = true;
            }
        }

        // EditモードのUI制御
        bool isSelectedLine = _selectedLineList.Count > 0;
        bool isCopiedLine = _copiedLineState.Count > 0;
        bool isExsistPastLine = _pastScoreLineList.Count > 0;
        _controlPanel.SetButtonState(isSelectedLine, isCopiedLine, isExsistPastLine);
    }
}
