using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab.ClientModels;
using System.Linq;

public class ScoreMakerModel
{
    private List<LevelInfo> _levelInfoList = new();
    public List<LevelInfo> LevelInfoList => _levelInfoList;
    public StageInfo OriginalStageInfo { get; private set; }

    public void Init(StageInfo stageInfo, int targetLevel, bool isDevelopOverride)
    {
        OriginalStageInfo = stageInfo;
        if (isDevelopOverride)
        {
            // 開発用編集の場合は1～3のステージを追加
            for (int i = 1; i <= MasterManager.MaxDefaultLevelId; i++)
            {
                _levelInfoList.Add(new LevelInfo
                {
                    Level = i,
                    Notes = new List<NoteMaster>(stageInfo.LevelList.FirstOrDefault(l => l.Level == i)?.Notes)
                });
            }
        }
        else
        {
            var notes = stageInfo.LevelList.FirstOrDefault(l => l.Level == targetLevel)?.Notes;
            if (notes != null)
            {
                _levelInfoList.Add(new LevelInfo
                {
                    Level = targetLevel,
                    Notes = new List<NoteMaster>(notes)
                });
            }
        }
    }

    // 開発で通常ステージを更新するとき用
    public async UniTask OverrideScore(List<NoteMaster> notes, int level, float bpm, float offset)
    {
        UpdateCurrentLevelNotes(notes, level);
        await MasterManager.UpdateOverrideStageMaster(OriginalStageInfo.StageHeader.StageId, _levelInfoList, bpm, offset);
    }

    public async UniTask SaveScore(List<NoteMaster> notes, int level, string overrideName)
    {
        // 現在のレベルの譜面を保存
        UpdateCurrentLevelNotes(notes, level, overrideName);
        await MasterManager.UpdateStageMaster(OriginalStageInfo.StageHeader.StageId, _levelInfoList);
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
