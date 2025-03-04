using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab.ClientModels;
using System.Linq;

public class ScoreMakerModel
{
    private List<LevelInfo> _levelInfoList = new();
    public List<LevelInfo> LevelInfoList => _levelInfoList;
    private StageInfo _currentStageInfo;
    public  StageInfo CurrentStageInfo => _currentStageInfo;

    public void Init(StageInfo stageInfo, List<int> levelList)
    {
        _currentStageInfo = stageInfo;
        foreach (var level in levelList)
        {
            _levelInfoList.Add(new LevelInfo
            {
                Level = level,
                Notes = stageInfo.LevelList.FirstOrDefault(l => l.Level == level)?.Notes
            });
        }
    }

    public async UniTask SaveScore(List<NoteMaster> notes, int level, string overrideName)
    {
        // 現在のレベルの譜面を保存
        UpdateCurrentLevelNotes(notes, level, overrideName);
        await MasterManager.UpdateOverrideMaster(_currentStageInfo.StageHeader.StageId, _levelInfoList);
    }

    // 現在のレベルの譜面状況を更新
    public void UpdateCurrentLevelNotes(List<NoteMaster> notes, int level, string overrideName = "")
    {
        var currentLevelInfo = _levelInfoList?.FirstOrDefault(l => l.Level == level);
        if (currentLevelInfo != null)
        {
            currentLevelInfo.StageNameOverride = overrideName;
            currentLevelInfo.Notes = notes;
        }
        else
        {
            _levelInfoList.Add(new LevelInfo
            {
                Level = level,
                StageNameOverride = overrideName,
                Notes = notes
            });
        }
    }
}
