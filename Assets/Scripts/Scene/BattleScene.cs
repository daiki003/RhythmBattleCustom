using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BattleScene : SceneBase
{
    [SerializeField] private BattlePresenter _battlePresenter;
    private BattleSceneInfo _battleSceneInfo;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo, fadePanel);
        _battleSceneInfo = _nextSceneInfo as BattleSceneInfo;
        await _battlePresenter.Init(_battleSceneInfo);
        if (!_battleSceneInfo.IsAdditional)
        {
            // 曲が始まる前にGC.Collect
            GC.Collect();
            await UniTask.WaitForSeconds(1f);
        }
    }

    public override async UniTask StartSceneAsync()
    {
        await base.StartSceneAsync();
        _battlePresenter.StartBattle();
    }
}
