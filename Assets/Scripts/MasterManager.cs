using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class SettingMaster
{
    public float CriticalTimeBuffer;
    public float HitTimeBuffer;
    public float BallTimeOffset;
    public float BallSpeedCoefficient;
    public float TestNoteTimeBuffer;
    public float ScoreMakerNoteTimeBuffer;
}

public class NoteMaster
{
    public int lpb;
    public int num;
    public int block;
    public int type;
    public List<NoteMaster> notes = new List<NoteMaster>();
    public int noteNumber => num * (4 / lpb);
}

public class StageHeader
{
    public string StageId;
    public string StageName;
    public float StripStartTime;
    public float StripEndTime;
    public float BPM;
    public int LPB;
    public float NoteTimeOffset;
    public float AdditionalBallSpeed;

    public StageHeader CreateCopy()
    {
        return new StageHeader
        {
            StageId = StageId,
            StageName = StageName,
            StripStartTime = StripStartTime,
            StripEndTime = StripEndTime,
            BPM = BPM,
            LPB = LPB,
            NoteTimeOffset = NoteTimeOffset,
            AdditionalBallSpeed = AdditionalBallSpeed
        };
    }
}

public class StageMaster
{
    public string StageId => StageHeader.StageId;
    public StageHeader StageHeader;
    public List<List<NoteMaster>> notes = new List<List<NoteMaster>>();
}

public class SingleStageMaster
{
    public string StageId;
    public int LevelId;
    public string StageNameOverride;
    public List<NoteMaster> notes = new List<NoteMaster>();
}

public class TitleDataResult
{
    public SettingMaster SettingeMaster;
    public List<StageMaster> StageMasterList = new List<StageMaster>();
}

public class PlayerDataResult
{
    public List<ClearState> ClearStateList = new();
    public SettingData SettingData = new();
    public List<StageMaster> OverrideStageMasterList = new();
    public List<SingleStageMaster> CustomStageList= new();
}

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static List<StageMaster> StageMasterList = new List<StageMaster>();
    public static List<StageMaster> OverrideMasterList = new List<StageMaster>();
    public static List<SingleStageMaster> CustomStageList = new List<SingleStageMaster>(); // カスタムステージを入れておくリスト
    // デフォルトステージの最大レベル
    public const int MaxDefaultLevelId = 3;
    // カスタムステージの最小レベルID
    public const int MinCustomLevelId = 100;

    public static async UniTask GetAllMasterData()
	{
        var getTitleDataTask = PlayFabController.GetTitleData();
        var getPlayerDataTask = PlayFabController.GetPlayerData();
        var result = await UniTask.WhenAll(getTitleDataTask, getPlayerDataTask);
        if (result.Item1 != null)
        {
            SetMasterData(result.Item1.SettingeMaster, result.Item1.StageMasterList);
        }
        if (result.Item2 != null)
        {
            SetPlayerData(result.Item2.ClearStateList, result.Item2.OverrideStageMasterList, result.Item2.CustomStageList);
        }
	}
    public static void SetMasterData(SettingMaster settingMaster, List<StageMaster> stageMasterList)
	{
		SettingMaster = settingMaster;
        StageMasterList.AddRange(stageMasterList);
	}
    public static void SetPlayerData(List<ClearState> clearStateList, List<StageMaster> overrideMasterList, List<SingleStageMaster> customStageList)
    {
        SaveDataManager.ClearStateList = clearStateList;
        OverrideMasterList.AddRange(overrideMasterList);
        CustomStageList = customStageList;
    }
    public static async UniTask UpdateOverrideStageMaster(string stageId, List<LevelInfo> levelInfoList, float bpm, float offset)
    {
        var targetMaster = StageMasterList.FirstOrDefault(s => s.StageId == stageId);
        targetMaster.StageHeader.BPM = bpm;
        targetMaster.StageHeader.NoteTimeOffset = offset;
        foreach (var levelInfo in levelInfoList)
        {
            var level = levelInfo.Level;
            var notes = levelInfo.Notes.Select(n => new NoteMaster
            {
                lpb = n.lpb,
                num = n.num,
                block = n.block,
                type = n.type,
                notes = n.notes.Select(nn => new NoteMaster
                {
                    lpb = nn.lpb,
                    num = nn.num,
                    block = nn.block,
                    type = nn.type,
                }).ToList()
            }).ToList();
            if (targetMaster.notes.Count >= level)
            {
                targetMaster.notes[level - 1] = notes;
            }
        }
        await PlayFabController.UpdateOverrideScore(targetMaster);
        SaveDataManager.DeleteClearState(stageId, levelInfoList.Select(l => l.Level).ToList());
        SetOverrideMaster(targetMaster);
    }
    public static async UniTask UpdateStageMaster(string stageId, List<LevelInfo> levelInfoList)
    {
        foreach (var levelInfo in levelInfoList)
        {
            var level = levelInfo.Level;
            var notes = levelInfo.Notes.Select(n => new NoteMaster
            {
                lpb = n.lpb,
                num = n.num,
                block = n.block,
                type = n.type,
                notes = n.notes.Select(nn => new NoteMaster
                {
                    lpb = nn.lpb,
                    num = nn.num,
                    block = nn.block,
                    type = nn.type,
                }).ToList()
            }).ToList();
            var targetCustomStage = CustomStageList.FirstOrDefault(s => s.StageId == stageId && s.LevelId == level);
            if (targetCustomStage != null)
            {
                targetCustomStage.notes = notes;
            }
            else
            {
                CustomStageList.Add(new SingleStageMaster
                {
                    StageId = stageId,
                    LevelId = level,
                    StageNameOverride = levelInfo.StageNameOverride,
                    notes = notes
                });
            }
        }
        await PlayFabController.UpdateCustomStageList(CustomStageList);
        SaveDataManager.DeleteClearState(stageId, levelInfoList.Select(l => l.Level).ToList());
    }
    public static void SetOverrideMaster(StageMaster master)
    {
        OverrideMasterList.RemoveAll(s => s.StageId == master.StageId);
        OverrideMasterList.Add(master);
    }
    public static StageMaster GetOverrideMaster(string stageId)
    {
        return OverrideMasterList.FirstOrDefault(s => s.StageId == stageId);
    }
    public static int GetNextCustumStageLevel(string stageId)
    {
        var customStageList = CustomStageList.Where(s => s.StageId == stageId).ToList();
        if (customStageList.Count == 0)
        {
            return MinCustomLevelId;
        }
        return customStageList.Max(s => s.LevelId) + 1;
    }
    public static async UniTask DeleteCustomStage(string stageId, int level)
    {
        CustomStageList.RemoveAll(s => s.StageId == stageId && s.LevelId == level);
        await PlayFabController.UpdateCustomStageList(CustomStageList);
        SaveDataManager.DeleteClearState(stageId, new List<int>(){level});
    }
}
