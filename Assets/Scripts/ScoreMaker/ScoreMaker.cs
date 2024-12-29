using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;

public class ScoreMaker : MonoBehaviour
{
    [SerializeField] private Transform _dialogTransform;
    [SerializeField] private Transform _scoreLineTransform;
    [SerializeField] private Button _selectSingleBallButton;
    [SerializeField] private Button _selectLongBallButton;
    [SerializeField] private GameObject _singleBallSelectedPanel;
    [SerializeField] private GameObject _longBallSelectedPanel;
    [SerializeField] private ScoreLine _scoreLinePrefab; 
    [SerializeField] private ScrollRect _scoreScrollRect;
    [SerializeField] private Button _playBgmButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private List<MenuButton> _levelButtonList = new();

    [SerializeField] private InputField _bpmInput;
    [SerializeField] private InputField _offsetInput;

    [SerializeField] private RectTransform _scoreAreaRect;
    [SerializeField] private Transform _ballLineTransform;

    private StageMaster _currentStageMaster;
    private List<ScoreLine> _scoreLineList = new();

    private const float _scoreAreaBottom = -1500f;
    private const float _scoreLineSpacing = 150f;
    private const float _scoreLineHeight = 15f;

    public enum ScoreMakerBallType
    {
        None,
        Single,
        Long
    }
    private ScoreMakerBallType _currentSelectBallType;
    private float _bpm => _currentStageMaster != null ? _currentStageMaster.BPM : 520f;
    private float _offset => _currentStageMaster != null ? _currentStageMaster.NoteTimeOffset : 0f;
    private float _singleBeatTime => 60f / _bpm;
    private int _currentLevel;

    public void Init()
    {
        GameManager.instance.ClickHandler.OnClickScoreLine.Subscribe(x =>
        {
            CreateBall(x.number, x.isLeft, _currentSelectBallType);
        }).AddTo(this);
        _selectSingleBallButton.OnClickAsObservable().Subscribe(ball =>
        {
            SEManager.instance.PlayBeatSe();
            SwitchBallType(isLong: false);
        }).AddTo(this);
        _selectLongBallButton.OnClickAsObservable().Subscribe(ball =>
        {
            SEManager.instance.PlayBeatSe();
            SwitchBallType(isLong: true);
        }).AddTo(this);
        _playBgmButton.OnClickAsObservable().Subscribe(_ =>
        {
            float posY = _scoreAreaRect.anchoredPosition.y;
            var lineNumber = (_scoreAreaBottom - posY) / (_scoreLineSpacing + _scoreLineHeight);
            BGMManager.instance.SetTime(lineNumber * _singleBeatTime + _offset);
            foreach (var line in _scoreLineList)
            {
                line.IsEnd = line.GetLineTime(_bpm, _offset) < BGMManager.instance.CurrentTime;
            }
            BGMManager.instance.Pause();
        }).AddTo(this);
        _saveButton.OnClickAsObservable().Subscribe(async _ =>
        {
            SEManager.instance.PlayBeatSe();
            // 現在のレベルの譜面を保存
            _currentStageMaster.notes[_currentLevel] = CreateNoteList();
            await PlayFabController.UpdateOverrideScore(_currentStageMaster);
            MasterManager.SetOverrideMaster(_currentStageMaster);
            // ダイアログを出す
            var dialog = Instantiate(ResourceManager.LoadPrefab("Dialog"), _dialogTransform).GetComponent<Dialog>();
            dialog.Init("保存しました", "閉じる");
        });
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEManager.instance.PlayBeatSe();
            GameManager.instance.GoToTitle();
        }).AddTo(this);
        _bpmInput.OnEndEditAsObservable().Subscribe(bpm =>
        {
            _currentStageMaster.BPM = float.Parse(bpm);
        }).AddTo(this);
        _offsetInput.OnEndEditAsObservable().Subscribe(offset =>
        {
            _currentStageMaster.NoteTimeOffset = float.Parse(offset);
        }).AddTo(this);
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            int level = i;
            var button = _levelButtonList[i];
            button.OnWhenClicked.Subscribe(_ =>
            {
                DarkeningLevelButton();
                button.OnClick();
                ChangeLevel(level);
            });
            if (i == 2)
            {
                DarkeningLevelButton();
                button.SetLight(true);
            }
        }

        SwitchBallType(isLong: false);
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

        var masterList = _currentStageMaster.notes[_currentLevel];
        int lineNumber = (int)(_bpm * (BGMManager.instance.CurrentClipLength / 60f));
        // ライン作成
        for (int i = 0; i < lineNumber; i++)
        {
            var scoreLine = Instantiate(_scoreLinePrefab, _scoreLineTransform);
            scoreLine.transform.SetSiblingIndex(0);
            scoreLine.Init(i);
            _scoreLineList.Add(scoreLine);
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
                var linePrefab = ResourceManager.LoadPrefab("ScoreMakerBallLine");
                var line = Instantiate(linePrefab, _ballLineTransform).GetComponent<ScoreMakerBallLine>();
                line.Init(isHead ? createdBall : pairBall, isHead ? pairBall : createdBall);
            }
        }
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

    public void StartMake(string stageId)
    {
        _scoreScrollRect.verticalNormalizedPosition = 0;
        _currentStageMaster = MasterManager.GetStageMaster(stageId).CreateCopy();
        _bpmInput.text = _bpm.ToString();
        _offsetInput.text = _offset.ToString();
        BGMManager.instance.SetClip(stageId);
        // 初期レベルは2
        _currentLevel = 2;
        CreateLine();
        BGMManager.instance.Play();
    }

    private void ChangeLevel(int level)
    {
        // 現在のレベルの譜面を保存
        _currentStageMaster.notes[_currentLevel] = CreateNoteList();

        // レベル更新
        _currentLevel = level;
        BGMManager.instance.Pause(forcePause: true);
        CreateLine();
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

    void Update()
    {
        if (BGMManager.instance.IsPlaying)
        {
            float currentLineNumber = GetCurrentLineNumber();
            float anchorY = _scoreAreaBottom - (_scoreLineSpacing + _scoreLineHeight) * currentLineNumber;
            _scoreAreaRect.anchoredPosition = new Vector3(200, anchorY, 0);
            var nextLine = _scoreLineList.FirstOrDefault(l => !l.IsEnd);
            if (nextLine != null && BGMManager.instance.CurrentTime > nextLine.GetLineTime(_bpm, _offset) - MasterManager.SettingMaster.ScoreMakerNoteTimeBuffer)
            {
                nextLine.Beat();
                nextLine.IsEnd = true;
            }
        }
    }
}
