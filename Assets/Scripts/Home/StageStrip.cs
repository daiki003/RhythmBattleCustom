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

    private StageHeader _stageHeader;
    public string StageId => _stageHeader.StageId;
    public float StartTime => _stageHeader.StripStartTime;
    public float EndTime => _stageHeader.StripEndTime;
    public int LevelId { get; private set; }

    public Subject<Unit> OnClickedStrip { get; private set; } = new();

    public void Init(StageHeader stageHeader, int level, string overrideName = "")
    {
        _stageHeader = stageHeader;
        LevelId = level;
        _titleText.text = string.IsNullOrEmpty(overrideName) ? _stageHeader.StageName : overrideName;
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + StageId);
        _enemyImage.sprite = enemySprite;
        _selectedPanel.gameObject.SetActive(false);
        _stripButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickedStrip.OnNext(default);
        });
    }

    public void UpdateScore(int level)
    {
        var clearState = SaveDataManager.GetClearState(StageId, level);
        if (clearState == null)
        {
            return;
        }
        bool isCleared = clearState.CriticalNumber > 0 || clearState.HitNumber > 0 || clearState.MissNumber > 0;
        _scoreText.text = isCleared ? string.Format("{0:F2}", clearState.Score.RoundDown(2)) : "-";
        _criticalText.text = isCleared ? clearState.CriticalNumber.ToString() : "-";
        _hitText.text = isCleared ? clearState.HitNumber.ToString() : "-";
        _missText.text = isCleared ? clearState.MissNumber.ToString() : "-";
    }

    public void SetSelected(bool isSelected)
    {
        _selectedPanel.gameObject.SetActive(isSelected);
    }
}
