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


    public void Init(SingleStageMaster stageMaster, bool isNewCreate)
    {
        _model = new ScoreMakerModel();
        _model.Init(stageMaster);

        _view.Init(stageMaster.StageHeader, _model.CurrentMaster.Notes);
        _view.ClickPracticeButton.Subscribe(async timeRate =>
        {
            _model.UpdateCurrentLevelNotes(_view.CreateNoteList());
            var sceneInfo = new BattleSceneInfo
            {
                StageMaster = _model.CurrentMaster,
                TimeRate = timeRate,
                IsPractice = true,
                IsAdditional = true
            };
            await GameManager.instance.OpenAdditionalScene(SceneType.Battle, sceneInfo);
        }).AddTo(this);
        _view.ClickSaveButton.Subscribe(_ =>
        {
            _view.DisplaySaveDialog(_model.OriginalStageInfo.StageHeader.StageName, isNewCreate);
        }).AddTo(this);
        _view.OnSave.Subscribe(async x =>
        {
            // 現在のレベルの譜面を保存
            await SaveScore(x.Item1, x.Item2);
        }).AddTo(this);
        _view.ClickDuplicateButton.Subscribe(_ =>
        {
            _view.DisplayDuplicateDialog(_model.OriginalStageInfo);
        }).AddTo(this);
    }

    private async UniTask SaveScore(string stageName, MusicParameter parameter)
    {
        // 現在のレベルの譜面を保存
        await _model.SaveScore(_view.CreateNoteList(), stageName, parameter);
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
