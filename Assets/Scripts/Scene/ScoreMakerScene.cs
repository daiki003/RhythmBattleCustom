using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ScoreMakerScene : SceneBase
{
    [SerializeField] private ScoreMaker _scoreMaker;
    private ScoreMakerSceneInfo _scoreMakerSceneInfo;
    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo);
        _scoreMakerSceneInfo = _nextSceneInfo as ScoreMakerSceneInfo;
        if (_scoreMakerSceneInfo == null)
        {
            return;
        }
        BGMManager.instance.SetClip(_scoreMakerSceneInfo.StageId, immediatePlay: false);
        _scoreMaker.Init(_scoreMakerSceneInfo.FirstLevel);
        await UniTask.CompletedTask;
    }

    public override void StartScene()
    {
        _scoreMaker.StartMake(_scoreMakerSceneInfo.StageId);
    }

    public override void Restart()
    {
        _scoreMaker.RestartMake();
    }
}
