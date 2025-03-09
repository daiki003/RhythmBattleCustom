using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using System;
using System.Linq;

public class BattlePresenter : MonoBehaviour
{
    [SerializeField] private BattleView _battleView;

    private BattleSceneInfo _battleSceneInfo;

    public void Init(BattleSceneInfo battleSceneInfo)
    {
        _battleSceneInfo = battleSceneInfo;
        _battleView.OnWhenClickedBack.Subscribe(_ =>
        {
            if (_battleSceneInfo.IsAdditional)
            {
                GameManager.instance.BackToMainScene().Forget();
            }
            else
            {
                GameManager.instance.OpenScene(SceneType.Home, new TitleSceneInfo()).Forget();
            }
        }).AddTo(this);
        _battleView.OnWhenFinishBattle.Subscribe(score =>
        {
            FinishBattle(score);
        }).AddTo(this);
        _battleView.Init(_battleSceneInfo.StageInfo.StageHeader, _battleSceneInfo.LevelInfo, _battleSceneInfo.IsPractice);
        _battleView.PrepareBattle();
        if (_battleSceneInfo.IsAdditional)
        {
            _battleView.SetSliderForAdditional(_battleSceneInfo.TimeRate);
        }
    }

    public void StartBattle()
    {
        if (_battleSceneInfo.TimeRate > 0)
        {
            _battleView.BattleStartFromMiddle();
        }
        else
        {
            _battleView.BattleStart().Forget();
        }
    }

    public void FinishBattle(Score score)
    {
        var clearState = new ClearState()
        {
            StageId = _battleSceneInfo.StageInfo.StageHeader.StageId,
            Level = _battleSceneInfo.Level,
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
        _battleView.StartResultAsync(clearState, SaveDataManager.GetClearState(_battleSceneInfo.StageInfo.StageHeader.StageId, _battleSceneInfo.Level), criticalMultiple, hitMultiple, missMultiple).Forget();
    }
}
