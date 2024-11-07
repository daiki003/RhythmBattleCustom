using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class StageStrip : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _comboText;
    [SerializeField] private Text _criticalText;
    [SerializeField] private Text _hitText;
    [SerializeField] private Text _missText;

    private string _stageId;
    private int _level;

    public void Init(string stageId, int level)
    {
        _stageId = stageId;
        _level = level;
        _startButton.OnClickAsObservable().Subscribe(async x =>
        {
            await StartBattle(_stageId, _level);
        });
        UpdateScore();
    }

    public void UpdateScore()
    {
        var clearState = SaveDataManager.ClearStateList.FirstOrDefault(c => c.StageId == _stageId && c.Level == _level);
        if (clearState == null)
        {
            return;
        }
        _scoreText.text = Mathf.Floor(clearState.Score).ToString();
        _comboText.text = clearState.Combo.ToString();
        _criticalText.text = clearState.CriticalNumber.ToString();
        _hitText.text = clearState.HitNumber.ToString();
        _missText.text = clearState.MissNumber.ToString();
    }

    private async UniTask StartBattle(string stageId, int level)
    {
        await GameManager.instance.StartBattle(stageId, level);
    }
}
