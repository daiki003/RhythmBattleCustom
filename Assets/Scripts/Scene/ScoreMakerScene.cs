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
        await BGMManager.instance.SetStageClip(_scoreMakerSceneInfo.StageMaster.StageHeader.MusicId, immediatePlay: false);
        bool isTutorial = _scoreMakerSceneInfo.TutorialCommand != null;
        _scoreMaker.Init(_scoreMakerSceneInfo.StageMaster, isTutorial, _scoreMakerSceneInfo.Type);
        if (isTutorial)
        {
            _scoreMaker.PlayTutorialAsync(_scoreMakerSceneInfo.TutorialCommand).Forget();
        }
    }

    public override async UniTask StartSceneAsync()
    {
        await base.StartSceneAsync();
        _scoreMaker.StartMake();
    }

    public override async UniTask Restart()
    {
        await base.Restart();
        // 曲を再セット
        await BGMManager.instance.SetStageClip(_scoreMakerSceneInfo.StageMaster.StageHeader.MusicId, immediatePlay: false);
        _scoreMaker.StartMake();
    }
}
