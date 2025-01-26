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
    [SerializeField] private Text _titleText;
    [SerializeField] private Button _startButton;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _criticalText;
    [SerializeField] private Text _hitText;
    [SerializeField] private Text _missText;
    [SerializeField] private ButtonWithBacklight _bgmButton;
    public ButtonWithBacklight BgmButton => _bgmButton;

    private string _stageId;
    private int _level;
    private bool _isScoreMaker;
    private bool _isBgmPlaying;

    public Subject<(string stageId, bool isPlay)> OnClickedBgmButton { get; private set; } = new Subject<(string, bool)>();

    public void Init(string stageId, string stageName, int level, bool isScoreMaker = false)
    {
        var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + stageId);
        _enemyImage.sprite = enemySprite;
        _titleText.text = stageName;
        _stageId = stageId;
        _level = level;
        _isScoreMaker = isScoreMaker;
        _startButton.OnClickAsObservable().Subscribe(async x =>
        {
            if (_isScoreMaker)
            {
                GameManager.instance.StartScoreMaker(_stageId);
            }
            else
            {
                await GameManager.instance.StartBattle(stageId, level);
            }
        }).AddTo(this);
        _bgmButton.Button.OnClickAsObservable().Subscribe(_ =>
        {
            _isBgmPlaying = !_isBgmPlaying; 
            OnClickedBgmButton.OnNext((_stageId, _isBgmPlaying));
        }).AddTo(this);
        _bgmButton.SetBacklight(false);
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
        _criticalText.text = clearState.CriticalNumber.ToString();
        _hitText.text = clearState.HitNumber.ToString();
        _missText.text = clearState.MissNumber.ToString();
    }
}
