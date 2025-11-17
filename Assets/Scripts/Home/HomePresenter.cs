using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Linq;
using Unity.Collections;

public class HomePresenter : MonoBehaviour
{
    [SerializeField] private  HomeView _view;
    private HomeModel _model;

    public void Init()
    {
        _model = new HomeModel();
        _model.Init();

        _view.Init(_model.StageMasterList);
        _view.ClickPlayStageButton.Subscribe(x =>
        {
            StartBattle(x.stageKey, isPractice: x.isPractice);
        }).AddTo(this);
        _view.ClickEditStageButton.Subscribe(x =>
        {
            StartScoreMaker(x.stageKey.MusicId, x.stageKey.StageId, isNewCreate: x.isNewCreate);
        }).AddTo(this);
        _view.ClickNewCreateStageButton.Subscribe(musicId =>
        {
            StartScoreMaker(musicId, MasterManager.GetNextStageId(musicId), isNewCreate: true);
        }).AddTo(this);
    }

    private void StartBattle(GetStageKey getStageKey, bool isPractice)
    {
        var stageMaster = _model.GetStageInfo(getStageKey.MusicId, getStageKey.StageId, getStageKey.PanelType);
        var sceneInfo = new BattleSceneInfo
        {
            StageMaster = stageMaster,
            IsPractice = isPractice
        };
        GameManager.instance.OpenScene(SceneType.Battle, sceneInfo).Forget();
    }

    private void StartScoreMaker(string musicId, int stageId, bool isNewCreate)
    {
        var sceneInfo = new ScoreMakerSceneInfo
        {
            StageMaster = _model.GetStageInfo(musicId, stageId, HomePanelType.Custom),
            TargetLevel = stageId,
            IsNewCreate = isNewCreate,
        };
        GameManager.instance.OpenScene(SceneType.ScoreMaker, sceneInfo).Forget();
    }
}
