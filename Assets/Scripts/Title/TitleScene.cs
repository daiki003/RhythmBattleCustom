using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TitleScene : SceneBase
{
    [SerializeField] private TitlePresenter _titlePresenter;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo, fadePanel);
        _titlePresenter.Init();
    }
}
