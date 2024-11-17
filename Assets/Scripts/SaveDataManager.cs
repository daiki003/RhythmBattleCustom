using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClearState
{
    public string StageId;
    public int Level;
    public float Score;
    public int CriticalNumber;
    public int HitNumber;
    public int MissNumber;
    public int Combo;
}

public static class SaveDataManager
{
    public static List<ClearState> ClearStateList = new List<ClearState>();

    public static ClearState GetClearState(string stageId, int level)
    {
        var clearState = ClearStateList.FirstOrDefault(c => c.StageId == stageId && c.Level == level);
        if (clearState == null)
        {
            // クリア状況が作られていなければここで作る
            clearState = new ClearState()
            {
                StageId = stageId,
                Level = level
            };
            ClearStateList.Add(clearState);
        }
        return clearState;
    }

    public static float GetTotalScore()
    {
        float totalScore = 0f;
        for (int i = 0; i < ClearStateList.Count; i++)
        {
            totalScore += ClearStateList[i].Score;
        }
        return totalScore;
    }

    public static void UpdateClearState(ClearState clearState)
    {
        var targetState = GetClearState(clearState.StageId, clearState.Level);
        if (targetState != null)
        {
            if (targetState.Score <= clearState.Score)
            {
                targetState.CriticalNumber = clearState.CriticalNumber;
                targetState.HitNumber = clearState.HitNumber;
                targetState.MissNumber = clearState.MissNumber;
                targetState.Score = clearState.Score;
            }
            if (targetState.Combo <= clearState.Combo)
            {
                targetState.Combo = clearState.Combo;
            }
        }
        PlayFabController.UpdateClearState(ClearStateList);
    }
}