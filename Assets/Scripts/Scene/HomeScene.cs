using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class HomeScene : SceneBase
{
    [SerializeField] private HomeManager _titleManager;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo);
        int lastBattleLevel = 1;
        if (_lastSceneInfo is BattleSceneInfo battleSceneInfo)
        {
            lastBattleLevel = battleSceneInfo.StageMaster.LevelId;
        }
        _titleManager.Init(lastBattleLevel);
    }
}
