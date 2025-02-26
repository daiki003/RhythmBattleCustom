using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

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
}

public class StageMaster
{
    public string StageId;
    public StageHeader StageHeader;
    public List<List<NoteMaster>> notes = new List<List<NoteMaster>>();
    public StageMaster CreateCopy()
    {
        var noteList = new List<List<NoteMaster>>();
        foreach (var note in notes)
        {
            noteList.Add(new List<NoteMaster>(note));
        }
        return new StageMaster()
        {
            StageId = StageId,
            StageHeader = StageHeader,
            notes = noteList
        };
    }
    public StageMaster CreateEmpty()
    {
        return new StageMaster()
        {
            StageId = StageId,
            StageHeader = StageHeader,
            notes = new List<List<NoteMaster>>() { new() }
        };
    }
}

public class SingleStageMaster
{
    public string StageId;
    public StageHeader StageHeader;
    public int LevelId;
    public List<NoteMaster> notes = new List<NoteMaster>();
    // StageMasterからSingleStageMasterを作成する
    public SingleStageMaster() { }
    public SingleStageMaster(StageMaster stageMaster, int level)
    {
        StageId = stageMaster.StageId;
        StageHeader = stageMaster.StageHeader;
        LevelId = level;
        notes = stageMaster.notes[level - 1];
    }
    public SingleStageMaster CreateCopy()
    {
        return new SingleStageMaster()
        {
            StageId = StageId,
            StageHeader = StageHeader,
            notes = notes
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
    public List<StageMaster> OverrideStageMasterList = new();
    public List<SingleStageMaster> CustomStageList= new();
}

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static List<StageMaster> StageMasterList = new List<StageMaster>();
    public static List<StageMaster> OverrideMasterList = new List<StageMaster>();
    public static List<SingleStageMaster> SingleStageList = new List<SingleStageMaster>(); // 通常のステージを入れておくリスト
    public static List<SingleStageMaster> CustomStageList = new List<SingleStageMaster>(); // カスタムステージを入れておくリスト
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
        CreateSingleStageList();
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
    public static void CreateSingleStageList()
    {
        SingleStageList = new List<SingleStageMaster>();
        foreach (var stage in StageMasterList)
        {
            var stageMaster = GetStageMaster(stage.StageId);
            for (int i = 0; i < stage.notes.Count; i++)
            {
                SingleStageList.Add(new SingleStageMaster(stageMaster, i + 1));
            }
        }
        SingleStageList.AddRange(CustomStageList);
    }
    public static void SetOverrideMaster(StageMaster master)
    {
        OverrideMasterList.RemoveAll(s => s.StageId == master.StageId);
        OverrideMasterList.Add(master);
        CreateSingleStageList();
    }
    public static void AddCustomStageList(SingleStageMaster master)
    {
        CustomStageList.Add(master);
        SingleStageList.Add(master);
    }
    public static StageMaster GetStageMaster(string stageId)
    {
        var overrideMaster = GetOverrideStageMaster(stageId);
        return overrideMaster ?? StageMasterList.FirstOrDefault(s => s.StageId == stageId);
    }
    public static StageMaster GetOverrideStageMaster(string stageId)
    {
        return OverrideMasterList.FirstOrDefault(s => s.StageId == stageId);
    }
    // stageIdとlevelIdからSingleStageMasterを取得する
    public static SingleStageMaster GetSingleStageMaster(string stageId, int levelId)
    {
        return SingleStageList.FirstOrDefault(s => s.StageId == stageId && s.LevelId == levelId);
    }
    public static SingleStageMaster GetCustomStageMaster(string stageId, int levelId)
    {
        return CustomStageList.FirstOrDefault(s => s.StageId == stageId && s.LevelId == levelId);
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
