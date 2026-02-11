using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab.ClientModels;
using System.Linq;
using R3;
using UnityEngine;
using System;

public class ScoreMakerModel
{
    public SingleStageMaster _currentMaster = new();
    public SingleStageMaster CurrentMaster => _currentMaster;
    public SingleStageMaster OriginalStageInfo { get; private set; }

    private int _lineNumber => (int)(CurrentMaster.StageHeader.BPM * 4 * ((CurrentMaster.StageHeader.EndTime - CurrentMaster.StageHeader.StartTime) / 60f));
    public int LineNumber => _lineNumber;
    private Subject<int> _onChangeLineNumber = new();
    public Observable<int> OnChangeLineNumber => _onChangeLineNumber;
    private Subject<StageHeader> _onChangeParameter = new();
    public Observable<StageHeader> OnChangeParameter => _onChangeParameter;

    public void Init(SingleStageMaster stageMaster)
    {
        OriginalStageInfo = stageMaster;
        _currentMaster = stageMaster.CreateCopy();
    }

    // 線の数が変わるパラメータ変化があったらこれを呼ぶ
    private void ChangeParameterWithLine()
    {
        _onChangeLineNumber.OnNext(_lineNumber);
        _onChangeParameter.OnNext(_currentMaster.StageHeader);
    }

    public void ChangeHeaderParameter(ChangeParameter param, float bgmLength)
    {
        if (param.Bpm.HasValue)
        {
            _currentMaster.StageHeader.BPM = param.Bpm.Value;
        }
        if (param.StartTime.HasValue)
        {
            _currentMaster.StageHeader.StartTime = param.StartTime.Value;
        }
        if (param.EndTime.HasValue)
        {
            // 現在の曲の長さ以上にならないようにする
            var actualEndTime = Mathf.Min(param.EndTime.Value, bgmLength);
            _currentMaster.StageHeader.EndTime = actualEndTime;
        }
        if (param.BeatNumber.HasValue)
        {
            _currentMaster.StageHeader.BeatsNumber = param.BeatNumber.Value;
        }
        if (param.ResetModulation)
        {
            _currentMaster.StageHeader.ModulationDict.Clear();
        }
        else if (param.ModulationChange != null)
        {
            var (measure, diff) = param.ModulationChange.Value;
            var measureStr = measure.ToString();
            var modulationDict = _currentMaster.StageHeader.ModulationDict;
            var modulationNum = modulationDict.GetValueOrDefault(measureStr) + diff;
            // 1つ前の拍子と同じところまでは下げられない
            modulationNum = Math.Max(modulationNum, _currentMaster.StageHeader.BeatsNumber * -1);
            modulationDict[measureStr] = modulationNum;
        }
        ChangeParameterWithLine();
    }

    public async UniTask SaveScore(List<NoteMaster> notes, string stageName, Dictionary<int, float> timeJumpDict)
    {
        // 現在のレベルの譜面を保存
        UpdateCurrentLevelNotes(notes);
        if (!string.IsNullOrEmpty(stageName))
        {
            _currentMaster.StageHeader.StageName = stageName;
        }
        if (timeJumpDict != null)
        {
            var dict = new Dictionary<string, float>();
            foreach (var kv in timeJumpDict)
            {
                dict[kv.Key.ToString()] = kv.Value;
            }
            _currentMaster.StageHeader.TimeJumpDict = dict;
        }
        else
        {
            _currentMaster.StageHeader.TimeJumpDict = new Dictionary<string, float>();
        }
        await MasterManager.UpdateStageMaster(_currentMaster);
    }

    // 現在のレベルの譜面状況を更新
    public void UpdateCurrentLevelNotes(List<NoteMaster> notes)
    {
        _currentMaster.Notes = notes;
    }

    public SingleStageMaster GetTutorialStageMaster()
    {
        return new SingleStageMaster
        {
            StageHeader = new StageHeader
            {
                MusicId = "BattleAbysswalker",
                StartTime = 0f,
                EndTime = 60f,
                BPM = 120f,
                BeatsNumber = 4
            },
            Notes = new List<NoteMaster>()
        };
    }
}
