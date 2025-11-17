using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    public string MusicId;
    public string StageName;
    public int PanelType;
    public float StripStartTime;
    public float StripEndTime;
    public float BPM;
    public int LPB;
    public float StartTime;
    public float EndTime;
    public int BeatsNumber;
    public List<int> ModulationList = new();
    public float AdditionalBallSpeed;

    public StageHeader CreateCopy()
    {
        return new StageHeader
        {
            MusicId = MusicId,
            StageName = StageName,
            StripStartTime = StripStartTime,
            StripEndTime = StripEndTime,
            BPM = BPM,
            LPB = LPB,
            StartTime = StartTime,
            EndTime = EndTime,
            BeatsNumber = BeatsNumber,
            ModulationList = ModulationList.ToList(),
            AdditionalBallSpeed = AdditionalBallSpeed
        };
    }
}

public class StageMaster
{
    public StageHeader StageHeader;
    public List<List<NoteMaster>> notes = new List<List<NoteMaster>>();
}

public class SingleStageMaster
{
    public StageHeader StageHeader;
    public string MusicId => StageHeader.MusicId;
    public int StageId;
    public List<NoteMaster> Notes = new List<NoteMaster>();

    public SingleStageMaster CreateCopy()
    {
        return new SingleStageMaster
        {
            StageHeader = StageHeader.CreateCopy(),
            StageId = StageId,
            Notes = Notes.Select(n => new NoteMaster
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
            }).ToList()
        };
    }
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
}

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static List<SingleStageMaster> SampleStageList = new List<SingleStageMaster>();
    public static List<SingleStageMaster> CustomStageList = new List<SingleStageMaster>();
    // 最小ステージID
    public const int MinStageId = 1;

    public static async UniTask GetAllMasterData()
	{
        var getTitleDataTask = PlayFabController.GetTitleData();
        var getPlayerDataTask = PlayFabController.GetPlayerData();
        var getCustomStageTask = PlayFabController.LoadCustomStageListAsync<List<SingleStageMaster>>();
        var result = await UniTask.WhenAll(getTitleDataTask, getPlayerDataTask, getCustomStageTask);
        if (result.Item1 != null)
        {
            SetMasterData(result.Item1.SettingeMaster, result.Item1.StageMasterList);
        }
        if (result.Item2 != null)
        {
            SetPlayerData(result.Item2.ClearStateList);
        }
        if (result.Item3 != null)
        {
            CustomStageList.AddRange(result.Item3);
        }
	}
    public static void SetMasterData(SettingMaster settingMaster, List<StageMaster> stageMasterList)
	{
		SettingMaster = settingMaster;
        foreach (var stage in stageMasterList)
        {
            var header = stage.StageHeader.CreateCopy();
            for (int i = 0; i < stage.notes.Count; i++)
            {
                var levelNotes = stage.notes[i];
                var singleMaster = new SingleStageMaster
                {
                    StageHeader = header.CreateCopy(),
                    StageId = i + 1,
                    Notes = levelNotes,
                };
                singleMaster.StageHeader.PanelType = i + 1;
                singleMaster.Notes = levelNotes;
                SampleStageList.Add(singleMaster);
            }
        }
	}
    public static void SetPlayerData(List<ClearState> clearStateList)
    {
        SaveDataManager.ClearStateList = clearStateList;
    }
    public static async UniTask UpdateStageMaster(SingleStageMaster stageMaster)
    {
        int index = CustomStageList.FindIndex(s => s.MusicId == stageMaster.StageHeader.MusicId && s.StageId == stageMaster.StageId);
        if (index >= 0)
        {
            CustomStageList[index] = stageMaster;
        }
        else
        {
            CustomStageList.Add(stageMaster);
        }
        await PlayFabController.UpdateCustomStageList(CustomStageList);
        SaveDataManager.DeleteClearState(stageMaster.StageHeader.MusicId, stageMaster.StageId);
    }
    public static int GetNextStageId(string musicId)
    {
        var customStageList = CustomStageList.Where(s => s.MusicId == musicId).ToList();
        if (customStageList.Count == 0)
        {
            return MinStageId;
        }
        return customStageList.Max(s => s.StageId) + 1;
    }
    public static async UniTask DeleteCustomStage(string stageId, int level)
    {
        CustomStageList.RemoveAll(s => s.MusicId == stageId && s.StageId == level);
        await PlayFabController.UpdateCustomStageList(CustomStageList);
        SaveDataManager.DeleteClearState(stageId, level);
    }
}
