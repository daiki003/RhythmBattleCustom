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

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static List<StageMaster> StageMasterList = new List<StageMaster>();
    public static List<StageMaster> OverrideMasterList = new List<StageMaster>();
    public static bool FinishGetMaster;
    public static void GetAllMasterData()
	{
		PlayFabController.GetTitleData(SetMasterData);
	}
    public static void SetMasterData(SettingMaster settingMaster, List<StageMaster> stageMasterList)
	{
		SettingMaster = settingMaster;
        StageMasterList.AddRange(stageMasterList);
        PlayFabController.GetPlayerData();
	}
    public static void SetOverrideMaster(StageMaster master)
    {
        OverrideMasterList.RemoveAll(s => s.StageId == master.StageId);
        OverrideMasterList.Add(master);
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
}
