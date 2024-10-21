using System.Collections.Generic;
using System.Linq;

public class ClearState
{
    public int StageId;
    public int Level;
    public int Score;
    public int CriticalNumber;
    public int HitNumber;
    public int MissNumber;
    public int Combo;
}

public static class SaveDataManager
{
    public static List<ClearState> ClearStateList = new List<ClearState>();
    public static void UpdateClearState(int stageId, int level, int criticalNumber, int hitNumber, int missNumber)
    {
        var targetState = ClearStateList.FirstOrDefault(c => c.StageId == stageId && c.Level == level);
        if (targetState != null && targetState.CriticalNumber <= criticalNumber)
        {
            targetState.CriticalNumber = criticalNumber;
            targetState.HitNumber = hitNumber;
            targetState.MissNumber = missNumber;
        }
        PlayFabController.UpdateClearState(ClearStateList);
    }
}