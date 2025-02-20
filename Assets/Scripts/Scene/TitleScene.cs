using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TitleScene : SceneBase
{
    protected override string _prefabPath => "Prefabs/TitlePanel";
    [SerializeField] private TitleManager _titleManager;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo);
        int lastBattleLevel = 0;
        if (_lastSceneInfo is BattleSceneInfo battleSceneInfo)
        {
            lastBattleLevel = battleSceneInfo.StageMaster.LevelId;
        }
        _titleManager.Init(lastBattleLevel);
    }
}
