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
    [SerializeField] private Text _comboText;

    [SerializeField] private Text _highScoreCriticalText;
    [SerializeField] private Text _highScoreHitText;
    [SerializeField] private Text _highScoreMissText;
    [SerializeField] private Text _highScoreText;
    [SerializeField] private Text _highScoreComboText;

    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _goHomeButton;

    public Subject<Unit> OnWhenPushRestart = new Subject<Unit>();
    public Subject<Unit> OnWhenPushGoHome = new Subject<Unit>();

    public void SetScore(ClearState clearState, ClearState highScoreClearState, float criticalMultiple, float hitMultiple, float missMultiple)
    {
        _criticalNumberText.text = clearState.CriticalNumber.ToString();
        _hitNumberText.text = clearState.HitNumber.ToString();
        _missNumberText.text = clearState.MissNumber.ToString();
        _totalScoreText.text = FloatUtility.RoundDown(clearState.Score, 2).ToString();
        _comboText.text = clearState.Combo.ToString();

        _criticalMultipleText.text = FloatUtility.RoundDown(criticalMultiple, 2).ToString();
        _hitMultipleText.text = FloatUtility.RoundDown(hitMultiple, 2).ToString();
        _missMultipleText.text = FloatUtility.RoundDown(missMultiple, 2).ToString();
        _criticalScoreText.text = FloatUtility.RoundDown(clearState.CriticalNumber * criticalMultiple, 2).ToString();
        _hitScoreText.text = FloatUtility.RoundDown(clearState.HitNumber * hitMultiple, 2).ToString();
        _missScoreText.text = FloatUtility.RoundDown(clearState.MissNumber * missMultiple, 2).ToString();

        _highScoreCriticalText.text = highScoreClearState.CriticalNumber.ToString();
        _highScoreHitText.text = highScoreClearState.HitNumber.ToString();
        _highScoreMissText.text = highScoreClearState.MissNumber.ToString();
        _highScoreText.text = FloatUtility.RoundDown(highScoreClearState.Score, 2).ToString();
        _highScoreComboText.text = highScoreClearState.Combo.ToString();

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
}
