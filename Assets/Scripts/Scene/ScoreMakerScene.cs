using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerScene : SceneBase
{
    [SerializeField] private ScoreMakerPresenter _scoreMaker;
    private ScoreMakerSceneInfo _scoreMakerSceneInfo;
    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo, fadePanel);
        _scoreMakerSceneInfo = _nextSceneInfo as ScoreMakerSceneInfo;
        if (_scoreMakerSceneInfo == null)
        {
            return;
        }
        await BGMManager.instance.SetClipFromLibrary(_scoreMakerSceneInfo.StageMaster.StageHeader.MusicId, immediatePlay: false);
        _scoreMaker.Init(_scoreMakerSceneInfo.StageMaster, _scoreMakerSceneInfo.IsNewCreate);
    }

    public override async UniTask StartSceneAsync()
    {
        await base.StartSceneAsync();
        _scoreMaker.StartMake(isRestart: false);
    }

    public override async UniTask Restart()
    {
        await base.Restart();
        _scoreMaker.StartMake(isRestart: true);
    }
}
