using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HomeScene : SceneBase
{
    [SerializeField] private HomeManager _titleManager;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo, fadePanel);
        int lastBattleLevel = 1;
        if (_lastSceneInfo is BattleSceneInfo battleSceneInfo)
        {
            lastBattleLevel = battleSceneInfo.Level;
        }
        _titleManager.Init(lastBattleLevel);
    }
}
