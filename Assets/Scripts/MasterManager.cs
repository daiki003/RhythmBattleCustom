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
            NoteTimeOffset = NoteTimeOffset
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
		var titleDataResult = await PlayFabController.GetTitleData();
        var playerDataResult = await PlayFabController.GetPlayerData();
        if (titleDataResult != null)
        {
            SetMasterData(titleDataResult.SettingeMaster, titleDataResult.StageMasterList);
        }
        if (playerDataResult != null)
        {
            SetPlayerData(playerDataResult.ClearStateList, playerDataResult.SettingData, playerDataResult.OverrideStageMasterList, playerDataResult.CustomStageList);
        }
	}
    public static void SetMasterData(SettingMaster settingMaster, List<StageMaster> stageMasterList)
	{
		SettingMaster = settingMaster;
        StageMasterList.AddRange(stageMasterList);
	}
    public static void SetPlayerData(List<ClearState> clearStateList, SettingData settingData, List<StageMaster> overrideMasterList, List<SingleStageMaster> customStageList)
    {
        SaveDataManager.ClearStateList = clearStateList;
        SaveDataManager.SettingData = settingData;
        BGMManager.instance.AdjustVolume(settingData.BgmVolume);
        SEManager.instance.AdjustVolume(settingData.SeVolume);
        OverrideMasterList.AddRange(overrideMasterList);
        CustomStageList = customStageList;
    }
    public static async UniTask UpdateOverrideMaster(string stageId, List<LevelInfo> levelInfoList)
    {
        var targetMaster = StageMasterList.FirstOrDefault(s => s.StageId == stageId);
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
            if (levelInfo.Level <= MaxDefaultLevelId)
            {
                targetMaster.notes[level - 1] = notes;
            }
            else
            {
                var targetCustomStage = CustomStageList.FirstOrDefault(s => s.StageId == stageId && s.LevelId == level);
                if (targetCustomStage != null)
                {
                    targetCustomStage.notes = notes;
                }
                else
                {
                    CustomStageList.Add(new SingleStageMaster
                    {
                        StageId = targetMaster.StageHeader.StageId,
                        LevelId = level,
                        StageNameOverride = levelInfo.StageNameOverride,
                        notes = notes
                    });
                }
            }
        }
        await PlayFabController.UpdateOverrideScore(targetMaster);
        await PlayFabController.UpdateCustomStageList(CustomStageList);
        SetOverrideMaster(targetMaster);
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
}
