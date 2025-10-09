using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;
using System.ComponentModel;

public class MusicParameter
{
    public float Bpm;
    public float StartTime;
    public float EndTime;
    public int BeatsNumber;
    public List<int> ModulationList = new(); // 変調する位置の指定パラメータ
}

public class ScoreMakerView : MonoBehaviour
{
    [SerializeField] private Transform _scoreLineTransform;
    [SerializeField] private ScoreLine _scoreLinePrefab; 
    [SerializeField] private ScrollRect _scoreScrollRect;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _helpButton;
    [SerializeField] private Button _undoButton;
    [SerializeField] private Button _backButton;

    [SerializeField] private InputField _startTimeInput;
    [SerializeField] private InputField _endTimeInput;

    [SerializeField] private RectTransform _scoreAreaRect;
    [SerializeField] private VerticalLayoutGroup _scoreAreaLayoutGroup;
    [SerializeField] private Scrollbar _bgmScrollBar;

    [SerializeField] private MoveButton _moveButtonPrefab;
    [SerializeField] private Transform _moveButtonArea;
    [SerializeField] private Button _playBgmButton; // BGM再生ボタン
    [SerializeField] private Button _startPlayMakeButton; // 演奏作成ボタン
    [SerializeField] private ValueAdjuster _pitchAdjuster; // ピッチ変更
    [SerializeField] private Button _practiceButton; // 練習ボタン
    private float _basePitch;

    // コントロールパネル
    [SerializeField] private ScoreMakerControlPanel _controlPanel;
    [SerializeField] private ControlPanelPageBall _pageBall;

    // コピペ関連
    [SerializeField] private GameObject _selectMask;
    private List<ScoreLine> _selectedLineList = new();

    // 演奏作成モード切替関連
    [SerializeField] private GameObject _controlUiRoot;
    [SerializeField] private GameObject _playMakeModeUiRoot;
    [SerializeField] private Button _finishPlayMakeModeButton;
    [SerializeField] private Button _resetPlayMakeModeButton;
    private bool _isPlayMakeMode;
    // 演奏作成モードでの開始位置
    private float _startPlayMakeLineNumber;
    private List<LineState> _startPlayMakeLineList;

    public class LineState
    {
        public ScoreMakerBallType LeftBallType;
        public ScoreMakerBallType RightBallType;
    }
    private List<LineState> _copiedLineState = new();

    private List<ScoreLine> _scoreLineList = new();
    private List<List<LineState>> _pastScoreLineList = new(); // 過去のスコアラインの状態を保持したのリスト（Undo用）
    private List<ScoreMakerBallLine> _longBallLineList = new(); // 作ったロングボール間の線のリスト
    private List<MoveButton> _moveButtonList = new(); // 特定のラインに飛ぶボタン
    private MoveButton _goLastButton; // 最後のボール位置に飛ぶボタン

    public MusicParameter CurrentMusicParameter;
    public float CurrentBpm => CurrentMusicParameter.Bpm;
    public float CurrentStartTime => CurrentMusicParameter.StartTime;
    public float CurrentEndTime => CurrentMusicParameter.EndTime;

    // 編集された状態がセーブされていないかどうか
    private bool _isEdited;
    private bool _isPause;

    private Subject<float> _clickPracticeButton = new();
    public Observable<float> ClickPracticeButton => _clickPracticeButton;
    private Subject<(string, MusicParameter)> _onSave = new();
    public Observable<(string, MusicParameter)> OnSave => _onSave;
    private Subject<Unit> _clickSaveButton = new();
    public Observable<Unit> ClickSaveButton => _clickSaveButton;
    private Subject<float> _clickDuplicateButton = new();
    public Observable<float> ClickDuplicateButton => _clickDuplicateButton;

    private const float _scoreAreaBottom = -1660f;
    private const float _scoreLineHeight = 15f;
    private const float _selectMaskOffset = 50f;
    private const float _maxLineSpacing = 300f; // ライン間隔最大値
    private const int _maxLastPastLineCount = 20;

    private int _lineNumber => (int)(CurrentBpm * 4 * ((CurrentEndTime - CurrentStartTime) / 60f));
    private float _currentTime => BGMManager.instance.CurrentTime;

    public enum ScoreMakerBallType
    {
        None,
        Single,
        Long
    }
    private OperationType _currentOperationType;
    private float _singleBeatTime => 60f / (CurrentBpm * 4);
    private bool _isStartMake;
    private bool _isDuringPractice;

    private Dictionary<bool, int> _lastBeatLineDict = new();

    public void Init(StageHeader stageHeader, List<NoteMaster> notes)
    {
        CurrentMusicParameter = new MusicParameter
        {
            Bpm = stageHeader.BPM,
            StartTime = stageHeader.StartTime,
            EndTime = stageHeader.EndTime,
            BeatsNumber = stageHeader.BeatsNumber,
            ModulationList = stageHeader.ModulationList.ToList()
        };
        BGMManager.instance.SetTime(CurrentStartTime);

        _currentOperationType = OperationType.Rotation;
        StartSubscribeMain();
        StartSubscribeControllPanel();
        _startTimeInput.text = CurrentStartTime.ToString();
        _endTimeInput.text = CurrentEndTime.ToString();
        CreateLine(notes);
        _scoreScrollRect.verticalNormalizedPosition = 0;
        _isEdited = false;
    }

    private void StartSubscribeMain()
    {
        // 基本操作系
        GameManager.instance.ClickHandler.OnClickScoreLinePocket.Subscribe(x =>
        {
            ClickLine(x.number, x.isLeft);
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickScoreLine.Subscribe(number =>
        {
            // ライン選択時はSelectLineとInversionだけ
            if (!(_currentOperationType is OperationType.SelectLine or OperationType.Inversion or OperationType.Up or OperationType.Down))
            {
                return;
            }
            ClickLine(number, isLeft: false);
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickScoreLineNumber.Subscribe(number =>
        {
            SEManager.instance.PlaySe(SeName.Button4);
            CreateMoveButton(number);
        }).AddTo(this);
        // TODO: ピンチイン・アウトでライン間隔を調整する機能を実装する
        // GameManager.instance.ClickHandler.OnPinchIn.Subscribe(value =>
        // {
        //     _scoreAreaLayoutGroup.spacing += value;
        // }).AddTo(this);
        // GameManager.instance.ClickHandler.OnPinchOut.Subscribe(value =>
        // {
        //     _scoreAreaLayoutGroup.spacing -= value;
        // }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickButton.Subscribe(isLeft =>
        {
            if (!_isPlayMakeMode)
            {
                return;
            }
            int currentLine = Mathf.RoundToInt(GetCurrentLineNumber());
            _lastBeatLineDict[isLeft] = currentLine;
            CreateBall(currentLine, isLeft, ScoreMakerBallType.Single);
            SEManager.instance.PlaySe(SeName.Beat);
        }).AddTo(this);

        // 演奏作成モードのロングはいったん廃止
        // GameManager.instance.ClickHandler.OnReleaseButton.Subscribe(isLeft =>
        // {
        //     if (!_isPlayMakeMode)
        //     {
        //         return;
        //     }
        //     if (!_lastBeatLineDict.TryGetValue(isLeft, out int lastLineNumber))
        //     {
        //         return;
        //     }
        //     _lastBeatLineDict.Remove(isLeft);
        //     // 直前に叩いたラインが2つ以上前の場合のみロングボールに変える
        //     int currentLine = Mathf.RoundToInt(GetCurrentLineNumber());
        //     if (lastLineNumber > currentLine - 2)
        //     {
        //         return;
        //     }
        //     ChangeBallType(lastLineNumber, isLeft, ScoreMakerBallType.Long);
        //     CreateBall(currentLine, isLeft, ScoreMakerBallType.Long);
        //     SEManager.instance.PlaySe(SeName.Beat);
        // }).AddTo(this);

        _playBgmButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePause();
        }).AddTo(this);
        _saveButton.OnClickAsObservable().Subscribe(_ =>
        {
            _clickSaveButton.OnNext(default);
        }).AddTo(this);
        _helpButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.OpenHelpDialog(HelpDialogPageType.ScoreMaker);
        }).AddTo(this);
        _undoButton.OnClickAsObservable().Subscribe(_ =>
        {
            Undo();
        }).AddTo(this);
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            _isPause = false;
            BGMManager.instance.Pause();
            if (_isEdited)
            {
                // 編集が保存されていなかったら確認ダイアログを出す
                var option = new MessageDialogOption
                {
                    TitleText = "ホームに戻る",
                    MessageText = "変更が保存されていませんが、このままホームに戻りますか？",
                    OkButtonText = "戻る",
                };
                var dialog = DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
                dialog.OnCloseDialog.Subscribe(result =>
                {
                    if (result.ResultType == DialogResultType.Ok)
                    {
                        GameManager.instance.OpenScene(SceneType.Home, new HomeSceneInfo()).Forget();
                    }
                });
                return;
            }
            GameManager.instance.OpenScene(SceneType.Home, new HomeSceneInfo()).Forget();
        }).AddTo(this);
        // 演奏作成
        _startPlayMakeButton.OnClickAsObservable().Subscribe(_ =>
        {
            RunControlPanelRequest(new ControlPanelRequestPlayMake());
        }).AddTo(this);
        _practiceButton.OnClickAsObservable().Subscribe(_ =>
        {
            RunControlPanelRequest(new ControlPanelRequestPractice());
        }).AddTo(this);
        _finishPlayMakeModeButton.OnClickAsObservable().Subscribe(_ =>
        {
            UpdatePastScoreLineList();
            _playMakeModeUiRoot.SetActive(false);
            _controlUiRoot.SetActive(true);
            _isPlayMakeMode = false;
        }).AddTo(this);
        _resetPlayMakeModeButton.OnClickAsObservable().Subscribe(_ =>
        {
            BGMManager.instance.Pause();
            _isPause = true;
            CreateBallFromLineState(_startPlayMakeLineList, startNumber: 0);
            SetPositionByLineNumber(_startPlayMakeLineNumber);
        }).AddTo(this);

        
        _basePitch = BGMManager.instance.CurrentPitch;
        _pitchAdjuster.Init(1f, 0.1f);
        _pitchAdjuster.CurrentValue.Subscribe(value =>
        {
            BGMManager.instance.SetPitch(_basePitch * value);
        }).AddTo(this);
    }

    private void ClickLine(int lineNumber, bool isLeft)
    {
        if (_currentOperationType != OperationType.SelectLine)
        {
            UpdatePastScoreLineList();
            SEManager.instance.PlaySe(SeName.Button2);
        }
        switch (_currentOperationType)
        {
            case OperationType.Single:
            case OperationType.Long:
                var ballType = _currentOperationType switch
                {
                    OperationType.Single => ScoreMakerBallType.Single,
                    OperationType.Long => ScoreMakerBallType.Long,
                    _ => ScoreMakerBallType.Single
                };
                CreateBall(lineNumber, isLeft, ballType);
                break;
            case OperationType.Rotation:
                RotationBall(lineNumber, isLeft);
                break;
            case OperationType.SelectLine:
                OnClickScoreLine(lineNumber);
                break;
            case OperationType.Inversion:
                InversionLine(_scoreLineList[lineNumber]);
                break;
            case OperationType.Up:
            case OperationType.Down:
                MoveLine(_scoreLineList[lineNumber], isUp: _currentOperationType == OperationType.Up);
                break;
        }
    }

#region ダイアログ系
    // セーブ確認のダイアログ表示
    public void DisplaySaveDialog(string stageName, bool isNewCreate)
    {
        var dialog = CreateSaveDialog(stageName, isNewCreate);
        dialog.OnCloseDialog.Subscribe(result  =>
        {
            string stageName = "";
            if (result is InputDialogResult inputResult)
            {
                stageName = inputResult.StageName;
            }
            if (result.ResultType == DialogResultType.Ok)
            {
                _onSave.OnNext((stageName, CurrentMusicParameter));
            }
        }).AddTo(this);
    }

    // セーブ確認のダイアログ作成
    public DialogBase CreateSaveDialog(string stageName, bool isNewCreate)
    {
        if (isNewCreate)
        {
            // 新規作成ならステージ名入力ダイアログを出す
            var option = new InputDialogOption
            {
                TitleText = "新規保存",
                OkButtonText = "決定",
                MessageText = "ステージ名を入力してください",
                PlaceHolderText = stageName,
                InitialInputText = stageName,
            };
            return DialogManager.instance.CreateDialog<InputDialog>(DialogManager.InputDialogPrefabName, option);
        }
        else
        {
            // 確認ダイアログを出す
            var option = new MessageDialogOption
            {
                TitleText = "保存",
                MessageText = "変更すると\nこのレベルのハイスコアは削除されます。\n変更を保存しますか？",
                OkButtonText = "保存する",
            };
            return DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
        }
    }

    // セーブ完了通知ダイアログ
    public void DisplaySaveFinishDialog()
    {
        var option = new MessageDialogOption
        {
            TitleText = "保存完了",
            OkButtonText = "OK",
            HideCancelButton = true,
            MessageText = "保存しました",
        };
        DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
        _isEdited = false;
    }

    // ステージ複製選択ダイアログ
    public void DisplayDuplicateDialog(SingleStageMaster stageMaster)
    {
        var option = new StageDuplicateDialogOption
        {
            TitleText = "ステージ複製",
            OkButtonText = "複製",
            StageInfo = stageMaster
        };
        var dialog = DialogManager.instance.CreateDialog<StageDuplicateDialog>(DialogManager.StageDuplicateDialogPrefabName, option);
        dialog.OnCloseDialog.Subscribe(result  =>
        {
            if (result.ResultType != DialogResultType.Ok) return;
            if (result is not StageDuplicateDialogResult duplicateResult) return;
            UpdatePastScoreLineList();
            CreateLine(duplicateResult.DuplicateNotes);
        }).AddTo(this);
    }
#endregion

    private void StartSubscribeControllPanel()
    {
        _controlPanel.OnRequest.Subscribe(request =>
        {
            RunControlPanelRequest(request);
        }).AddTo(this);

        _selectMask.SetActive(false);
        _controlPanel.Init(CurrentMusicParameter, BGMManager.instance.CurrentClip);

        _pageBall.Init();
        _pageBall.OnRequest.Subscribe(request =>
        {
            RunControlPanelRequest(request);
        }).AddTo(this);
        _pageBall.SetSelectBallType(_currentOperationType);
    }

    private void RunControlPanelRequest(ControlPanelRequestBase request)
    {
        switch (request)
        {
            case ControlPanelRequestSelectBall selectBallRequest:
                _currentOperationType = selectBallRequest.OperationType;
                break;
            case ControlPanelRequestPractice _:
                _isDuringPractice = true;
                _clickPracticeButton.OnNext(_bgmScrollBar.value);
                break;
            case ControlPanelRequestCopy _:
                CopyLine();
                break;
            case ControlPanelRequestSelectCancel _:
                _selectedLineList.Clear();
                _selectMask.SetActive(false);
                break;
            case ControlPanelRequestInversion _:
                InversionSelectedLine();
                break;
            case ControlPanelRequestDeleteRange _:
                ClearLine();
                break;
            case ControlPanelRequestStageDuplicate _:
                _clickDuplicateButton.OnNext(_bgmScrollBar.value);
                break;
            case ControlPanelRequestEvenlySpaced _:
                bool isLeft = false;
                for (int i = 0; i < _scoreLineList.Count; i++)
                {
                    _scoreLineList[i].ClearLine();
                    // 4の倍数で配置
                    if (i % 4 != 0)
                    {
                        continue;
                    }
                    CreateBall(i, isLeft, ScoreMakerBallType.Single);
                    // 左右交互に配置
                    isLeft = !isLeft;
                }
                break;
            case ControlPanelRequestAutoCreate _:
                AutoCreate();
                break;
            case ControlPanelRequestPlayMake _:
                _startPlayMakeLineNumber = GetCurrentLineNumber();
                _startPlayMakeLineList = GetCurrentLineList();
                _controlUiRoot.SetActive(false);
                _playMakeModeUiRoot.SetActive(true);
                _isPlayMakeMode = true;
                break;
            case ControlPanelRequestAllClear _:
                for (int i = 0; i < _scoreLineList.Count; i++)
                {
                    _scoreLineList[i].ClearLine();
                }
                break;
            case ControlPanelRequestChangeBpm changeBpm:
                CurrentMusicParameter.Bpm = changeBpm.Bpm;
                AdjustmentLineNumber();
                break;
            case ControlPanelRequestChangeStartTime changeStartTime:
                CurrentMusicParameter.StartTime = changeStartTime.StartTime;
                AdjustmentLineNumber();
                break;
            case ControlPanelRequestChangeEndTime changeEndTime:
                // 現在の曲の長さ以上にならないようにする
                var actualEndTime = Mathf.Min(changeEndTime.EndTime, BGMManager.instance.Length);
                CurrentMusicParameter.EndTime = actualEndTime;
                if (!Mathf.Approximately(actualEndTime, changeEndTime.EndTime))
                {
                    _controlPanel.SetMusicParameter(CurrentMusicParameter);
                }
                AdjustmentLineNumber();
                break;
            case ControlPanelRequestBeatsNumber beatsNumber:
                CurrentMusicParameter.BeatsNumber = beatsNumber.BeatsNumber;
                SetFirstBeatNumberText();
                break;
            case ControlPanelRequestModulation modulation:
                CurrentMusicParameter.ModulationList.Add(modulation.Measure * CurrentMusicParameter.BeatsNumber + modulation.Beat);
                SetFirstBeatNumberText();
                break;
            case ControlPanelRequestResetModulation _:
                CurrentMusicParameter.ModulationList = new List<int>();
                SetFirstBeatNumberText();
                break;
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
            float currentTime = i * _singleBeatTime + CurrentStartTime;
            float nextTime = (i + 1) * _singleBeatTime + CurrentStartTime;
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

    private void ChangePause()
    {
        float lineNumber = GetCurrentLineNumber();
        _isPause = !_isPause;
        if (_isPause)
        {
            BGMManager.instance.Pause();
        }
        else
        {
            float currentTime = lineNumber * _singleBeatTime + CurrentStartTime;
            BGMManager.instance.SetTime(currentTime);
            // BGMの現在時刻より前のラインは全て終わった判定にする
            foreach (var line in _scoreLineList)
            {
                line.IsEnd = line.GetLineTime(CurrentBpm, CurrentStartTime) < currentTime;
            }
            BGMManager.instance.Restart();
        }
    }

    // Editモードでのライン選択
    private void OnClickScoreLine(int number)
    {
        SEManager.instance.PlaySe(SeName.Button2);
        if (_controlPanel.IsWaitingPaste)
        {
            PasteLine(number);
        }
        else
        {
            SelectLine(number);
        }
    }

    public void CreateLine(List<NoteMaster> notes)
    {
        AdjustmentLineNumber();
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
            CreateBall(targetNote.noteNumber, targetNote.block < 3, isSingle ? ScoreMakerBallType.Single : ScoreMakerBallType.Long);
            if (!isSingle && targetNote.notes.Count > 0)
            {
                var endNoteMaster = targetNote.notes[0];
                CreateBall(endNoteMaster.noteNumber, endNoteMaster.block < 3, endNoteMaster.type == 1 ? ScoreMakerBallType.Single : ScoreMakerBallType.Long);
            }
        }
    }

    private void AdjustmentLineNumber()
    {
        // ラインの数が多すぎる場合は削除
        if (_scoreLineList.Count > _lineNumber)
        {
            for (int i = _scoreLineList.Count - 1; i >= _lineNumber; i--)
            {
                var destroyLine = _scoreLineList[i];
                _scoreLineList.Remove(destroyLine);
                Destroy(destroyLine.gameObject);
            }
        }
        // ラインの数が少なすぎる場合は追加
        else if (_scoreLineList.Count < _lineNumber)
        {
            for (int i = _scoreLineList.Count; i < _lineNumber; i++)
            {
                var scoreLine = Instantiate(_scoreLinePrefab, _scoreLineTransform);
                scoreLine.transform.SetSiblingIndex(0);
                scoreLine.Init(i);
                _scoreLineList.Add(scoreLine);
            }
        }
        SetFirstBeatNumberText();
    }

    private void SetFirstBeatNumberText()
    {
        int startNumber = 0;
        int firstBeatNumber = 0;
        for (int i = 0; i < _scoreLineList.Count; i++)
        {
            if (CurrentMusicParameter.ModulationList.Contains(i))
            {
                startNumber = i;
                _scoreLineList[i].SetFirstBeatText(firstBeatNumber);
                firstBeatNumber++;
                continue;
            }
            if (CurrentMusicParameter.BeatsNumber != 0 && (i - startNumber) % CurrentMusicParameter.BeatsNumber == 0)
            {
                _scoreLineList[i].SetFirstBeatText(firstBeatNumber);
                firstBeatNumber++;
                continue;
            }
            _scoreLineList[i].HideFirstBeatText();
        }
    }

    private void CreateBall(int lineNumber, bool isLeft, ScoreMakerBallType ballType)
    {
        _isEdited = true;
        var createdBall = _scoreLineList[lineNumber].CreateBall(ballType, isLeft);
        ConnectLongBall(lineNumber, isLeft, createdBall);
        SetGoLastButton();
    }

    private void RotationBall(int lineNumber, bool isLeft)
    {
        _isEdited = true;
        var createdBall = _scoreLineList[lineNumber].RotationBall(isLeft);
        ConnectLongBall(lineNumber, isLeft, createdBall);
        SetGoLastButton();
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
        bool isHead = false;
        var pairBall = SearchLonelyLongBall(frontLineList, isLeft);
        // なければ先のボールを探す
        if (pairBall == null)
        {
            isHead = true;
            var backLineList = _scoreLineList.GetRange(lineNumber + 1, _scoreLineList.Count - lineNumber - 1);
            pairBall = SearchLonelyLongBall(backLineList, isLeft);
        }
        if (pairBall != null)
        {
            var linePrefab = ResourceManager.LoadPrefab<ScoreMakerBallLine>("ScoreMaker/ScoreMakerBallLine");
            var longBallLine = Instantiate(linePrefab);
            longBallLine.Init(isHead ? createdBall : pairBall, isHead ? pairBall : createdBall, _scoreAreaLayoutGroup.spacing);
            _longBallLineList.Add(longBallLine);
            longBallLine.OnWhenDestroyed.Subscribe(line =>
            {
                _longBallLineList.Remove(line);
            }).AddTo(this);
        }
    }

    private void SetGoLastButton()
    {
        var lastLine = _scoreLineList.LastOrDefault(l => l.HasBall);
        if (lastLine == null)
        {
            return;
        }
        if (_goLastButton == null)
        {
            _goLastButton = Instantiate(_moveButtonPrefab, _moveButtonArea);
            SubscribeMoveButton(_goLastButton);
        }
        _goLastButton.SetLineNumber(lastLine.LineNumber, _scoreLineList.Count);
    }

    private void CreateMoveButton(int lineNumber)
    {
        var sameNuberButton = _moveButtonList.FirstOrDefault(b => b.TargetLineNumber == lineNumber);
        var targetLine = _scoreLineList.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (sameNuberButton != null)
        {
            _moveButtonList.Remove(sameNuberButton);
            Destroy(sameNuberButton.gameObject);
            targetLine.SetMoveButton(false);
        }
        else
        {
            var button = Instantiate(_moveButtonPrefab, _moveButtonArea);
            SubscribeMoveButton(button);
            _moveButtonList.Add(button);
            button.SetLineNumber(lineNumber, _scoreLineList.Count);
            targetLine.SetMoveButton(true);
        }
    }

    private void SubscribeMoveButton(MoveButton moveButton)
    {
        moveButton.OnClickAsObservable().Subscribe(_ =>
        {
            var posY = _scoreAreaBottom - moveButton.TargetLineNumber * (_scoreAreaLayoutGroup.spacing + _scoreLineHeight);
            _scoreAreaRect.anchoredPosition = new Vector2(0, posY);
        }).AddTo(moveButton);
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
        BGMManager.instance.Play();
    }

    // 練習モードから戻ってきたとき
    public void RestartMake()
    {
        _isDuringPractice = false;
        gameObject.SetActive(true);
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
        return (_currentTime - CurrentStartTime) / _singleBeatTime;
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
                _selectedLineList.Clear();
                int start = Mathf.Min(line.LineNumber, oppositLine.LineNumber);
                int end = Mathf.Max(line.LineNumber, oppositLine.LineNumber);
                for (int i = start; i <= end; i++)
                {
                    _selectedLineList.Add(_scoreLineList[i]);
                }
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

    void Update()
    {
        if (!_isStartMake || _isDuringPractice)
        {
            return;
        }
        if (!_isPause)
        {
            float currentLineNumber = GetCurrentLineNumber();
            float anchorY = _scoreAreaBottom - (_scoreAreaLayoutGroup.spacing + _scoreLineHeight) * currentLineNumber;
            _scoreAreaRect.anchoredPosition = new Vector2(0, anchorY);
            var nextLine = _scoreLineList.FirstOrDefault(l => !l.IsEnd);
            if (nextLine != null && _currentTime > nextLine.GetLineTime(CurrentBpm, CurrentStartTime) - MasterManager.SettingMaster.ScoreMakerNoteTimeBuffer)
            {
                if (!_isPlayMakeMode)
                {
                    nextLine.Beat();
                }
                nextLine.IsEnd = true;
            }
            if (_currentTime > CurrentEndTime)
            {
                // BGMが終わったら自動で止める
                _isPause = true;
                BGMManager.instance.FadeOut(duration: 1f).Forget();
            }
        }

        // EditモードのUI制御
        bool isSelectedLine = _selectedLineList.Count > 0;
        bool isCopiedLine = _copiedLineState.Count > 0;
        bool isExsistPastLine = _pastScoreLineList.Count > 0;
        _controlPanel.SetButtonState(isSelectedLine, isCopiedLine, isExsistPastLine);
        _undoButton.interactable = isExsistPastLine;
    }
}
