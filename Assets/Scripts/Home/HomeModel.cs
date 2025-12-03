using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class HomeModel
{
    private List<SingleStageMaster> _customStageList = new();
    private List<SingleStageMaster> _sampleStageList = new();
    private List<SingleStageMaster> _stageMasterList = new();
    public List<SingleStageMaster> StageMasterList => _stageMasterList;

    private const int _minStageId = 1;

    public void Init()
    {
        CreateStageList();
    }

    private void CreateStageList()
    {
        _customStageList = MasterManager.CustomStageList;
        _sampleStageList = MasterManager.SampleStageList;
        _stageMasterList = MasterManager.CustomStageList.Concat(MasterManager.SampleStageList).ToList();
    }

    public SingleStageMaster GetStageInfo(string musicId, int stageId, HomePanelType panelType)
    {
        var stageMaster = _stageMasterList.FirstOrDefault(s => s.MusicId == musicId && s.StageId == stageId && s.StageHeader.PanelType == (int)panelType);
        if (stageMaster == null)
        {
            stageMaster = new SingleStageMaster
            {
                StageHeader = new StageHeader
                {
                    MusicId = musicId,
                    StageName = "",
                    PanelType = (int)panelType,
                    BPM = 100,
                    StripStartTime = 0f,
                    StripEndTime = 20f,
                    EndTime = 100f,
                    BeatsNumber = 4,
                },
                StageId = stageId,
                Notes = new List<NoteMaster>(),
            };
        }
        return stageMaster;
    }
}
