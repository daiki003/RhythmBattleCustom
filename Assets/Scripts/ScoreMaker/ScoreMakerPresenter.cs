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
        _model.OnChangeLineNumber.Subscribe(lineNumber =>
        {
            _view.AdjustmentLineNumber(lineNumber);
        }).AddTo(this);
        _model.OnChangeParameter.Subscribe(parameter =>
        {
            _view.SetHeaderParameter(parameter);
        }).AddTo(this);

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
        _view.OnSave.Subscribe(async name =>
        {
            // 現在のレベルの譜面を保存
            await SaveScore(name);
        }).AddTo(this);
        _view.OnChangeParameter.Subscribe(param =>
        {
            _model.ChangeHeaderParameter(param, BGMManager.instance.Length);
        });

        stageMaster.StageHeader.EndTime = stageMaster.StageHeader.EndTime > 0 ? stageMaster.StageHeader.EndTime : BGMManager.instance.Length;
        _model.Init(stageMaster);
        _view.Init(_model.CurrentMaster.Notes, _model.LineNumber);
        _view.SetHeaderParameter(_model.CurrentMaster.StageHeader);
    }

    private async UniTask SaveScore(string stageName)
    {
        // 現在のレベルの譜面を保存
        await _model.SaveScore(_view.CreateNoteList(), stageName, _view.GetTimeJumpDict());
        _view.DisplaySaveFinishDialog();
    }

    public void StartMake()
    {
        _view.StartMake();
    }
}
