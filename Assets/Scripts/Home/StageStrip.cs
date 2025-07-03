using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class StageStrip : MonoBehaviour
{
    [SerializeField] private Button _stripButton;
    [SerializeField] private Image _selectedPanel;
    [SerializeField] private Image _enemyImage;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _criticalText;
    [SerializeField] private Text _hitText;
    [SerializeField] private Text _missText;
    [SerializeField] private ScoreStars _scoreStars;

    private StageHeader _stageHeader;
    public string StageId => _stageHeader.MusicId;
    public float StartTime => _stageHeader.StripStartTime;
    public float EndTime => _stageHeader.StripEndTime;
    public int LevelId { get; private set; }

    public Subject<Unit> OnClickedStrip { get; private set; } = new();

    public void Init(StageHeader stageHeader, int level)
    {
        _stageHeader = stageHeader;
        LevelId = level;
        _titleText.text = _stageHeader.StageName;
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + StageId);
        _enemyImage.sprite = enemySprite;
        _selectedPanel.gameObject.SetActive(false);
        _stripButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickedStrip.OnNext(default);
        }).AddTo(this);
    }

    public void UpdateScore(int level)
    {
        var clearState = SaveDataManager.GetClearState(StageId, level);
        if (clearState == null)
        {
            return;
        }
        bool isCleared = clearState.IsCleared;
        _scoreText.text = isCleared ? string.Format("{0:F2}", clearState.Score.RoundDown(2)) : "-";
        _criticalText.text = isCleared ? clearState.CriticalNumber.ToString() : "-";
        _hitText.text = isCleared ? clearState.HitNumber.ToString() : "-";
        _missText.text = isCleared ? clearState.MissNumber.ToString() : "-";
        _scoreStars.UpdteStar(clearState);
    }

    public void SetSelected(bool isSelected)
    {
        _selectedPanel.gameObject.SetActive(isSelected);
    }
}
