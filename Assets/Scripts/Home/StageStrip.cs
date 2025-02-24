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

    public string StageId { get; private set; }
    public float StartTime { get; private set; }
    public float EndTime { get; private set; }

    public Subject<Unit> OnClickedStrip { get; private set; } = new();

    public void Init(StageMaster stageMaster)
    {
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + stageMaster.StageId);
        _enemyImage.sprite = enemySprite;
        _titleText.text = stageMaster.StageName;
        StartTime = stageMaster.StripStartTime;
        EndTime = stageMaster.StripEndTime;
        StageId = stageMaster.StageId;
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
        bool isCleared = clearState.Score > 0;
        _scoreText.text = isCleared ? string.Format("{0:F1}", clearState.Score.RoundDown(1)) : "-";
        _criticalText.text = isCleared ? clearState.CriticalNumber.ToString() : "-";
        _hitText.text = isCleared ? clearState.HitNumber.ToString() : "-";
        _missText.text = isCleared ? clearState.MissNumber.ToString() : "-";
    }

    public void SetSelected(bool isSelected)
    {
        _selectedPanel.gameObject.SetActive(isSelected);
    }
}
