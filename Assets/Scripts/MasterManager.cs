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
    public List<SingleStageMaster> CustomStageList= new();
}

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static List<StageHeader> SampleStageHeaderList = new List<StageHeader>();
    public static List<SingleStageMaster> StageMasterList = new List<SingleStageMaster>(); // カスタムステージを入れておくリスト
    // デフォルトステージの最大レベル
    public const int MaxDefaultLevelId = 3;
    // 最小ステージID
    public const int MinStageId = 1;

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
            SetPlayerData(result.Item2.ClearStateList, result.Item2.CustomStageList);
        }
	}
    public static void SetMasterData(SettingMaster settingMaster, List<StageMaster> stageMasterList)
	{
		SettingMaster = settingMaster;
        foreach (var stage in stageMasterList)
        {
            var header = stage.StageHeader.CreateCopy();
            SampleStageHeaderList.Add(header);
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
                StageMasterList.Add(singleMaster);
            }
        }
	}
    public static void SetPlayerData(List<ClearState> clearStateList, List<SingleStageMaster> customStageList)
    {
        SaveDataManager.ClearStateList = clearStateList;
        StageMasterList.AddRange(customStageList);
    }
    public static async UniTask UpdateStageMaster(SingleStageMaster stageMaster)
    {
        int index = StageMasterList.FindIndex(s => s.MusicId == stageMaster.StageHeader.MusicId && s.StageId == stageMaster.StageId);
        if (index >= 0)
        {
            StageMasterList[index] = stageMaster;
        }
        else
        {
            StageMasterList.Add(stageMaster);
        }
        await PlayFabController.UpdateCustomStageList(StageMasterList);
        SaveDataManager.DeleteClearState(stageMaster.StageHeader.MusicId, stageMaster.StageId);
    }
    public static int GetNextStageId(string musicId)
    {
        var customStageList = StageMasterList.Where(s => s.MusicId == musicId).ToList();
        if (customStageList.Count == 0)
        {
            return MinStageId;
        }
        return customStageList.Max(s => s.StageId) + 1;
    }
    public static async UniTask DeleteCustomStage(string stageId, int level)
    {
        StageMasterList.RemoveAll(s => s.MusicId == stageId && s.StageId == level);
        await PlayFabController.UpdateCustomStageList(StageMasterList);
        SaveDataManager.DeleteClearState(stageId, level);
    }
}
