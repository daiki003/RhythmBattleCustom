using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BattleScene : SceneBase
{
    [SerializeField] private BattlePresenter _battlePresenter;
    private BattleSceneInfo _battleSceneInfo;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo);
        _battleSceneInfo = _nextSceneInfo as BattleSceneInfo;
        _battlePresenter.Init(_battleSceneInfo.StageMaster, _battleSceneInfo.IsPractice);
        if (!_battleSceneInfo.IsAdditional)
        {
            // 曲が始まる前にGC.Collect
            GC.Collect();
            await UniTask.WaitForSeconds(1f);
        }
    }

    public override void StartScene()
    {
        _battlePresenter.StartBattle(_battleSceneInfo.TimeRate, _battleSceneInfo.IsAdditional);
    }
}
