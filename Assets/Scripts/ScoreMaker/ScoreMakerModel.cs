using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab.ClientModels;
using System.Linq;

public class ScoreMakerModel
{
    public SingleStageMaster _currentMaster = new();
    public SingleStageMaster CurrentMaster => _currentMaster;
    public SingleStageMaster OriginalStageInfo { get; private set; }

    public void Init(SingleStageMaster stageMaster)
    {
        OriginalStageInfo = stageMaster;
        _currentMaster = stageMaster.CreateCopy();
    }

    public async UniTask SaveScore(List<NoteMaster> notes, string stageName, MusicParameter parameter)
    {
        // 現在のレベルの譜面を保存
        UpdateCurrentLevelNotes(notes);
        if (!string.IsNullOrEmpty(stageName))
        {
            _currentMaster.StageHeader.StageName = stageName;
        }
        _currentMaster.StageHeader.BPM = parameter.Bpm;
        _currentMaster.StageHeader.StartTime = parameter.StartTime;
        _currentMaster.StageHeader.EndTime = parameter.EndTime;
        _currentMaster.StageHeader.BeatsNumber = parameter.BeatsNumber;
        _currentMaster.StageHeader.ModulationList = parameter.ModulationList.ToList();
        await MasterManager.UpdateStageMaster(_currentMaster);
    }

    // 現在のレベルの譜面状況を更新
    public void UpdateCurrentLevelNotes(List<NoteMaster> notes)
    {
        _currentMaster.Notes = notes;
    }
}
