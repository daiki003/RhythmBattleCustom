using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ClearState
{
    public string StageId;
    public int Level;
    public float Score;
    public int CriticalNumber;
    public int HitNumber;
    public int MissNumber;
    public bool IsCleared => CriticalNumber > 0 || HitNumber > 0 || MissNumber > 0;

    public ClearState CreateCopy()
    {
        return new ClearState
        {
            StageId = StageId,
            Level = Level,
            Score = Score,
            CriticalNumber = CriticalNumber,
            HitNumber = HitNumber,
            MissNumber = MissNumber
        };
    }
}

public class SettingData
{
    public float BgmVolume;
    public float SeVolume;
    public float Offset;
    public float BallSpeed;
    public float Target;
}

public static class SaveDataManager
{
    public static List<ClearState> ClearStateList = new();
    public static SettingData SettingData = new();

    private const string _bgmVolumeKey = "BgmVolume";
    private const string _seVolumeKey = "SeVolume";
    private const string _offsetKey = "Offset";
    private const string _ballSpeedKey = "BallSpeed";

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

    private static float GetCurrentTotalScore()
    {
        float totalScore = 0f;
        for (int i = 0; i < ClearStateList.Count; i++)
        {
            totalScore += ClearStateList[i].Score;
        }
        return totalScore;
    }

    private static float GetMaxTotalScore()
    {
        return MasterManager.StageMasterList.Count * 300f;
    }

    private static int GetTotalStar()
    {
        int totalStar = 0;
        var normalStageScoreList = ClearStateList.Where(c => c.Level <= MasterManager.MaxDefaultLevelId).ToList();
        for (int i = 0; i < normalStageScoreList.Count; i++)
        {
            totalStar += GetStarState(normalStageScoreList[i]);
        }
        return totalStar;
    }

    private static int GetMaxStar()
    {
        return MasterManager.StageMasterList.Count * 9;
    }

    // 達成率を計算する
    public static float CalculateAchievementRate()
    {
        float maxAchievementPoint = GetMaxTotalScore() + GetMaxStar() * 10f;
        float currentAchievementPoint = GetCurrentTotalScore() + GetTotalStar() * 10f;
        return currentAchievementPoint / maxAchievementPoint * 100f;
    }

    private static int GetStarState(ClearState clearState)
    {
        // スコアが100であれば3つ星
        if (clearState.Score >= 100f)
        {
            return 3;
        }
        // ミスが1つもなければ2つ星
        if (clearState.Score >= 80f && clearState.MissNumber == 0)
        {
            return 2;
        }
        // スコアが80以上なら1つ星
        if (clearState.Score >= 80f)
        {
            return 1;
        }
        return 0;
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
        }
        PlayFabController.UpdateClearState(ClearStateList).Forget();
    }

    public static void DeleteClearState(string stageId, List<int> levelList)
    {
        ClearStateList.RemoveAll(c => c.StageId == stageId && levelList.Contains(c.Level));
        PlayFabController.UpdateClearState(ClearStateList).Forget();
    }

    public static void CreateSettingData()
    {
        SettingData = new SettingData()
        {
            BgmVolume = PlayerPrefs.GetFloat(_bgmVolumeKey, 0.5f),
            SeVolume = PlayerPrefs.GetFloat(_seVolumeKey, 0.5f),
            Offset = PlayerPrefs.GetFloat(_offsetKey),
            BallSpeed = PlayerPrefs.GetFloat(_ballSpeedKey)
        };
        BGMManager.instance.AdjustVolume(SettingData.BgmVolume);
        SEManager.instance.AdjustVolume(SettingData.SeVolume);
    }

    public static void UpdateSettingData(float bgmVolume, float seVolume, float offset, float ballSpeed, float target)
    {
        SettingData.BgmVolume = bgmVolume;
        SettingData.SeVolume = seVolume;
        SettingData.Offset = offset;
        SettingData.BallSpeed = ballSpeed;
        SettingData.Target = target;

        // タイトルで使うのでPlayerPrefsに保存
        PlayerPrefs.SetFloat(_bgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(_seVolumeKey, seVolume);
        PlayerPrefs.SetFloat(_offsetKey, offset);
        PlayerPrefs.SetFloat(_ballSpeedKey, ballSpeed);
    }
}