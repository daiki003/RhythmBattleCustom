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

    public string StageId { get; private set; }
    public int Level { get; private set; }
    public float StartTime { get; private set; }
    public float EndTime { get; private set; }

    public Subject<Unit> OnClickedStrip { get; private set; } = new();

    public void Init(StageHeader stageHeader, int level = 0, bool isDisplayLevel = false)
    {
        _titleText.text = stageHeader.StageName + (isDisplayLevel ? "Lv" + level : "");
        StartTime = stageHeader.StripStartTime;
        EndTime = stageHeader.StripEndTime;
        SetStageId(stageHeader.StageId);
        Level = level;
        _selectedPanel.gameObject.SetActive(false);
        _stripButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickedStrip.OnNext(default);
        });
    }

    public void SetStageId(string stageId)
    {
        StageId = stageId;
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + StageId);
        _enemyImage.sprite = enemySprite;
    }

    public void SetSelected(bool isSelected)
    {
        _selectedPanel.gameObject.SetActive(isSelected);
    }
}
