using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ResultView : MonoBehaviour
{
    [SerializeField] private Text _criticalNumberText;
    [SerializeField] private Text _criticalMultipleText;
    [SerializeField] private Text _criticalScoreText;
    [SerializeField] private Text _hitNumberText;
    [SerializeField] private Text _hitMultipleText;
    [SerializeField] private Text _hitScoreText;
    [SerializeField] private Text _missNumberText;
    [SerializeField] private Text _missMultipleText;
    [SerializeField] private Text _missScoreText;
    [SerializeField] private Text _totalScoreText;
    [SerializeField] private GameObject _newHighScore;

    [SerializeField] private Text _highScoreCriticalText;
    [SerializeField] private Text _highScoreHitText;
    [SerializeField] private Text _highScoreMissText;
    [SerializeField] private Text _highScoreText;

    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _goHomeButton;

    public Subject<Unit> OnWhenPushRestart = new Subject<Unit>();
    public Subject<Unit> OnWhenPushGoHome = new Subject<Unit>();

    public void Init()
    {
        _restartButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnWhenPushRestart.OnNext(default);
            gameObject.SetActive(false);
        });
        _goHomeButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnWhenPushGoHome.OnNext(default);
        });
    }

    public void SetScore(ClearState clearState, ClearState highScoreClearState, float criticalMultiple, float hitMultiple, float missMultiple)
    {
        _criticalNumberText.text = clearState.CriticalNumber.ToString();
        _hitNumberText.text = clearState.HitNumber.ToString();
        _missNumberText.text = clearState.MissNumber.ToString();
        _totalScoreText.text = clearState.Score.RoundDown(2).ToString();

        _criticalMultipleText.text = criticalMultiple.RoundDown(2).ToString();
        _hitMultipleText.text = hitMultiple.RoundDown(2).ToString();
        _missMultipleText.text = missMultiple.RoundDown(2).ToString();
        _criticalScoreText.text = (clearState.CriticalNumber * criticalMultiple).RoundDown(2).ToString();
        _hitScoreText.text = (clearState.HitNumber * hitMultiple).RoundDown(2).ToString();
        _missScoreText.text = (clearState.MissNumber * missMultiple).RoundDown(2).ToString();

        _highScoreCriticalText.text = highScoreClearState.CriticalNumber.ToString();
        _highScoreHitText.text = highScoreClearState.HitNumber.ToString();
        _highScoreMissText.text = highScoreClearState.MissNumber.ToString();
        _highScoreText.text = highScoreClearState.Score.RoundDown(2).ToString();

        _newHighScore.SetActive(clearState.Score > highScoreClearState.Score);
    }
}
