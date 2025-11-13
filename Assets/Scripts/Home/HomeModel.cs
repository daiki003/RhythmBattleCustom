using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class LevelInfo
{
    public int Level;
    public List<NoteMaster> Notes = new();
}

public class StageInfo
{
    public StageHeader StageHeader;
    public List<LevelInfo> LevelList = new();
}

public class HomeModel
{
    private List<SingleStageMaster> _stageMasterList = new();
    public List<SingleStageMaster> StageMasterList => _stageMasterList;

    private const int _minStageId = 1;

    public void Init()
    {
        CreateStageList();
    }

    private void CreateStageList()
    {
        _stageMasterList = MasterManager.StageMasterList;
    }

    private LevelInfo CreateLevelInfo(SingleStageMaster singleStageMaster)
    {
        return new LevelInfo
        {
            Level = singleStageMaster.StageId,
            Notes = new List<NoteMaster>(singleStageMaster.Notes),
        };
    }

    public SingleStageMaster GetStageInfo(string musicId, int stageId)
    {
        var stageMaster = _stageMasterList.FirstOrDefault(s => s.MusicId == musicId && s.StageId == stageId);
        if (stageMaster == null)
        {
            stageMaster = new SingleStageMaster
            {
                StageHeader = new StageHeader
                {
                    MusicId = musicId,
                    StageName = "",
                    BPM = 100,
                    LPB = 4,
                    StripStartTime = 0f,
                    StripEndTime = 20f,
                    EndTime = 100f,
                    BeatsNumber = 4
                },
                StageId = GetNextStageId(musicId),
                Notes = new List<NoteMaster>(),
            };
        }
        return stageMaster;
    }

    private int GetNextStageId(string musicId)
    {
        var stageList = _stageMasterList.Where(s => s.MusicId == musicId);
        if (stageList.Count() == 0)
        {
            return _minStageId;
        }
        return stageList.Max(s => s.StageId) + 1;
    }
}
