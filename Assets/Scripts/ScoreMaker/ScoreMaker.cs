using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;

public class ScoreMaker : MonoBehaviour
{
    [SerializeField] private Transform _scoreLineTransform;
    [SerializeField] private Button _selectSingleBallButton;
    [SerializeField] private Button _selectLongBallButton;
    [SerializeField] private GameObject _singleBallSelectedPanel;
    [SerializeField] private GameObject _longBallSelectedPanel;
    [SerializeField] private ScoreLine _scoreLinePrefab; 
    [SerializeField] private ContentSizeFitter _lineContentSizeFitter;
    [SerializeField] private ScrollRect _scoreScrollRect;
    [SerializeField] private Scrollbar _scoreScrollBar;
    [SerializeField] private Button _playBgmButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private List<Button> _levelButtonList = new List<Button>();

    [SerializeField] private InputField _bpmInput;
    [SerializeField] private InputField _offsetInput;

    [SerializeField] private RectTransform _scoreAreaRect;
    [SerializeField] private Transform _ballLineTransform;

    private StageMaster _currentOriginalStageMaster;
    private List<ScoreLine> _scoreLineList = new List<ScoreLine>();

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
    private float _bpm = 520f;
    private float _offset = 0f;
    private float _singleBeatTime => 60f / _bpm;
    private int _currentLevel;

    public void Init()
    {
        GameManager.instance.ClickHandler.OnClickScoreLine.Subscribe(x =>
        {
            CreateBall(x.number, x.isLeft, _currentSelectBallType);
        });
        GameManager.instance.ClickHandler.OnDragSingleBall.Subscribe(ball =>
        {
            
        });
        _selectSingleBallButton.OnClickAsObservable().Subscribe(ball =>
        {
            SwitchBallType(isLong: false);
        });
        _selectLongBallButton.OnClickAsObservable().Subscribe(ball =>
        {
            SwitchBallType(isLong: true);
        });
        _playBgmButton.OnClickAsObservable().Subscribe(_ =>
        {
            // BGMManager.instance.SetTimeByRate(_scoreScrollRect.verticalNormalizedPosition, _offset);
            float posY = _scoreAreaRect.anchoredPosition.y;
            var lineNumber = (_scoreAreaBottom - posY) / (_scoreLineSpacing + _scoreLineHeight);
            BGMManager.instance.SetTime(lineNumber * _singleBeatTime + _offset);
            foreach (var line in _scoreLineList)
            {
                line.IsEnd = line.LineNumber * (60f / _bpm) < BGMManager.instance.CurrentTime;
            }
            BGMManager.instance.Pause();
        });
        _saveButton.OnClickAsObservable().Subscribe(_ =>
        {
            PlayFabController.UpdateOverrideScore(CreateMaster());
        });
        _backButton.OnClickAsObservable().Subscribe(_ =>
        {
            GameManager.instance.GoToTitle();
        });
        _bpmInput.OnEndEditAsObservable().Subscribe(bpm =>
        {
            _bpm = float.Parse(bpm);
        });
        _offsetInput.OnEndEditAsObservable().Subscribe(offset =>
        {
            _offset = float.Parse(offset);
        });
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            int level = i;
            _levelButtonList[i].OnClickAsObservable().Subscribe(_ =>
            {
                ChangeLevel(level);
            });
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

        var masterList = _currentOriginalStageMaster.notes[_currentLevel];
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
        _currentOriginalStageMaster = MasterManager.GetStageMaster(stageId);
        _bpm = _currentOriginalStageMaster.BPM;
        _offset = _currentOriginalStageMaster.NoteTimeOffset;
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

    public StageMaster CreateMaster()
    {
        var newMaster = new StageMaster()
        {
            StageId = _currentOriginalStageMaster.StageId,
            BPM = _bpm,
            LPB = _currentOriginalStageMaster.LPB,
            NoteTimeOffset = _offset,
            notes = _currentOriginalStageMaster.notes,
        };
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
        newMaster.notes[2] = noteList;
        return newMaster;
    }

    void Update()
    {
        if (BGMManager.instance.IsPlaying)
        {
            float currentLineNumber = GetCurrentLineNumber();
            float anchorY = _scoreAreaBottom - (_scoreLineSpacing + _scoreLineHeight) * currentLineNumber;
            _scoreAreaRect.anchoredPosition = new Vector3(200, anchorY, 0);
            var nextLine = _scoreLineList.FirstOrDefault(l => !l.IsEnd);
            if (nextLine != null && BGMManager.instance.CurrentTime > nextLine.LineNumber * (60f / _bpm) + _offset - MasterManager.SettingMaster.TestNoteTimeBuffer)
            {
                nextLine.Beat();
                nextLine.IsEnd = true;
            }
        }
    }
}
