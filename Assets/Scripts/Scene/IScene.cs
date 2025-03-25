using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public StageHeader StageHeader;
    public LevelInfo LevelInfo;
    public bool IsPractice;
    public float TimeRate;
}

public class TitleSceneInfo : SceneInfoBase
{
    
}

public class ScoreMakerSceneInfo : SceneInfoBase
{
    public StageInfo StageInfo;
    public int TargetLevel;
    public bool IsNewCreate;
    public bool IsDevelopOverride;
}

public static class SceneInfoExtension
{
    public static int GetLastLevel(this SceneInfoBase sceneInfo)
    {
        if (sceneInfo is BattleSceneInfo battleSceneInfo)
        {
            return battleSceneInfo.LevelInfo.Level;
        }
        else if (sceneInfo is ScoreMakerSceneInfo scoreMakerSceneInfo)
        {
            return scoreMakerSceneInfo.TargetLevel;
        }
        return 1;
    }
}
