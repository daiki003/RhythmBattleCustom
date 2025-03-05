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
            StartBattle(x.StageId, x.Level, isPractice: false);
        }).AddTo(this);
        _view.ClickPracticeStageButton.Subscribe(x =>
        {
            StartBattle(x.StageId, x.Level, isPractice: true);
        }).AddTo(this);
        _view.ClickEditStageButton.Subscribe(x =>
        {
            StartScoreMaker(x.StageId, x.Level, isNewCreate: false);
        }).AddTo(this);
        _view.ClickNewCreateStageButton.Subscribe(stageId =>
        {
            StartScoreMaker(stageId, MasterManager.GetNextCustumStageLevel(stageId), isNewCreate: true);
        }).AddTo(this);
    }

    private void StartBattle(string stageId, int level, bool isPractice)
    {
        var sceneInfo = new BattleSceneInfo
        {
            StageInfo = _model.GetStageInfo(stageId),
            Level = level,
            IsPractice = isPractice
        };
        GameManager.instance.OpenScene(SceneType.Battle, sceneInfo).Forget();
    }

    private void StartScoreMaker(string stageId, int level, bool isNewCreate)
    {
        var sceneInfo = new ScoreMakerSceneInfo
        {
            StageInfo = _model.GetStageInfo(stageId),
            FirstLevel = level,
            IsNewCreate = isNewCreate,
            LevelList = level >= MasterManager.MinCustomLevelId ? new List<int>(){ level } : new List<int>(){ 1, 2, 3 }
        };
        GameManager.instance.OpenScene(SceneType.ScoreMaker, sceneInfo).Forget();
    }
}
