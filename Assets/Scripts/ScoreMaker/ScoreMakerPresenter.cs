using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;
using Unity.VisualScripting;
using Cysharp.Threading.Tasks;

public class ScoreMakerPresenter : MonoBehaviour
{
    [SerializeField] private ScoreMakerView _view;
    private ScoreMakerModel _model;

    private LevelInfo _currentLevelInfo => _model.LevelInfoList?.FirstOrDefault(l => l.Level == _currentLevel);
    private int _currentLevel;
    private bool _isDevelopOverride;

    public void Init(StageInfo stageInfo, int targetLevel, bool isNewCreate, bool isDevelopOverride)
    {
        _currentLevel = targetLevel;
        _isDevelopOverride = isDevelopOverride;
        _model = new ScoreMakerModel();
        _model.Init(stageInfo, targetLevel, isDevelopOverride);

        _view.Init(stageInfo.StageHeader, _currentLevelInfo?.Notes, targetLevel);
        _view.ClickPracticeButton.Subscribe(async timeRate =>
        {
            _model.UpdateCurrentLevelNotes(_view.CreateNoteList(), _currentLevel);
            var sceneInfo = new BattleSceneInfo
            {
                StageHeader = _model.OriginalStageInfo.StageHeader,
                LevelInfo = _currentLevelInfo,
                TimeRate = timeRate,
                IsPractice = true,
                IsAdditional = true
            };
            await GameManager.instance.OpenAdditionalScene(SceneType.Battle, sceneInfo);
        }).AddTo(this);
        _view.OnChangeLevel.Subscribe(x =>
        {
            // 現在のレベルの譜面を保存
            _model.UpdateCurrentLevelNotes(x.notes, _currentLevel);

            // レベル更新
            _currentLevel = x.level;
            BGMManager.instance.Pause();
            _view.CreateLine(_currentLevelInfo.Notes);
        }).AddTo(this);
        _view.ClickSaveButton.Subscribe(_ =>
        {
            _view.DisplaySaveDialog(_model.OriginalStageInfo.StageHeader.StageName, isNewCreate);
        }).AddTo(this);
        _view.OnSave.Subscribe(async stageName =>
        {
            // 現在のレベルの譜面を保存
            await SaveScore(stageName);
        }).AddTo(this);
        _view.ClickDuplicateButton.Subscribe(_ =>
        {
            _view.DisplayDuplicateDialog(_model.OriginalStageInfo);
        }).AddTo(this);
    }

    private async UniTask SaveScore(string overrideName = "")
    {
        // 現在のレベルの譜面を保存
        if (_isDevelopOverride)
        {
            await _model.OverrideScore(_view.CreateNoteList(), _currentLevel, _view.CurrentBpm, _view.CurrentOffset);
        }
        else
        {
            await _model.SaveScore(_view.CreateNoteList(), _currentLevel, overrideName);
        }
        _view.DisplaySaveFinishDialog();
    }

    public void StartMake(bool isRestart)
    {
        if (isRestart)
        {
            _view.RestartMake();
        }
        else
        {
            _view.StartMake();
        }        
    }
}
