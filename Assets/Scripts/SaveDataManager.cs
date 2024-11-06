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
    public static void UpdateClearState(string stageId, int level, int criticalNumber, int hitNumber, int missNumber, int combo)
    {
        var targetState = ClearStateList.FirstOrDefault(c => c.StageId == stageId && c.Level == level);
        if (targetState == null)
        {
            targetState = new ClearState()
            {
                StageId = stageId,
                Level = level,
            };
            ClearStateList.Add(targetState);
        }
        float baseScore = criticalNumber * 100 + hitNumber * 50 - missNumber * 100;
        float realScore = Mathf.Max(0, baseScore / (criticalNumber + hitNumber + missNumber));
        if (targetState != null && targetState.Score <= realScore)
        {
            targetState.CriticalNumber = criticalNumber;
            targetState.HitNumber = hitNumber;
            targetState.MissNumber = missNumber;
            targetState.Score = realScore;
        }
        if (targetState != null && targetState.Combo <= combo)
        {
            targetState.Combo = combo;
        }
        PlayFabController.UpdateClearState(ClearStateList);
    }
}