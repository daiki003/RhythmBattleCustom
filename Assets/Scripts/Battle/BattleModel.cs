using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleModel
{
    private const float _criticalBaseMultiple = 100f;
    private const float _hitBaseMultiple = 50f;
    private const float _missBaseMultiple = -200f;

    public float CriticalMultiple { get; private set; }
    public float HitMultiple { get; private set; }
    public float MissMultiple { get; private set; }

    public ClearState CalculateScore(Score score, string stageId, int level)
    {
        var clearState = new ClearState()
        {
            StageId = stageId,
            Level = level,
        };
        float totalCount = score.CriticalCount + score.HitCount + score.MissCount;
        CriticalMultiple = _criticalBaseMultiple / totalCount;
        HitMultiple = _hitBaseMultiple / totalCount;
        MissMultiple = _missBaseMultiple / totalCount;
        float realScore = Mathf.Max(0, score.CriticalCount * CriticalMultiple + score.HitCount * HitMultiple + score.MissCount * MissMultiple);

        if (clearState != null && clearState.Score <= realScore)
        {
            clearState.CriticalNumber = score.CriticalCount;
            clearState.HitNumber = score.HitCount;
            clearState.MissNumber = score.MissCount;
            clearState.Score = realScore;
        }
        SaveDataManager.UpdateClearState(clearState);
        return clearState;
    }
}
