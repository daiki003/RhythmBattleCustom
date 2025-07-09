using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using System;
using System.Linq;

public class BattlePresenter : MonoBehaviour
{
    [SerializeField] private BattleView _battleView;
    private BattleModel _model;

    private BattleSceneInfo _battleSceneInfo;

    public async UniTask Init(BattleSceneInfo battleSceneInfo)
    {
        _model = new BattleModel();
        _battleSceneInfo = battleSceneInfo;
        _battleView.OnWhenClickedBack.Subscribe(_ =>
        {
            if (_battleSceneInfo.IsAdditional)
            {
                GameManager.instance.BackToMainScene().Forget();
            }
            else
            {
                GameManager.instance.OpenScene(SceneType.Home, new HomeSceneInfo()).Forget();
            }
        }).AddTo(this);
        _battleView.OnWhenFinishBattle.Subscribe(score =>
        {
            FinishBattle(score);
        }).AddTo(this);
        _battleView.Init(_battleSceneInfo.StageMaster.StageHeader, _battleSceneInfo.StageMaster.Notes, _battleSceneInfo.IsPractice, _battleSceneInfo.IsAdditional);
        await _battleView.PrepareBattle();
        if (_battleSceneInfo.IsAdditional)
        {
            _battleView.SetSliderForAdditional(_battleSceneInfo.TimeRate);
        }
    }

    public void StartBattle()
    {
        if (_battleSceneInfo.TimeRate > 0)
        {
            _battleView.BattleStartFromMiddle();
        }
        else
        {
            _battleView.BattleStart().Forget();
        }
    }

    public void FinishBattle(Score score)
    {
        string musicId = _battleSceneInfo.StageMaster.StageHeader.MusicId;
        int stageId = _battleSceneInfo.StageMaster.StageId;
        var highScoreClearState = SaveDataManager.GetClearState(musicId, stageId).CreateCopy();
        var clearState = _model.CalculateScore(score, musicId, stageId);
        _battleView.StartResultAsync(clearState, highScoreClearState, _model.CriticalMultiple, _model.HitMultiple, _model.MissMultiple).Forget();
    }
}
