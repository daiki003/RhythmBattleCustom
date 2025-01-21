using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SettingMaster
{
    public float CriticalTimeBuffer;
    public float HitTimeBuffer;
    public float BallTimeOffset;
    public float BallSpeed;
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

public class StageMaster
{
    public string StageId;
    public float BPM;
    public int LPB;
    public float NoteTimeOffset;
    public List<List<NoteMaster>> notes = new List<List<NoteMaster>>();
    public StageMaster CreateCopy()
    {
        var noteList = new List<List<NoteMaster>>();
        foreach (var note in notes)
        {
            noteList.Add(new List<NoteMaster>(note));
        }
        noteList.Add(new List<NoteMaster>(notes[0]));
        return new StageMaster()
        {
            StageId = StageId,
            BPM = BPM,
            LPB = LPB,
            NoteTimeOffset = NoteTimeOffset,
            notes = noteList
        };
    }
}

public class SingleStageMaster
{
    public string StageId;
    public string StageName;
    public int LevelId;
    public float BPM;
    public int LPB;
    public float NoteTimeOffset;
    public List<NoteMaster> notes = new List<NoteMaster>();
    // StageMasterからSingleStageMasterを作成する
    public SingleStageMaster() { }
    public SingleStageMaster(StageMaster stageMaster, int level)
    {
        StageId = stageMaster.StageId;
        LevelId = level;
        BPM = stageMaster.BPM;
        LPB = stageMaster.LPB;
        NoteTimeOffset = stageMaster.NoteTimeOffset;
        notes = stageMaster.notes[level];
    }
    public SingleStageMaster CreateCopy()
    {
        return new SingleStageMaster()
        {
            StageId = StageId,
            StageName = StageName,
            BPM = BPM,
            LPB = LPB,
            NoteTimeOffset = NoteTimeOffset,
            notes = notes
        };
    }
}

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static List<StageMaster> StageMasterList = new List<StageMaster>();
    public static List<StageMaster> OverrideMasterList = new List<StageMaster>();
    public static List<SingleStageMaster> SingleStageList = new List<SingleStageMaster>(); // 全てのステージを入れておくリスト
    public static List<SingleStageMaster> CustomStageList = new List<SingleStageMaster>(); // カスタムステージを入れておくリスト
    public static bool FinishGetMaster;
    // カスタムステージの最小レベルID
    private static int _minCustomLevelId = 100;

    public static void GetAllMasterData()
	{
		PlayFabController.GetTitleData(SetMasterData);
	}
    public static void SetMasterData(SettingMaster settingMaster, List<StageMaster> stageMasterList)
	{
		SettingMaster = settingMaster;
        StageMasterList.AddRange(stageMasterList);
        foreach (var stage in StageMasterList)
        {
            for (int i = 0; i < stage.notes.Count; i++)
            {
                SingleStageList.Add(new SingleStageMaster(stage, i));
            }
        }
        PlayFabController.GetPlayerData();
	}
    public static void SetOverrideMaster(StageMaster master)
    {
        OverrideMasterList.RemoveAll(s => s.StageId == master.StageId);
        OverrideMasterList.Add(master);
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
            return _minCustomLevelId;
        }
        return customStageList.Max(s => s.LevelId) + 1;
    }
}
