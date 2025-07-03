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

    public void Init(int lastLevel)
    {
        _model = new HomeModel();
        _model.Init();

        _view.Init(_model.StageList, lastLevel);
        _view.ClickPlayStageButton.Subscribe(x =>
        {
            StartBattle(x.stageKey.StageId, x.stageKey.Level, isPractice: x.isPractice);
        }).AddTo(this);
        _view.ClickEditStageButton.Subscribe(x =>
        {
            StartScoreMaker(x.StageId, x.Level, isNewCreate: false);
        }).AddTo(this);
        _view.ClickNewCreateStageButton.Subscribe(musicId =>
        {
            StartScoreMaker(musicId, MasterManager.GetNextStageId(musicId), isNewCreate: true);
        }).AddTo(this);
    }

    private void StartBattle(string stageId, int level, bool isPractice)
    {
        var stageInfo = _model.GetStageInfo(stageId);
        var sceneInfo = new BattleSceneInfo
        {
            StageHeader = stageInfo.StageHeader,
            LevelInfo = stageInfo.LevelList.FirstOrDefault(l => l.Level == level),
            IsPractice = isPractice
        };
        GameManager.instance.OpenScene(SceneType.Battle, sceneInfo).Forget();
    }

    private void StartScoreMaker(string musicId, int stageId, bool isNewCreate)
    {
        var sceneInfo = new ScoreMakerSceneInfo
        {
            StageInfo = _model.GetStageInfo(musicId),
            TargetLevel = stageId,
            IsNewCreate = isNewCreate,
            IsDevelopOverride = stageId <= MasterManager.MaxDefaultLevelId,
        };
        GameManager.instance.OpenScene(SceneType.ScoreMaker, sceneInfo).Forget();
    }
}
