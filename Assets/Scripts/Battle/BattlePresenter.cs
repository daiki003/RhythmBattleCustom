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

    public void Init(BattleSceneInfo battleSceneInfo)
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
                GameManager.instance.OpenScene(SceneType.Home, new TitleSceneInfo()).Forget();
            }
        }).AddTo(this);
        _battleView.OnWhenFinishBattle.Subscribe(score =>
        {
            FinishBattle(score);
        }).AddTo(this);
        _battleView.Init(_battleSceneInfo.StageInfo.StageHeader, _battleSceneInfo.LevelInfo, _battleSceneInfo.IsPractice);
        _battleView.PrepareBattle();
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
        var clearState = _model.CalculateScore(score, _battleSceneInfo.StageInfo.StageHeader.StageId, _battleSceneInfo.Level);
        _battleView.StartResultAsync(clearState, SaveDataManager.GetClearState(_battleSceneInfo.StageInfo.StageHeader.StageId, _battleSceneInfo.Level), _model.CriticalMultiple, _model.HitMultiple, _model.MissMultiple).Forget();
    }
}
