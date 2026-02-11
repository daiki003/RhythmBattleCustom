using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class HomeModel
{
    private List<SingleStageMaster> _stageMasterList = new();
    public List<SingleStageMaster> StageMasterList => _stageMasterList;

    public void Init()
    {
        CreateStageList();
    }

    private void CreateStageList()
    {
        _stageMasterList = MasterManager.CustomStageList.Concat(MasterManager.SampleStageList).ToList();
    }

    public SingleStageMaster GetStageInfo(string musicId, int stageId, bool isMyMusic, bool isCopy)
    {
        var stageMaster = _stageMasterList.FirstOrDefault(s => s.MusicId == musicId && s.StageId == stageId);
        if (stageMaster == null || isCopy)
        {
            var stageHeader = new StageHeader
            {
                MusicId = musicId,
                StageName = "",
                PanelType = (int)HomePanelType.Custom,
                IsMyMusic = isMyMusic,
                BPM = 100,
                StripStartTime = 0f,
                StripEndTime = 20f,
                EndTime = 100f,
                BeatsNumber = 4,
            };
            if (!isMyMusic)
            {
                stageHeader = MasterManager.SampleStageList.FirstOrDefault(s => s.MusicId == musicId)?.StageHeader;
            }
            stageMaster = new SingleStageMaster
            {
                StageHeader = stageHeader,
                StageId = MasterManager.GetNextStageId(musicId),
                Notes = stageMaster != null ? stageMaster.Notes.ToList() : new List<NoteMaster>(),
            };
        }
        return stageMaster;
    }
}
