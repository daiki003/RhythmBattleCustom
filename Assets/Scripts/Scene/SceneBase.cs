using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneBase : MonoBehaviour, IScene
{
    protected virtual string _prefabPath => "";
    protected SceneInfoBase _lastSceneInfo;
    protected SceneInfoBase _nextSceneInfo;

    public virtual async UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo)
    {
        _lastSceneInfo = lastSceneInfo;
        _nextSceneInfo = nextSceneInfo;
        await UniTask.CompletedTask;
    }
    public virtual void StartScene() {}
    public virtual void Restart() {}
    public virtual void Pause()
    {
        gameObject.SetActive(false);
    }
    public virtual void Dispose()
    {
        Destroy(gameObject);
    }
}
