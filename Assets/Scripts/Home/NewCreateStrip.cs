using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class NewCreateStrip : MonoBehaviour
{
    [SerializeField] private Button _stripButton;
    [SerializeField] private Image _selectedPanel;
    [SerializeField] private Image _enemyImage;
    [SerializeField] private Text _titleText;

    public string MusicId { get; private set; }
    public int StageId { get; private set; }
    public float StartTime { get; private set; }
    public float EndTime { get; private set; }

    public Subject<Unit> OnClickedStrip { get; private set; } = new();

    public void Init(StageHeader stageHeader, int level = 0, bool isDisplayLevel = false)
    {
        _titleText.text = stageHeader.StageName + (isDisplayLevel ? "Lv" + level : "");
        StartTime = stageHeader.StripStartTime;
        EndTime = stageHeader.StripEndTime;
        SetStageId(stageHeader.MusicId);
        StageId = level;
        _selectedPanel.gameObject.SetActive(false);
        _stripButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickedStrip.OnNext(default);
        }).AddTo(this);
    }

    public void SetStageId(string stageId)
    {
        MusicId = stageId;
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + MusicId);
        _enemyImage.sprite = enemySprite;
    }

    public void SetSelected(bool isSelected)
    {
        _selectedPanel.gameObject.SetActive(isSelected);
    }
}
