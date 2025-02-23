using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SceneBase : MonoBehaviour, IScene
{
    private Image _fadePanel;
    protected SceneInfoBase _lastSceneInfo;
    protected SceneInfoBase _nextSceneInfo;

    private const float _panelFadeTime = 0.5f;

    public virtual async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel)
    {
        _lastSceneInfo = lastSceneInfo;
        _nextSceneInfo = nextSceneInfo;
        _fadePanel = fadePanel;
        await UniTask.CompletedTask;
    }
    public virtual async UniTask StartSceneAsync()
    {
        await _fadePanel.DOFade(0, _panelFadeTime).SetEase(Ease.InQuad).ToUniTask();
    }
    public virtual async UniTask Restart()
    {
        gameObject.SetActive(true);
        await _fadePanel.DOFade(0, _panelFadeTime).SetEase(Ease.InQuad).ToUniTask();
    }
    public virtual async UniTask Pause()
    {
        await _fadePanel.DOFade(1, _panelFadeTime).ToUniTask();
        gameObject.SetActive(false);
    }
    public async UniTask DisposeAsync()
    {
        await _fadePanel.DOFade(1, _panelFadeTime).ToUniTask();
        Destroy(gameObject);
    }
}
