using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class LevelInfo
{
    public int Level;
    public string StageNameOverride;
    public List<NoteMaster> Notes = new();
}

public class StageInfo
{
    public StageHeader StageHeader;
    public List<LevelInfo> LevelList = new();
    public bool HasCustomStage()
    {
        return LevelList.Any(l => l.Level > MasterManager.MaxDefaultLevelId);
    }
}

public class HomeModel
{
    private List<StageInfo> _stageList = new();
    public List<StageInfo> StageList => _stageList;

    public void Init()
    {
        CreateStageList();
    }

    private void CreateStageList()
    {
        var customStageList = MasterManager.CustomStageList;
        foreach (var master in MasterManager.StageMasterList)
        {
            var stageMaster = MasterManager.GetOverrideMaster(master.StageId) ?? master;
            var stageInfo = new StageInfo();
            stageInfo.StageHeader = stageMaster.StageHeader.CreateCopy();
            // 通常の3レベル分を追加
            for (int i = 0; i < stageMaster.notes.Count; i++)
            {
                var levelInfo = new LevelInfo
                {
                    Level = i + 1,
                    Notes = new List<NoteMaster>(stageMaster.notes[i])
                };
                stageInfo.LevelList.Add(levelInfo);
            }
            // カスタムステージ分を追加
            stageInfo.LevelList.AddRange(customStageList.Where(c => c.StageId == stageInfo.StageHeader.StageId).Select(c => CreateLevelInfo(c)));
            _stageList.Add(stageInfo);
        }
    }

    private LevelInfo CreateLevelInfo(SingleStageMaster singleStageMaster)
    {
        return new LevelInfo
        {
            Level = singleStageMaster.LevelId,
            StageNameOverride = singleStageMaster.StageNameOverride,
            Notes = new List<NoteMaster>(singleStageMaster.notes),
        };
    }

    public StageInfo GetStageInfo(string stageId)
    {
        return _stageList.FirstOrDefault(s => s.StageHeader.StageId == stageId);
    }
}
