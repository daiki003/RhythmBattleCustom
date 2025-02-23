using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public interface IScene
{
    public UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel);
    public UniTask StartSceneAsync();
    public UniTask Restart();
    public UniTask Pause();
    public UniTask DisposeAsync();
}

public class SceneInfoBase
{
    public bool IsAdditional;
}

public class BattleSceneInfo : SceneInfoBase
{
    public SingleStageMaster StageMaster;
    public bool IsPractice;
    public float TimeRate;
}

public class TitleSceneInfo : SceneInfoBase
{
    
}

public class ScoreMakerSceneInfo : SceneInfoBase
{
    public string StageId;
    public int FirstLevel;
}
