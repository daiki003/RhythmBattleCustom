using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IScene
{
    public UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo);
    public void StartScene();
    public void Restart();
    public void Pause();
    public void Dispose();
}

public class SceneInfoBase
{
    public bool IsAdditional;
}

public class BattleSceneInfo : SceneInfoBase
{
    public SingleStageMaster StageMaster;
    public float TimeRate;
}

public class TitleSceneInfo : SceneInfoBase
{
    
}

public class ScoreMakerSceneInfo : SceneInfoBase
{
    public string StageId;
}
