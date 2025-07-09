using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HomeScene : SceneBase
{
    [SerializeField] private HomePresenter _titleManager;

    public override async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        await base.InitAsync(lastSceneInfo, nextSceneInfo, fadePanel);
        _titleManager.Init();
    }
}
