using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class StageStrip : MonoBehaviour
{
    [SerializeField] private Image _enemyImage;
    [SerializeField] private Button _startButton;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _comboText;
    [SerializeField] private Text _criticalText;
    [SerializeField] private Text _hitText;
    [SerializeField] private Text _missText;

    private string _stageId;
    private int _level;
    private bool _isScoreMaker;

    public void Init(string stageId, int level, bool isScoreMaker = false)
    {
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + stageId);
        _enemyImage.sprite = enemySprite;
        _stageId = stageId;
        _level = level;
        _isScoreMaker = isScoreMaker;
        _startButton.OnClickAsObservable().Subscribe(async x =>
        {
            if (_isScoreMaker)
            {
                GameManager.instance.GoToScoreMaker(_stageId);
            }
            else
            {
                await StartBattle(_stageId, _level);
            }
        });
        UpdateScore();
    }

    public void UpdateScore()
    {
        var clearState = SaveDataManager.GetClearState(_stageId, _level);
        if (clearState == null)
        {
            return;
        }
        _scoreText.text = FloatUtility.RoundDown(clearState.Score, 2).ToString();
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
