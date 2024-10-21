using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingMaster
{
    public float NoteTimeOffset;
    public float CriticalTimeBuffer;
    public float HitTimeBuffer;
    public float BallTimeOffset;
    public float BallSpeed;
    public float BPM;
    public float TestNoteTimeBuffer;
    public List<List<NoteMaster>> notes = new List<List<NoteMaster>>();
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
    public int StageId;
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

    public static void SetMasterData(SettingMaster settingMaster, StageMaster battleRoseMoon)
	{
		SettingMaster = settingMaster;
        StageMasterList.Add(battleRoseMoon);
        PlayFabController.GetPlayerData();
        FinishGetMaster = true;
	}
}
