using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerScene : SceneBase
{
    [SerializeField] private ScoreMaker _scoreMaker;
    private ScoreMakerSceneInfo _scoreMakerSceneInfo;
    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo, fadePanel);
        _scoreMakerSceneInfo = _nextSceneInfo as ScoreMakerSceneInfo;
        if (_scoreMakerSceneInfo == null)
        {
            return;
        }
        BGMManager.instance.SetClip(_scoreMakerSceneInfo.StageInfo.StageHeader.StageId, immediatePlay: false);
        _scoreMaker.Init(_scoreMakerSceneInfo.StageInfo, _scoreMakerSceneInfo.FirstLevel, _scoreMakerSceneInfo.IsNewCreate, _scoreMakerSceneInfo.LevelList);
        await UniTask.CompletedTask;
    }

    public override async UniTask StartSceneAsync()
    {
        await base.StartSceneAsync();
        _scoreMaker.StartMake();
    }

    public override async UniTask Restart()
    {
        await base.Restart();
        _scoreMaker.RestartMake();
    }
}
