using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks;

public class ScoreMakerView : MonoBehaviour
{
    [SerializeField] private Transform _scoreLineTransform;
    [SerializeField] private ScoreLine _scoreLinePrefab; 
    [SerializeField] private ScrollRect _scoreScrollRect;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _helpButton;
    [SerializeField] private Button _undoButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private List<MenuButton> _levelButtonList = new();

    [SerializeField] private InputField _bpmInput;
    [SerializeField] private InputField _offsetInput;

    [SerializeField] private RectTransform _scoreAreaRect;
    [SerializeField] private VerticalLayoutGroup _scoreAreaLayoutGroup;
    [SerializeField] private Scrollbar _bgmScrollBar;

    [SerializeField] private MoveButton _moveButtonPrefab;
    [SerializeField] private Transform _moveButtonArea;
    [SerializeField] private Button _playBgmButton; // BGM再生ボタン

    // コントロールパネル
    [SerializeField] private ScoreMakerControlPanel _controlPanel;

    // コピペ関連
    [SerializeField] private GameObject _selectMask;
    private List<ScoreLine> _selectedLineList = new();
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

    public float CurrentBpm { get; private set; }
    public float CurrentOffset  { get; private set; }
    // 編集された状態がセーブされていないかどうか
    private bool _isEdited;
    private bool _isPause;

    private Subject<float> _clickPracticeButton = new();
    public Observable<float> ClickPracticeButton => _clickPracticeButton;
    private Subject<(List<NoteMaster> notes, int level)> _onChangeLevel = new();
    public Observable<(List<NoteMaster> notes, int level)> OnChangeLevel => _onChangeLevel;
    private Subject<string> _onSave = new();
    public Observable<string> OnSave => _onSave;
    private Subject<Unit> _clickSaveButton = new();
    public Observable<Unit> ClickSaveButton => _clickSaveButton;
    private Subject<float> _clickDuplicateButton = new();
    public Observable<float> ClickDuplicateButton => _clickDuplicateButton;

    private const float _scoreAreaBottom = -1500f;
    private const float _scoreLineHeight = 15f;
    private const float _selectMaskOffset = 50f;
    private const float _maxLineSpacing = 300f; // ライン間隔最大値
    private const int _maxLastPastLineCount = 20;

    private int _lineNumber => (int)(CurrentBpm * (BGMManager.instance.CurrentClipLength / 60f));

    public enum ScoreMakerBallType
    {
        None,
        Single,
        Long
    }
    private ScoreMakerBallType _currentSelectBallType;
    private float _singleBeatTime => 60f / CurrentBpm;
    private bool _isStartMake;
    private bool _isDuringPractice;

    public void Init(StageHeader stageHeader, List<NoteMaster> notes, int firstLevel)
    {
        CurrentBpm = stageHeader.BPM;
        CurrentOffset = stageHeader.NoteTimeOffset + SaveDataManager.SettingData.Offset;

        // レベルボタン初期化
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            int level = i + 1;
            var button = _levelButtonList[i];
            button.OnWhenClicked.Subscribe(_ =>
            {
                DarkeningLevelButton();
                button.SetLight(true);
                ChangeLevel(level);
            }).AddTo(this);
            if (level == firstLevel)
            {
                DarkeningLevelButton();
                button.SetLight(true);
            }
        }

        _currentSelectBallType = ScoreMakerBallType.Single;
        StartSubscribeMain();
        StartSubscribeControllPanel();
        _bpmInput.text = CurrentBpm.ToString();
        _offsetInput.text = CurrentOffset.ToString();
        CreateLine(notes);
        _scoreScrollRect.verticalNormalizedPosition = 0;
        _isEdited = false;
    }

    private void StartSubscribeMain()
    {
        GameManager.instance.ClickHandler.OnClickScoreLinePocket.Subscribe(x =>
        {
            if (_controlPanel.IsEditMode)
            {
                OnClickScoreLine(x.number);
            }
            else
            {
                UpdatePastScoreLineList();
                SEManager.instance.PlaySe(SeName.Button2);
                CreateBall(x.number, x.isLeft, _currentSelectBallType);
            }
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickScoreLine.Subscribe(number =>
        {
            if (_controlPanel.IsEditMode)
            {
                OnClickScoreLine(number);
            }
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickScoreLineNumber.Subscribe(number =>
        {
            SEManager.instance.PlaySe(SeName.Button4);
            CreateMoveButton(number);
        }).AddTo(this);
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
        _bpmInput.OnEndEditAsObservable().Subscribe(bpm =>
        {
            _isEdited = true;
            CurrentBpm = float.Parse(bpm);
        }).AddTo(this);
        _offsetInput.OnEndEditAsObservable().Subscribe(offset =>
        {
            _isEdited = true;
            CurrentOffset = float.Parse(offset);
        }).AddTo(this);
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
                _onSave.OnNext(stageName);
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
        _controlPanel.OnSelectBall.Subscribe(ballType =>
        {
            _currentSelectBallType = ballType;
        }).AddTo(this);
        _controlPanel.OnPractice.Subscribe(_ =>
        {
            _isDuringPractice = true;
            _clickPracticeButton.OnNext(_bgmScrollBar.value);
        }).AddTo(this);
        // コピー、ペーストボタン
        _selectMask.SetActive(false);
        _controlPanel.OnCopy.Subscribe(_ =>
        {
            CopyLine();
        }).AddTo(this);
        _controlPanel.OnSelectCancel.Subscribe(_ =>
        {
            _selectedLineList.Clear();
            _selectMask.SetActive(false);
        }).AddTo(this);
        _controlPanel.OnInversion.Subscribe(_ =>
        {
            InversionLine();
        }).AddTo(this);
        _controlPanel.OnDeleteRange.Subscribe(_ =>
        {
            ClearLine();
        }).AddTo(this);
        _controlPanel.OnStageDuplicate.Subscribe(_ =>
        {
            _clickDuplicateButton.OnNext(default);
        }).AddTo(this);
        _controlPanel.OnChangeLineSpacing.Subscribe(value =>
        {
            float spacing = value * _maxLineSpacing;
            _scoreAreaLayoutGroup.spacing = spacing;
            foreach (var line in _longBallLineList)
            {
                line.UpdateLineSpacing(spacing);
            }
        }).AddTo(this);

        _controlPanel.Init();
    }

    private void ChangePause()
    {
        _isPause = !_isPause;
        if (_isPause)
        {
            BGMManager.instance.Pause();
        }
        else
        {
            float posY = _scoreAreaRect.anchoredPosition.y;
            var lineNumber = (_scoreAreaBottom - posY) / (_scoreAreaLayoutGroup.spacing + _scoreLineHeight);
            float currentTime = lineNumber * _singleBeatTime + CurrentOffset;
            BGMManager.instance.SetTime(currentTime);
            // BGMの現在時刻より前のラインは全て終わった判定にする
            foreach (var line in _scoreLineList)
            {
                line.IsEnd = line.GetLineTime(CurrentBpm, CurrentOffset) < currentTime;
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
        // すでにラインが作られていたら全て削除
        while (_scoreLineList.Count > 0)
        {
            var destroyLine = _scoreLineList[0];
            _scoreLineList.Remove(destroyLine);
            Destroy(destroyLine.gameObject);
        }

        // ライン作成
        for (int i = 0; i < _lineNumber; i++)
        {
            var scoreLine = Instantiate(_scoreLinePrefab, _scoreLineTransform);
            scoreLine.transform.SetSiblingIndex(0);
            scoreLine.Init(i);
            _scoreLineList.Add(scoreLine);
        }
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

    private void CreateBall(int lineNumber, bool isLeft, ScoreMakerBallType ballType)
    {
        _isEdited = true;
        var targetLine = _scoreLineList[lineNumber];
        var createdBall = targetLine.CreateBall(ballType, isLeft);
        // ロングボールなら、線で繋げられないか検索する
        if (ballType == ScoreMakerBallType.Long && createdBall != null)
        {
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
        SetGoLastButton();
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

    private void ChangeLevel(int level)
    {
        // レベル更新
        BGMManager.instance.Pause();
        _onChangeLevel.OnNext((CreateNoteList(), level));
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
        return (BGMManager.instance.CurrentTime - CurrentOffset) / _singleBeatTime;
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

    private void DarkeningLevelButton()
    {
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            _levelButtonList[i].SetLight(false);
        }
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
        _pastScoreLineList.Add(lastScoreLineList);
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
    private void InversionLine()
    {
        UpdatePastScoreLineList();
        foreach (var line in _selectedLineList)
        {
            var leftBall = line.GetBall(isLeft: true);
            var rightBall = line.GetBall(isLeft: false);
            line.ClearLine();
            CreateBall(line.LineNumber, isLeft: false, leftBall?.BallType ?? ScoreMakerBallType.None);
            CreateBall(line.LineNumber, isLeft: true, rightBall?.BallType ?? ScoreMakerBallType.None);
        }
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
        if (BGMManager.instance.IsPlaying)
        {
            float currentLineNumber = GetCurrentLineNumber();
            float anchorY = _scoreAreaBottom - (_scoreAreaLayoutGroup.spacing + _scoreLineHeight) * currentLineNumber;
            _scoreAreaRect.anchoredPosition = new Vector2(0, anchorY);
            var nextLine = _scoreLineList.FirstOrDefault(l => !l.IsEnd);
            if (nextLine != null && BGMManager.instance.CurrentTime > nextLine.GetLineTime(CurrentBpm, CurrentOffset) - MasterManager.SettingMaster.ScoreMakerNoteTimeBuffer)
            {
                nextLine.Beat();
                nextLine.IsEnd = true;
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
