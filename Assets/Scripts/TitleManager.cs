using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Transform _stageStripTransform;
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _comboText;
    [SerializeField] private Text _criticalText;
    [SerializeField] private Text _hitText;
    [SerializeField] private Text _missText;
    [SerializeField] private Button _startButton;

    private int _selectedStage;
    private int _selectedLevel;


    void Start()
    {
        var strip = Instantiate(_stageStripPrefab, _stageStripTransform);
        strip.Init(1);
        strip.OnWhenClickLevelButton.Subscribe(x =>
        {
            var clearState = SaveDataManager.ClearStateList.FirstOrDefault(c => c.StageId == x.stageId && c.Level == x.level);
            if (clearState == null)
            {
                return;
            }
            _selectedStage = x.stageId;
            _selectedLevel = x.level;
            _levelText.text = x.level.ToString();
            _scoreText.text = clearState.Score.ToString();
            _comboText.text = clearState.Combo.ToString();
            _criticalText.text = clearState.CriticalNumber.ToString();
            _hitText.text = clearState.HitNumber.ToString();
            _missText.text = clearState.MissNumber.ToString();
        });
        _startButton.OnClickAsObservable().Subscribe(x =>
        {
            StartBattle(_selectedStage, _selectedLevel);
        });
    }

    private void StartBattle(int stageId, int level)
    {
        gameObject.SetActive(false);
        GameManager.instance.StartBattle(stageId, level);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
