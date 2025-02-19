using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using System;

public class BattlePresenter : MonoBehaviour
{
    [SerializeField] private BattleView _battleView;

    private SingleStageMaster _currentStageMaster;
    private bool _isFromScoreMaker;

    public void Init(SingleStageMaster singleStageMaster)
    {
        _currentStageMaster = singleStageMaster;
        _battleView.OnWhenClickedBack.Subscribe(_ =>
        {
            if (_isFromScoreMaker)
            {
                GameManager.instance.BackToScoreMaker();
            }
            else
            {
                GameManager.instance.GoToTitle();
            }
        }).AddTo(this);
        _battleView.OnWhenFinishBattle.Subscribe(score =>
        {
            FinishBattle(score).Forget();
        }).AddTo(this);
        _battleView.Init(singleStageMaster);
        _battleView.PrepareBattle();
    }

    public void StartBattle()
    {
        _battleView.BattleStart().Forget();
    }

    // 途中から開始
    public void StartBattleFromScoreMaker(float timeRate)
    {
        _isFromScoreMaker = true;
        _battleView.BattleStartFromMiddle(timeRate);
    }

    public async UniTask FinishBattle(Score score)
    {
        var clearState = new ClearState()
        {
            StageId = _currentStageMaster.StageId,
            Level = _currentStageMaster.LevelId,
        };
        float totalCount = score.CriticalCount + score.HitCount + score.MissCount;
        float criticalMultiple = 100f / totalCount;
        float hitMultiple = 50f / totalCount;
        float missMultiple = -100f / totalCount;
        float realScore = Mathf.Max(0, score.CriticalCount * criticalMultiple + score.HitCount * hitMultiple + score.MissCount * missMultiple);
        if (clearState != null && clearState.Score <= realScore)
        {
            clearState.CriticalNumber = score.CriticalCount;
            clearState.HitNumber = score.HitCount;
            clearState.MissNumber = score.MissCount;
            clearState.Score = realScore;
            clearState.Combo = Math.Max(score.ComboCount, score.MaxComboCount);;
        }
        if (!_battleView.IsTest)
        {
            SaveDataManager.UpdateClearState(clearState);
        }
        await _battleView.StartResultAsync(clearState, SaveDataManager.GetClearState(_currentStageMaster.StageId, _currentStageMaster.LevelId), criticalMultiple, hitMultiple, missMultiple);
    }
}
