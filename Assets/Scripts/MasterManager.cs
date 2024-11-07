using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingMaster
{
    public float CriticalTimeBuffer;
    public float HitTimeBuffer;
    public float BallTimeOffset;
    public float BallSpeed;
    public float TestNoteTimeBuffer;
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
}

public static class MasterManager
{
    public static SettingMaster SettingMaster;
    public static  List<StageMaster> StageMasterList = new List<StageMaster>();
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
}
