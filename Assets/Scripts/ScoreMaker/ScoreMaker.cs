using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;
using Unity.VisualScripting;
using Cysharp.Threading.Tasks;

public class ScoreMaker : MonoBehaviour
{
    [SerializeField] private Transform _scoreLineTransform;
    [SerializeField] private GameObject _singleBallSelectedPanel;
    [SerializeField] private GameObject _longBallSelectedPanel;
    [SerializeField] private ScoreLine _scoreLinePrefab; 
    [SerializeField] private ScrollRect _scoreScrollRect;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _practiceButton;
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

    // コントロールパネル関連
    [SerializeField] private GameObject _controllPanelPage1;
    [SerializeField] private GameObject _controllPanelPage2;
    [SerializeField] private GameObject _controllPanelPage3;
    // 1ページ目
    [SerializeField] private Button _selectSingleBallButton; // シングルボール選択ボタン
    [SerializeField] private Button _selectLongBallButton; // ロングボール選択ボタン
    // 2ページ目
    [SerializeField] private Button _copyButton; // コピーボタン
    [SerializeField] private Button _pasteButton; // ペーストボタン
    [SerializeField] private Button _selectCancelButton; // 選択解除ボタン
    [SerializeField] private Button _inversionButton; // 左右反転ボタン
    [SerializeField] private Button _undoButton; // 一手戻すボタン
    // 3ページ目
    [SerializeField] private Slider _lineSpacingSlider; // ライン間隔調整スライダー
    [SerializeField] private Slider _ballSizeSlider; // ボールサイズ調整スライダー
    // 全体
    [SerializeField] private List<Button> _nextPageButtonList; // 次ページボタン
    [SerializeField] private List<Button> _backPageButtonList; // 前ページボタン
    [SerializeField] private GameObject _messageMask;
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _pasteCancelButton; // ペーストキャンセルボタン

    // コピペ関連
    [SerializeField] private GameObject _selectMask;
    private bool _isEditMode;
    private bool _isSelectingPaste;
    private List<ScoreLine> _selectedLineList = new();
    public class LineState
    {
        public ScoreMakerBallType LeftBallType;
        public ScoreMakerBallType RightBallType;
    }
    private List<LineState> _copiedLineState = new();

    private List<LevelInfo> _levelInfoList = new();
    private LevelInfo _currentLevelInfo => _levelInfoList?.FirstOrDefault(l => l.Level == _currentLevel);

    private StageInfo _currentStageInfo;
    private StageMaster _currentStageMaster;

    private List<ScoreLine> _scoreLineList = new();
    private List<List<LineState>> _pastScoreLineList = new(); // 過去のスコアラインの状態を保持したのリスト（Undo用）
    private List<ScoreMakerBallLine> _longBallLineList = new(); // 作ったロングボール間の線のリスト
    private List<MoveButton> _moveButtonList = new(); // 特定のラインに飛ぶボタン
    private MoveButton _goLastButton; // 最後のボール位置に飛ぶボタン

    private const float _scoreAreaBottom = -1500f;
    private const float _scoreLineHeight = 15f;
    private const float _selectMaskWidth = 630f;
    private const float _selectMaskFirstHeight = 100f;
    private const float _maxLineSpacing = 300f; // ライン間隔最大値

    private int _lineNumber => (int)(_bpm * (BGMManager.instance.CurrentClipLength / 60f));

    public enum ScoreMakerBallType
    {
        None,
        Single,
        Long
    }
    private ScoreMakerBallType _currentSelectBallType;
    private float _bpm => _currentStageInfo?.StageHeader.BPM ?? 520f;
    private float _offset => _currentStageInfo?.StageHeader.NoteTimeOffset + SaveDataManager.SettingData.Offset ?? SaveDataManager.SettingData.Offset;
    private float _singleBeatTime => 60f / _bpm;
    private int _currentLevel;
    private bool _isStartMake;
    private bool _isDuringPractice;

    public void Init(StageInfo stageInfo, int firstLevel, bool isNewCreate, List<int> levelList)
    {
        // レベルボタン初期化
        _currentLevel = firstLevel;
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            int level = i + 1;
            var button = _levelButtonList[i];
            button.OnWhenClicked.Subscribe(_ =>
            {
                DarkeningLevelButton();
                button.OnClick();
                ChangeLevel(level);
            });
            if (level == _currentLevel)
            {
                DarkeningLevelButton();
                button.SetLight(true);
            }
        }

        SwitchBallType(isLong: false);
        _currentStageInfo = stageInfo;
        foreach (var level in levelList)
        {
            _levelInfoList.Add(new LevelInfo
            {
                Level = level,
                Notes = stageInfo.LevelList.FirstOrDefault(l => l.Level == level)?.Notes
            });
        }
        StartSubscribeMain(isNewCreate);
        StartSubscribeControllPanel();
        _bpmInput.text = _bpm.ToString();
        _offsetInput.text = _offset.ToString();
        CreateLine();
        _scoreScrollRect.verticalNormalizedPosition = 0;
    }

    private void StartSubscribeMain(bool isNewCreate)
    {
        GameManager.instance.ClickHandler.OnClickScoreLinePocket.Subscribe(x =>
        {
            if (_isEditMode)
            {
                OnClickScoreLine(x.number);
            }
            else
            {
                CreateBall(x.number, x.isLeft, _currentSelectBallType);
            }
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickScoreLine.Subscribe(number =>
        {
            if (_isEditMode)
            {
                OnClickScoreLine(number);
            }
        }).AddTo(this);
        GameManager.instance.ClickHandler.OnClickScoreLineNumber.Subscribe(number =>
        {
            CreateMoveButton(number);
        }).AddTo(this);
        _selectSingleBallButton.OnClickAsObservable().Subscribe(ball =>
        {
            SEManager.instance.PlayButtonSe();
            SwitchBallType(isLong: false);
        }).AddTo(this);
        _selectLongBallButton.OnClickAsObservable().Subscribe(ball =>
        {
            SEManager.instance.PlayButtonSe();
            SwitchBallType(isLong: true);
        }).AddTo(this);
        _playBgmButton.OnClickAsObservable().Subscribe(_ =>
        {
            float posY = _scoreAreaRect.anchoredPosition.y;
            var lineNumber = (_scoreAreaBottom - posY) / (_scoreAreaLayoutGroup.spacing + _scoreLineHeight);
            float currentTime = lineNumber * _singleBeatTime + _offset;
            BGMManager.instance.SetTime(currentTime);
            // BGMの現在時刻より前のラインは全て終わった判定にする
            foreach (var line in _scoreLineList)
            {
                line.IsEnd = line.GetLineTime(_bpm, _offset) < currentTime;
            }
            BGMManager.instance.ChangePause();
        }).AddTo(this);
        _saveButton.OnClickAsObservable().Subscribe(async _ =>
        {
            if (isNewCreate)
            {
                SEManager.instance.PlayButtonSe();
                // ステージ名入力ダイアログを出す
                var option = new InputDialogOption
                {
                    TitleText = "新規保存",
                    OkButtonText = "決定",
                    MessageText = "ステージ名を入力してください",
                    PlaceHolderText = _currentStageInfo.StageHeader.StageName,
                    InitialInputText = _currentStageInfo.StageHeader.StageName,
                };
                var inputDialog = DialogManager.instance.CreateDialog<InputDialog>("UI/InputDialog", option);
                inputDialog.OnCloseDialog.Subscribe(async result  =>
                {
                    if (result is not InputDialogResult inputResult) return;

                    if (inputResult.ResultType == DialogResultType.Ok)
                    {
                        await SaveCurrentStage(inputResult.StageName);
                    }
                });
            }
            else
            {
                SEManager.instance.PlayButtonSe();
                await SaveCurrentStage();
            }
        });
        _practiceButton.OnClickAsObservable().Subscribe(async _ =>
        {
            _isDuringPractice = true;
            _currentStageMaster.notes[_currentLevel - 1] = CreateNoteList();
            var sceneInfo = new BattleSceneInfo
            {
                StageInfo = _currentStageInfo,
                Level = _currentLevel,
                TimeRate = _bgmScrollBar.value,
                IsPractice = true,
                IsAdditional = true
            };
            await GameManager.instance.OpenAdditionalScene(SceneType.Battle, sceneInfo);
        });
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
            GameManager.instance.OpenScene(SceneType.Title, new TitleSceneInfo()).Forget();
        }).AddTo(this);
        _bpmInput.OnEndEditAsObservable().Subscribe(bpm =>
        {
            _currentStageMaster.StageHeader.BPM = float.Parse(bpm);
        }).AddTo(this);
        _offsetInput.OnEndEditAsObservable().Subscribe(offset =>
        {
            _currentStageMaster.StageHeader.NoteTimeOffset = float.Parse(offset);
        }).AddTo(this);
    }

    private async UniTask SaveCurrentStage(string overrideName = "")
    {
        // 現在のレベルの譜面を保存
        UpdateCurrentLevelNotes(overrideName);
        await MasterManager.UpdateOverrideMaster(_currentStageInfo.StageHeader.StageId, _levelInfoList);
        // ダイアログを出す
        DisplaySaveFinishDialog();
    }

    private void DisplaySaveFinishDialog()
    {
        var option = new MessageDialogOption
        {
            TitleText = "保存完了",
            OkButtonText = "OK",
            HideCancelButton = true,
            MessageText = "保存しました"
        };
        DialogManager.instance.CreateDialog<MessageDialog>("UI/MessageDialog", option);
    }

    private void StartSubscribeControllPanel()
    {
        // コントロールパネル内のページ送りボタン
        OnClickControllPanelPageButton(page: 1);
        for (int i = 0; i < _nextPageButtonList.Count; i++)
        {
            int index = i;
            _nextPageButtonList[i].OnClickAsObservable().Subscribe(_ =>
            {
                SEManager.instance.PlayButtonSe();
                OnClickControllPanelPageButton(page: index + 2);
            }).AddTo(this);
        }
        for (int i = 0; i < _backPageButtonList.Count; i++)
        {
            int index = i;
            _backPageButtonList[i].OnClickAsObservable().Subscribe(_ =>
            {
                SEManager.instance.PlayButtonSe();
                OnClickControllPanelPageButton(page: index + 1);
            }).AddTo(this);
        }

        // コピー、ペーストボタン
        _messageMask.gameObject.SetActive(false);
        _selectMask.SetActive(false);
        _copyButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
            CopyLine();
        }).AddTo(this);
        _pasteButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
            _messageText.text = "貼り付け先の最初の列を選択してください";
            _messageMask.gameObject.SetActive(true);
            _isSelectingPaste = true;
        }).AddTo(this);
        _selectCancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
            _selectedLineList.Clear();
            _selectMask.SetActive(false);
        }).AddTo(this);
        _inversionButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
            InversionLine();
        }).AddTo(this);
        _undoButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayButtonSe();
            Undo();
        }).AddTo(this);
        _pasteCancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            _messageMask.gameObject.SetActive(false);
            _isSelectingPaste = false;
        }).AddTo(this);

        _lineSpacingSlider.OnValueChangedAsObservable().Subscribe(value =>
        {
            float spacing = value * _maxLineSpacing;
            _scoreAreaLayoutGroup.spacing = spacing;
            foreach (var line in _longBallLineList)
            {
                line.UpdateLineSpacing(spacing);
            }
        }).AddTo(this);
        // 初期値は中間にしておく
        _lineSpacingSlider.value = 0.5f;
    }

    // Editモードでのライン選択
    private void OnClickScoreLine(int number)
    {
        if (_isSelectingPaste)
        {
            PasteLine(number);
        }
        else
        {
            SelectLine(number);
        }
    }

    private void OnClickControllPanelPageButton(int page)
    {
        _controllPanelPage1.SetActive(page == 1);
        _controllPanelPage2.SetActive(page == 2);
        _controllPanelPage3.SetActive(page == 3);
        // 2ページ目がEditモード
        _isEditMode = page == 2;
    }

    private void CreateLine()
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
        var masterList = _levelInfoList.FirstOrDefault(l => l.Level == _currentLevel)?.Notes;
        if (masterList == null)
        {
            return;
        }
        // ボール作成
        for (int i = 0; i < masterList.Count; i++)
        {
            var targetMaster = masterList[i];
            bool isSingle = targetMaster.type == 1;
            CreateBall(targetMaster.noteNumber, targetMaster.block < 3, isSingle ? ScoreMakerBallType.Single : ScoreMakerBallType.Long);
            if (!isSingle && targetMaster.notes.Count > 0)
            {
                var endNoteMaster = targetMaster.notes[0];
                CreateBall(endNoteMaster.noteNumber, endNoteMaster.block < 3, endNoteMaster.type == 1 ? ScoreMakerBallType.Single : ScoreMakerBallType.Long);
            }
        }
    }

    private void CreateBall(int lineNumber, bool isLeft, ScoreMakerBallType ballType)
    {
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
        // 現在のレベルの譜面を保存
        UpdateCurrentLevelNotes();

        // レベル更新
        _currentLevel = level;
        BGMManager.instance.Pause();
        CreateLine();
    }

    // 現在のレベルの譜面状況を更新
    private void UpdateCurrentLevelNotes(string overrideName = "")
    {
        if (_currentLevelInfo != null)
        {
            _currentLevelInfo.StageNameOverride = overrideName;
            _currentLevelInfo.Notes = CreateNoteList();
        }
        else
        {
            _levelInfoList.Add(new LevelInfo
            {
                Level = _currentLevel,
                StageNameOverride = overrideName,
                Notes = CreateNoteList()
            });
        }
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
        return (BGMManager.instance.CurrentTime - _offset) / _singleBeatTime;
    }

    private void SwitchBallType(bool isLong)
    {
        _currentSelectBallType = isLong ? ScoreMakerBallType.Long : ScoreMakerBallType.Single;
        _singleBallSelectedPanel.SetActive(!isLong);
        _longBallSelectedPanel.SetActive(isLong);
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
            MaskSingleLine(line);
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
                int distance = Mathf.Max(1, Mathf.Abs(oppositLine.LineNumber - line.LineNumber));
                bool isSelectOverLine = oppositLine.LineNumber < line.LineNumber;
                float height = _selectMaskFirstHeight + distance * (_scoreAreaLayoutGroup.spacing + _scoreLineHeight);
                var rectTransform = _selectMask.GetComponent<RectTransform>();
                // マスクの高さを合わせる
                rectTransform.sizeDelta = new Vector2(_selectMaskWidth, height);
                // pivot変更
                int pivotY = isSelectOverLine ? 0 : 1;
                rectTransform.pivot = new Vector2(rectTransform.pivot.x, pivotY);
                // 少しはみ出させる
                int heightDirection = isSelectOverLine ? -1 : 1;
                _selectMask.transform.localPosition = new Vector2(0, heightDirection * _selectMaskFirstHeight / 2);

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

    // 1行だけマスクする
    private void MaskSingleLine(ScoreLine line)
    {
        _selectMask.SetActive(true);
        _selectMask.transform.SetParent(line.transform);
        _selectMask.transform.localPosition = Vector3.zero;
        var rectTransform = _selectMask.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(_selectMaskWidth, _selectMaskFirstHeight);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void RemoveUntilSingleLine(bool isLastRemove)
    {
        while (_selectedLineList.Count > 1)
        {
            _selectedLineList.RemoveAt(isLastRemove ? _selectedLineList.Count - 1 : 0);
        }
        MaskSingleLine(_selectedLineList[0]);
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
    }

    private void UpdatePastScoreLineList()
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
        _pastScoreLineList.Add(lastScoreLineList);
    }

    private void Undo()
    {
        var lastScoreLineList = _pastScoreLineList.LastOrDefault();
        if (lastScoreLineList == null)
        {
            return;
        }
        _pastScoreLineList.Remove(lastScoreLineList);
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
        foreach (var line in _selectedLineList)
        {
            var leftBall = line.GetBall(isLeft: true);
            var rightBall = line.GetBall(isLeft: false);
            line.ClearLine();
            CreateBall(line.LineNumber, isLeft: false, leftBall?.BallType ?? ScoreMakerBallType.None);
            CreateBall(line.LineNumber, isLeft: true, rightBall?.BallType ?? ScoreMakerBallType.None);
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
            if (nextLine != null && BGMManager.instance.CurrentTime > nextLine.GetLineTime(_bpm, _offset) - MasterManager.SettingMaster.ScoreMakerNoteTimeBuffer)
            {
                nextLine.Beat();
                nextLine.IsEnd = true;
            }
        }

        if (!_isEditMode)
        {
            _messageMask.gameObject.SetActive(false);
            return;
        }

        // EditモードのUI制御
        bool isSelectedLine = _selectedLineList.Count > 0;
        bool isCopiedLine = _copiedLineState.Count > 0;
        bool isExsistPastLine = _pastScoreLineList.Count > 0;
        _copyButton.interactable = isSelectedLine;
        _inversionButton.interactable = isSelectedLine;
        _selectCancelButton.interactable = isSelectedLine;
        _pasteButton.interactable = isCopiedLine;
        _undoButton.interactable = isExsistPastLine;
        // 全てのボタンが押せないならメッセージを表示
        if (!isSelectedLine && !isCopiedLine && !isExsistPastLine)
        {
            _messageText.text = "対象の線を選んでください";
            _messageMask.gameObject.SetActive(true);
        }
        else if (!_isSelectingPaste)
        {
            _messageMask.gameObject.SetActive(false);
        }
    }
}
