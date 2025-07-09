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

        _view.Init(_model.StageMasterList, 0);
        _view.ClickPlayStageButton.Subscribe(x =>
        {
            StartBattle(x.stageKey.MusicId, x.stageKey.StageId, isPractice: x.isPractice);
        }).AddTo(this);
        _view.ClickEditStageButton.Subscribe(x =>
        {
            StartScoreMaker(x.MusicId, x.StageId, isNewCreate: false);
        }).AddTo(this);
        _view.ClickNewCreateStageButton.Subscribe(musicId =>
        {
            StartScoreMaker(musicId, MasterManager.GetNextStageId(musicId), isNewCreate: true);
        }).AddTo(this);
    }

    private void StartBattle(string musicId, int stageId, bool isPractice)
    {
        var stageMaster = _model.GetStageInfo(musicId, stageId);
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
            StageMaster = _model.GetStageInfo(musicId, stageId),
            TargetLevel = stageId,
            IsNewCreate = isNewCreate,
        };
        GameManager.instance.OpenScene(SceneType.ScoreMaker, sceneInfo).Forget();
    }
}
